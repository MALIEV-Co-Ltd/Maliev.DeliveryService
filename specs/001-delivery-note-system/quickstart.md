# Developer Quickstart: Delivery Note System

**Feature**: Delivery Note System
**Date**: 2026-02-16
**Audience**: Developers implementing or extending the Delivery Note System

## Overview

This guide helps developers get started with the Delivery Note System. It covers local setup, development workflow, testing, and common development tasks.

## Prerequisites

Before you begin, ensure you have:

✅ **.NET 10 SDK** installed (`dotnet --version` should show 10.x)
✅ **Docker Desktop** installed and running (for Aspire and Testcontainers)
✅ **PostgreSQL 18** client tools (optional, for database inspection)
✅ **Git** for version control
✅ **IDE**: Visual Studio 2025, VS Code with C# Dev Kit, or JetBrains Rider

## Quick Start (5 Minutes)

### 1. Clone and Build

```bash
# Navigate to repository root
cd B:\maliev\Maliev.DeliveryService

# Restore dependencies
dotnet restore Maliev.DeliveryService.slnx

# Build solution
dotnet build Maliev.DeliveryService.slnx --configuration Debug

# Verify zero warnings (TreatWarningsAsErrors=true)
```

### 2. Run with Aspire

```bash
# Navigate to Aspire AppHost
cd ../Maliev.Aspire/Maliev.Aspire.AppHost

# Run Aspire orchestration
dotnet run

# Open browser to Aspire dashboard (usually http://localhost:15888)
```

**What happens**:
- PostgreSQL container starts
- RabbitMQ container starts
- Redis container starts
- DeliveryService starts and runs migrations
- BFF starts and connects to DeliveryService

### 3. Test the API

**Option A: Scalar UI** (Recommended)
```
Open: http://localhost:5000/scalar/v1
```

**Option B: curl**
```bash
# Create delivery note
curl -X POST http://localhost:5000/delivery/v1/delivery-notes \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "orderId": "ORD-2026-001",
    "deliveryDate": "2026-02-20T10:00:00Z",
    "deliveryContactName": "John Smith",
    "deliveryContactPhone": "+66-123-456-789",
    "items": [{
      "productCode": "PROD-001",
      "productName": "Widget A",
      "quantityOrdered": 100,
      "quantityManufactured": 95,
      "quantityDelivered": 95,
      "unitOfMeasure": "pcs"
    }]
  }'
```

## Project Structure

```
Maliev.DeliveryService/
├── Maliev.DeliveryService.Api/          # Web API project
│   ├── Controllers/                     # API endpoints
│   ├── DTOs/                            # Request/response models + manual mapping
│   ├── Services/                        # Business logic
│   ├── Clients/                         # HTTP clients to other services
│   ├── Consumers/                       # MassTransit event consumers
│   └── Program.cs                       # Startup configuration
│
├── Maliev.DeliveryService.Data/         # Data access project
│   ├── Entities/                        # EF entity classes
│   ├── Configurations/                  # EF configurations
│   ├── DeliveryDbContext.cs             # DbContext
│   └── Migrations/                      # EF migrations
│
└── Maliev.DeliveryService.Tests/        # Test project
    ├── Unit/                            # Fast, isolated unit tests
    ├── Integration/                     # Slower integration tests with Testcontainers
    └── Fakes/                           # Manual test doubles (NO mocking libraries)
```

## Development Workflow

### Adding a New Feature

1. **Create a branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Write failing tests** (TDD):
   ```csharp
   // Maliev.DeliveryService.Tests/Unit/Services/DeliveryNoteServiceTests.cs
   [Fact]
   public async Task YourNewFeature_When_Then()
   {
       // Arrange
       var service = CreateService();

       // Act
       var result = await service.YourNewMethod();

       // Assert
       result.Should().NotBeNull();
   }
   ```

3. **Implement the feature**:
   - Add/modify entities in `Data/Entities/`
   - Update `Services/` business logic
   - Create/update DTOs in `DTOs/`
   - Add controller endpoints in `Controllers/`

4. **Make tests pass**:
   ```bash
   dotnet test Maliev.DeliveryService.Tests --filter Category=Unit
   ```

