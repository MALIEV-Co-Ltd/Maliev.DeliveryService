using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Application.Services;
using Maliev.DeliveryService.Infrastructure.Authorization;
using Maliev.DeliveryService.Infrastructure.Consumers;
using Maliev.DeliveryService.Infrastructure.HttpClients;
using Maliev.DeliveryService.Infrastructure.Persistence;
using Maliev.DeliveryService.Infrastructure.Services;
using Maliev.DeliveryService.Infrastructure.Storage;
using Maliev.Aspire.ServiceDefaults;
using Maliev.Aspire.ServiceDefaults.IAM;
using MassTransit;

// Initialize bootstrap logging
using var loggerFactory = LoggerFactory.Create(logBuilder => logBuilder.AddConsole());
var bootstrapLogger = loggerFactory.CreateLogger("Program");

try
{
    Program.Log.StartingHost(bootstrapLogger, "Delivery Service");

    var builder = WebApplication.CreateBuilder(args);

    // --- Secrets & Configuration ---
    builder.AddGoogleSecretManagerVolume(); // Load secrets from /mnt/secrets if available

    // --- Infrastructure & Observability ---
    builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience
    builder.AddStandardMiddleware(options =>
    {
        options.EnableRequestLogging = true;
    });
    builder.AddServiceMeters("delivery-meter"); // Register service meters for OpenTelemetry business metrics

    builder.Services.AddHttpContextAccessor();

    // Database Context with ServiceDefaults
    builder.AddPostgresDbContext<DeliveryDbContext>(
        connectionName: "DeliveryDbContext");

    builder.AddStandardCache("delivery:"); // Redis + in-memory fallback, memory-optimized

    builder.AddMassTransitWithRabbitMq(x =>
    {
        x.AddEntityFrameworkOutbox<DeliveryDbContext>(options =>
        {
            _ = options.UsePostgres();
            options.UseBusOutbox();
        });

        // Register all event consumers
        x.AddConsumer<OrderCompletedEventConsumer>();
    }); // RabbitMQ message bus (non-blocking startup)

    // IAM Registration
    builder.Services.AddIAMRegistration<DeliveryIAMRegistrationService>("delivery");

    // --- API Configuration ---
    builder.AddStandardCors(); // CORS with fail-fast validation
    builder.AddDefaultApiVersioning(); // API versioning with URL segment reader

    // JWT Authentication
    builder.AddJwtAuthentication();

    // Add OpenAPI
    if (!builder.Environment.IsProduction())
    {
        builder.AddStandardOpenApi(
            title: "MALIEV Delivery Service API",
            description: "Delivery and logistics service. Handles delivery notes, shipping tracking, carrier integration, and proof of delivery.");
    }

    builder.Services.AddControllers();

    // Register application services
    builder.Services.AddScoped<DeliveryNoteIdGenerator>();
    builder.Services.AddScoped<IDeliveryNoteAuthorizationService, DeliveryNoteAuthorizationService>();
    builder.Services.AddScoped<IDeliveryNoteService, DeliveryNoteService>();

    // Register Google Cloud Storage
    if (builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddSingleton<IFileStorageService, InMemoryFileStorageService>();
    }
    else
    {
        try
        {
            builder.Services.AddSingleton(Google.Cloud.Storage.V1.StorageClient.Create());
            builder.Services.AddScoped<IFileStorageService, GoogleCloudStorageService>();
        }
        catch (Exception ex)
        {
            bootstrapLogger.LogWarning(ex, "Google Cloud Storage client could not be initialized");
        }
    }

    // Register HTTP Clients with Aspire resilience
    builder.Services.AddHttpClient<IOrderServiceClient, OrderServiceClient>(client =>
    {
        // Base address will be resolved by service discovery if "order-service" is used
        client.BaseAddress = new Uri(builder.Configuration["OrderService:BaseUrl"] ?? "http://order-service");
    })
    .AddHttpMessageHandler<Maliev.Aspire.ServiceDefaults.IAM.ServiceAccountAuthenticationHandler>();

    // Authorization Infrastructure
    builder.Services.AddPermissionAuthorization();

    // IAM Registration
    builder.AddIAMServiceClient("delivery");

    var app = builder.Build();
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    // --- Database Migrations ---
    await app.MigrateDatabaseAsync<DeliveryDbContext>();

    // Middleware Pipeline
    app.UseStandardMiddleware();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseCors();

    app.UseAuthentication();
    app.UseAuthorization();

    // Map endpoints after middleware
    app.MapControllers();

    // Map Aspire default endpoints (/health, /alive, /metrics)
    app.MapDefaultEndpoints(servicePrefix: "delivery");

    // Map OpenAPI and Scalar documentation (dev/staging only)
    app.MapApiDocumentation(servicePrefix: "delivery");

    Program.Log.ServiceStarted(logger, "Delivery Service");
    await app.RunAsync();
}
catch (Exception ex)
{
    Program.Log.HostTerminated(bootstrapLogger, ex, "Delivery Service");
    throw;
}
finally
{
    loggerFactory.Dispose();
}

/// <summary>
/// Main program class for the application
/// </summary>
public partial class Program
{
    internal static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Starting {ServiceName} host")]
        public static partial void StartingHost(ILogger logger, string serviceName);

        [LoggerMessage(Level = LogLevel.Critical, Message = "{ServiceName} host terminated unexpectedly during startup")]
        public static partial void HostTerminated(ILogger logger, Exception ex, string serviceName);

        [LoggerMessage(Level = LogLevel.Information, Message = "{ServiceName} started successfully")]
        public static partial void ServiceStarted(ILogger logger, string serviceName);
    }
}
