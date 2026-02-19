using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults;
using Maliev.DeliveryService.Api.Clients;
using Maliev.DeliveryService.Api.Services;
using Maliev.DeliveryService.Data;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire ServiceDefaults (OpenTelemetry, Health Checks, Service Discovery)
builder.AddServiceDefaults();

// Configure DbContext with PostgreSQL using Aspire extension
builder.AddPostgresDbContext<DeliveryDbContext>(connectionName: "DeliveryDbContext");

// Configure MassTransit with RabbitMQ using Aspire extension
builder.AddMassTransitWithRabbitMq(x =>
{
    // Register event consumers
    x.AddConsumer<Maliev.DeliveryService.Api.Consumers.OrderCompletedEventConsumer>();
}, (context, cfg) =>
{
    // Configure retry policy with exponential backoff (1s, 2s, 4s, 8s, 16s)
    cfg.UseMessageRetry(r =>
    {
        r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(1));
    });

    cfg.ConfigureEndpoints(context);
});

// Configure Redis using Aspire extension
builder.AddStandardCache("delivery:");

// JWT Authentication and Permission Authorization
builder.AddJwtAuthentication();
builder.Services.AddPermissionAuthorization();

// Register IAM service
builder.Services.AddIAMRegistration<DeliveryIAMRegistrationService>("delivery");

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

// Register HTTP Clients with Aspire resilience
builder.Services.AddHttpClient<IOrderServiceClient, OrderServiceClient>(client =>
{
    // Use service discovery name
    var baseUrl = builder.Configuration["OrderService:BaseUrl"];
    client.BaseAddress = new Uri(!string.IsNullOrEmpty(baseUrl) ? baseUrl : "http://OrderService");
    client.Timeout = TimeSpan.FromSeconds(10);
}).AddStandardResilienceHandler();

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
if (!builder.Environment.IsProduction())
{
    builder.Services.AddOpenApi();
}

// Custom health checks (Postgres, Redis and RabbitMQ are handled by extensions)
builder.Services.AddHealthChecks()
    .AddCheck<Maliev.DeliveryService.Api.HealthChecks.DatabaseMigrationHealthCheck>(
        "database_migrations",
        tags: new[] { "ready" });

var app = builder.Build();

// Apply database migrations on startup
await app.MigrateDatabaseAsync<DeliveryDbContext>();

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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make Program class accessible for testing
public partial class Program { }