5. **Run integration tests**:
   ```bash
   dotnet test Maliev.DeliveryService.Tests --filter Category=Integration
   ```

6. **Verify build**:
   ```bash
   dotnet build Maliev.DeliveryService.slnx --configuration Release
   ```

### Database Migrations

**Create a migration**:
```bash
cd Maliev.DeliveryService.Data

# Add migration
dotnet ef migrations add YourMigrationName --startup-project ../Maliev.DeliveryService.Api

# Review generated migration in Migrations/ folder
# Edit if necessary (before applying)

# Apply migration to local database
dotnet ef database update --startup-project ../Maliev.DeliveryService.Api
```

**Rollback a migration**:
```bash
# Rollback to previous migration
dotnet ef database update PreviousMigrationName --startup-project ../Maliev.DeliveryService.Api

# Remove the migration file
dotnet ef migrations remove --startup-project ../Maliev.DeliveryService.Api
```

**Best Practices**:
- Always review generated migrations before applying
- Test both `Up()` and `Down()` methods
- Use meaningful migration names: `AddTrackingNumberIndex`, `UpdateDeliveryStatusEnum`
- Keep migrations small and focused on one change

### Adding an API Endpoint

**Example: Add "Get Delivery Notes by Customer"**

1. **Add method to service interface**:
   ```csharp
   // Services/IDeliveryNoteService.cs
   Task<List<DeliveryNoteSummaryDto>> GetDeliveryNotesByCustomerAsync(
       Guid customerId,
       CancellationToken ct = default);
   ```

2. **Implement in service**:
   ```csharp
   // Services/DeliveryNoteService.cs
   public async Task<List<DeliveryNoteSummaryDto>> GetDeliveryNotesByCustomerAsync(
       Guid customerId,
       CancellationToken ct = default)
   {
       return await _context.DeliveryNotes
           .Where(dn => dn.CustomerId == customerId)
           .OrderByDescending(dn => dn.CreatedAt)
           .Select(dn => new DeliveryNoteSummaryDto
           {
               DeliveryNoteId = dn.DeliveryNoteId,
               CustomerName = dn.CustomerName,
               DeliveryDate = dn.DeliveryDate,
               Status = dn.Status.ToString(),
               ItemCount = dn.Items.Count
           })
           .ToListAsync(ct);
   }
   ```

3. **Add controller endpoint**:
   ```csharp
   // Controllers/DeliveryNotesController.cs
   [HttpGet("by-customer/{customerId}")]
   [ProducesResponseType(typeof(List<DeliveryNoteSummaryDto>), StatusCodes.Status200OK)]
   public async Task<ActionResult<List<DeliveryNoteSummaryDto>>> GetByCustomer(
       [FromRoute] Guid customerId,
       CancellationToken ct)
   {
       // Authorization check
       var principalId = User.GetPrincipalId();
       if (!await _authorizationService.CanAccessCustomerAsync(principalId, customerId, ct))
       {
           return Forbid();
       }

       var result = await _deliveryNoteService.GetDeliveryNotesByCustomerAsync(customerId, ct);
       return Ok(result);
   }
   ```

4. **Test**:
   ```csharp
   [Fact]
   public async Task GetByCustomer_ReturnsDeliveryNotes()
   {
       // Arrange
       var customerId = Guid.NewGuid();
       await SeedDeliveryNote(customerId);

       // Act
       var response = await _client.GetAsync($"/delivery/v1/delivery-notes/by-customer/{customerId}");

       // Assert
       response.StatusCode.Should().Be(HttpStatusCode.OK);
       var notes = await response.Content.ReadFromJsonAsync<List<DeliveryNoteSummaryDto>>();
       notes.Should().HaveCount(1);
   }
   ```

### Manual DTO Mapping (NO AutoMapper)

**Pattern**: Extension methods on entity classes

