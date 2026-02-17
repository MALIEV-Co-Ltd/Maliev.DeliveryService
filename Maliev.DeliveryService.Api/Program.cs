using Asp.Versioning;
using Maliev.DeliveryService.Api.Clients;
using Maliev.DeliveryService.Api.Services;
using Maliev.DeliveryService.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire ServiceDefaults (OpenTelemetry, Health Checks, Service Discovery)
builder.AddServiceDefaults();

// Configure DbContext with PostgreSQL
builder.Services.AddDbContext<DeliveryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DeliveryDb")));

// Configure MassTransit with RabbitMQ
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddMassTransit(x =>
    {
        // Register event consumers
        x.AddConsumer<Maliev.DeliveryService.Api.Consumers.OrderCompletedEventConsumer>();

        x.UsingRabbitMq((context, cfg) =>
        {
            var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ");
            cfg.Host(rabbitMqConfig["Host"], h =>
            {
                h.Username(rabbitMqConfig["Username"] ?? "guest");
                h.Password(rabbitMqConfig["Password"] ?? "guest");
            });

            // Configure retry policy with exponential backoff (1s, 2s, 4s, 8s, 16s)
            cfg.UseMessageRetry(r =>
            {
                r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(1));
            });

            cfg.ConfigureEndpoints(context);
        });
    });
}
else
{
    builder.Services.AddMassTransit(x =>
    {
        x.UsingInMemory((context, cfg) =>
        {
            cfg.ConfigureEndpoints(context);
        });
    });
}

// Configure Redis
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetSection("Redis")["ConnectionString"];
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

// Register Services
builder.Services.AddScoped<DeliveryNoteIdGenerator>();
builder.Services.AddScoped<IDeliveryNoteAuthorizationService, DeliveryNoteAuthorizationService>();
builder.Services.AddScoped<IDeliveryNoteService, DeliveryNoteService>();

// Register Google Cloud Storage
if (!builder.Environment.IsEnvironment("Testing"))
{
    try 
    {
        builder.Services.AddSingleton(Google.Cloud.Storage.V1.StorageClient.Create());
        builder.Services.AddScoped<IFileStorageService, GoogleCloudStorageService>();
    }
    catch (Exception ex)
    {
        // Log warning but don't crash startup - allow app to run with limited functionality
        Console.WriteLine($"WARNING: Google Cloud Storage client could not be initialized: {ex.Message}");
    }
}
else
{
    // File storage not registered in testing environment - tests should mock IFileStorageService via WebApplicationFactory
}

// Register HTTP Clients with Aspire resilience
builder.Services.AddHttpClient<IOrderServiceClient, OrderServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["OrderService:BaseUrl"] ?? "http://localhost:5001");
    client.Timeout = TimeSpan.FromSeconds(10);
}).AddStandardResilienceHandler(); // Aspire built-in exponential backoff retry

// Add Controllers
builder.Services.AddControllers();

// Configure API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

// Configure OpenAPI
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddOpenApi();
}

// Configure Health Checks
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("DeliveryDb") ?? throw new InvalidOperationException("DeliveryDb connection string not configured"))
        .AddRedis(builder.Configuration.GetSection("Redis")["ConnectionString"] ?? throw new InvalidOperationException("Redis connection string not configured"))
        .AddRabbitMQ()
        .AddCheck<Maliev.DeliveryService.Api.HealthChecks.DatabaseMigrationHealthCheck>(
            "database_migrations",
            tags: new[] { "ready" });
}

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapDefaultEndpoints("delivery"); // Aspire health checks and diagnostics

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Delivery Service API")
               .WithTheme(ScalarTheme.Mars)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make Program class accessible for testing
public partial class Program { }