```csharp
// DTOs/DtoMappingExtensions.cs
public static class DtoMappingExtensions
{
    public static DeliveryNoteResponse ToResponse(this DeliveryNote entity)
    {
        return new DeliveryNoteResponse
        {
            DeliveryNoteId = entity.DeliveryNoteId,
            OrderId = entity.OrderId,
            CustomerId = entity.CustomerId,
            CustomerName = entity.CustomerName,
            DeliveryDate = entity.DeliveryDate,
            Status = entity.Status.ToString(),
            Items = entity.Items.Select(i => i.ToResponse()).ToList(),
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
    }

    public static DeliveryNoteItemResponse ToResponse(this DeliveryNoteItem entity)
    {
        return new DeliveryNoteItemResponse
        {
            Id = entity.Id,
            ProductCode = entity.ProductCode,
            ProductName = entity.ProductName,
            QuantityOrdered = entity.QuantityOrdered,
            QuantityManufactured = entity.QuantityManufactured,
            QuantityDelivered = entity.QuantityDelivered,
            UnitOfMeasure = entity.UnitOfMeasure
        };
    }
}
```

**Usage**:
```csharp
// In service
var deliveryNote = await _context.DeliveryNotes
    .Include(dn => dn.Items)
    .FirstOrDefaultAsync(dn => dn.DeliveryNoteId == id);

return deliveryNote.ToResponse(); // Extension method
```

### Validation (NO FluentValidation)

**Pattern**: Inline validation in service methods

```csharp
private void ValidateCreateRequest(CreateDeliveryNoteRequest request)
{
    // Required fields
    if (request.Items == null || !request.Items.Any())
        throw new ValidationException("At least one item is required");

    // Business rules
    if (request.OrderId == null && request.PurchaseOrderId == null)
        throw new ValidationException("Either OrderId or PurchaseOrderId must be specified");

    // Item-level validation
    foreach (var item in request.Items)
    {
        if (item.QuantityDelivered <= 0)
            throw new ValidationException($"Quantity delivered must be positive for {item.ProductCode}");

        if (item.QuantityDelivered > item.QuantityManufactured)
            throw new ValidationException(
                $"Cannot deliver more than manufactured for {item.ProductCode}. " +
                $"Manufactured: {item.QuantityManufactured}, Attempted: {item.QuantityDelivered}");
    }
}
```

## Testing Strategies

### Unit Tests (Fast, Isolated)

**Use InMemoryDatabase** for speed:
```csharp
public class DeliveryNoteServiceTests
{
    private DeliveryDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<DeliveryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new DeliveryDbContext(options);
    }

    [Fact]
    public async Task CreateDeliveryNote_ValidRequest_ReturnsDeliveryNote()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var fakePublisher = new FakePublishEndpoint();
        var service = new DeliveryNoteService(context, fakePublisher, new DeliveryNoteIdGenerator(context));

        // Act
        var result = await service.CreateDeliveryNoteAsync(request, "user", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.DeliveryNoteId.Should().StartWith("DN-2026-");
    }
}
```

### Integration Tests (Testcontainers)

**Use real PostgreSQL and RabbitMQ**:
```csharp
public class DeliveryNotesControllerTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgresContainer = null!;
    private RabbitMqContainer _rabbitMqContainer = null!;
    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:18")
            .WithDatabase("test_delivery_db")
            .Build();
        await _postgresContainer.StartAsync();

        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .Build();
        await _rabbitMqContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace services with test containers
                    services.RemoveAll<DbContextOptions<DeliveryDbContext>>();
                    services.AddDbContext<DeliveryDbContext>(options =>
                        options.UseNpgsql(_postgresContainer.GetConnectionString()));
                });
            });

        // Run migrations
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    [Fact]
    public async Task CreateDeliveryNote_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/delivery/v1/delivery-notes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<DeliveryNoteResponse>();
        result.Should().NotBeNull();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
        await _factory.DisposeAsync();
    }
}
```

### Manual Test Doubles (NO Mocking Libraries)

**Create fake implementations**:
```csharp
// Tests/Fakes/FakePublishEndpoint.cs
public class FakePublishEndpoint : IPublishEndpoint
{
    public List<object> PublishedMessages { get; } = new();

    public Task Publish<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        PublishedMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        PublishedMessages.Add(message);
        return Task.CompletedTask;
    }

    // ... implement other interface methods
}
```

**Usage in tests**:
```csharp
var fakePublisher = new FakePublishEndpoint();
var service = new DeliveryNoteService(context, fakePublisher, idGenerator);

await service.CreateDeliveryNoteAsync(request, "user", ct);

// Assert event was published
var publishedEvent = fakePublisher.PublishedMessages
    .OfType<DeliveryNoteCreatedEvent>()
    .Single();
```

## Common Tasks

### Debugging

**Attach to running service**:
1. Start Aspire: `dotnet run` in AppHost
2. In VS/Rider: Attach to process `Maliev.DeliveryService.Api`
3. Set breakpoints in Controllers or Services
4. Make API request via Scalar or curl

**View logs**:
```bash
# Aspire dashboard shows structured logs
# Open: http://localhost:15888
# Navigate to: Logs > deliveryservice
```

**Inspect database**:
```bash
# Connect with psql
psql -h localhost -U postgres -d delivery_service_db

# View delivery notes
SELECT * FROM delivery_notes;

# View items
SELECT * FROM delivery_note_items WHERE delivery_note_id = 'DN-2026-000001';
```

### Performance Testing

**Benchmark with k6**:
```javascript
// load-test.js
import http from 'k6/http';
import { check } from 'k6';

export let options = {
  vus: 10,
  duration: '30s',
};

export default function () {
  let payload = JSON.stringify({
    orderId: 'ORD-2026-001',
    deliveryDate: '2026-02-20T10:00:00Z',
    deliveryContactName: 'Test User',
    deliveryContactPhone: '+66-123-456-789',
    items: [{
      productCode: 'PROD-001',
      productName: 'Widget',
      quantityOrdered: 100,
      quantityManufactured: 100,
      quantityDelivered: 100,
      unitOfMeasure: 'pcs'
    }]
  });

  let res = http.post('http://localhost:5000/delivery/v1/delivery-notes', payload, {
    headers: { 'Content-Type': 'application/json' },
  });

  check(res, {
    'status is 201': (r) => r.status === 201,
    'response time < 200ms': (r) => r.timings.duration < 200,
  });
}
```

**Run load test**:
```bash
k6 run load-test.js
```

### Monitoring

**View metrics** in Aspire dashboard:
- Request counts
- Error rates
- Response times (p50, p95, p99)
- Database query performance

**View distributed traces**:
1. Open Aspire dashboard
2. Navigate to Traces
3. Filter by service: `deliveryservice`
4. Click on a trace to see full call graph

## Troubleshooting

### Problem: Migrations fail to apply

**Solution**:
```bash
# Drop database and recreate
dotnet ef database drop --force --startup-project Maliev.DeliveryService.Api
dotnet ef database update --startup-project Maliev.DeliveryService.Api
```

### Problem: Testcontainers fail to start

**Solution**:
- Ensure Docker Desktop is running
- Check Docker resource limits (increase if needed)
- Clean up stopped containers: `docker system prune -a`

### Problem: Events not publishing

**Solution**:
1. Check RabbitMQ management UI: `http://localhost:15672` (user: guest, pass: guest)
2. Verify exchange exists: `maliev.delivery.events`
3. Check logs for publish errors
4. Verify MassTransit configuration in Program.cs

### Problem: Authorization fails

**Solution**:
- Verify JWT token is valid
- Check token includes required claims (user ID, customer assignments)
- Verify `IDeliveryNoteAuthorizationService` is registered in DI
- Check authorization logic in controller

## Resources

- **API Documentation**: http://localhost:5000/scalar/v1
- **Aspire Dashboard**: http://localhost:15888
- **RabbitMQ Management**: http://localhost:15672
- **Spec Files**: `B:\maliev\Maliev.DeliveryService\specs\001-delivery-note-system\`
- **Maliev Coding Standards**: `B:\maliev\docs\coding-standards.md` (if exists)

## Next Steps

After completing this quickstart:

1. Review [data-model.md](./data-model.md) for entity relationships
2. Review [contracts/delivery-notes.openapi.yaml](./contracts/delivery-notes.openapi.yaml) for full API spec
3. Review [contracts/events.md](./contracts/events.md) for event contracts
4. Run `/speckit.tasks` to generate implementation tasks
5. Start implementing! Follow TDD and maintain 80%+ test coverage

---

**Happy coding! 🚀**
