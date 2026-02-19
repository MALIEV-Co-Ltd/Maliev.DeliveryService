# Phase 0: Research & Technical Decisions

**Feature**: Delivery Note System
**Date**: 2026-02-16

## Purpose

This document captures all technical research and decision-making that resolved "NEEDS CLARIFICATION" items from the Technical Context. It provides the foundation for the implementation plan.

## Architecture Decision: New Microservice vs Extension

### Decision

Create a new standalone microservice called `Maliev.DeliveryService`.

### Rationale

1. **Domain Separation**: Delivery logistics is a distinct bounded context from order management and procurement
2. **Cross-Service References**: DeliveryNote must reference entities from BOTH OrderService (customer orders) AND PurchaseOrderService (supplier deliveries)
3. **Independent Scaling**: Delivery tracking may have different load patterns than order processing
4. **Established Pattern**: Maliev platform already has 25+ microservices following this pattern
5. **Future Growth**: Easier to add carrier integrations, route optimization, and logistics features
6. **Team Autonomy**: Allows dedicated team ownership without coupling to other services

### Alternatives Considered

| Alternative | Rejected Because |
|-------------|------------------|
| Extend OrderService | Would couple delivery logic to order management; doesn't handle purchase order deliveries; violates single responsibility |
| Extend PurchaseOrderService | Same coupling issue; doesn't handle customer order deliveries |
| Shared Library | Doesn't provide independent API, database, or deployment; can't scale independently |

## Tech Stack Selection

### Language & Framework

**Decision**: C# 13.0, .NET 10.0, ASP.NET Core 10.0 Web API

**Rationale**:
- Consistency with existing Maliev services (all .NET)
- Excellent Aspire integration for service orchestration
- Strong typing prevents runtime errors
- Native async/await for high performance
- Mature ecosystem for enterprise features

### Database

**Decision**: PostgreSQL 18

**Rationale**:
- Maliev standard for transactional data
- Excellent support for JSON (future extensibility)
- Partial indexes for soft-delete queries
- Proven reliability for ACID transactions
- EF Core 10.0 has excellent PostgreSQL support

**Alternatives Considered**:
- SQL Server: Rejected (PostgreSQL is Maliev standard)
- MongoDB: Rejected (delivery notes need ACID transactions)

### Messaging

**Decision**: RabbitMQ via MassTransit

**Rationale**:
- Maliev platform standard
- Reliable message delivery
- MassTransit provides excellent abstraction
- Supports retry, saga patterns, and message scheduling
- Good observability integration

### Caching

**Decision**: Redis via StackExchange.Redis

**Rationale**:
- Maliev platform standard
- Fast in-memory lookups for frequently accessed delivery notes
- Distributed caching across instances
- Good for reducing database load on read-heavy operations

### File Storage

**Decision**: Google Cloud Storage via UploadService

**Rationale**:
- Maliev platform already has UploadService abstraction
- Handles file uploads, virus scanning, and URL generation
- Cheaper than database storage for files
- Supports large files (photos, PDFs, signatures)

## Sequential ID Generation Strategy

### Decision

Database-driven sequential ID generation with format `DN-YYYY-XXXXXX`.

### Implementation Approach

```csharp
public async Task<string> GenerateNextIdAsync()
{
    var currentYear = DateTime.UtcNow.Year;
    var prefix = $"DN-{currentYear}-";

    // Use serializable transaction to prevent race conditions
    using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

    var lastId = await _context.DeliveryNotes
        .Where(dn => dn.DeliveryNoteId.StartsWith(prefix))
        .OrderByDescending(dn => dn.DeliveryNoteId)
        .Select(dn => dn.DeliveryNoteId)
        .FirstOrDefaultAsync();

    int nextNumber = 1;
    if (lastId != null)
    {
        var numberPart = lastId.Substring(prefix.Length);
        if (int.TryParse(numberPart, out int currentNumber))
        {
            nextNumber = currentNumber + 1;
        }
    }

    await transaction.CommitAsync();
    return $"{prefix}{nextNumber:D6}";
}
```

### Rationale

- Human-readable IDs for customer support
- Year prefix allows sequence reset annually
- Database transaction ensures uniqueness even under concurrent load
- 6-digit padding supports 999,999 delivery notes per year

### Risk Mitigation

- **Concurrency**: Serializable isolation prevents race conditions
- **Retry Logic**: Up to 3 attempts if transaction conflicts occur
- **Alternative**: PostgreSQL sequence with custom formatting (future optimization if needed)

## Service Integration Patterns

### Decision: Graceful Degradation with Retry

**Pattern**:
```csharp
// Core operation succeeds even if non-critical service fails
try
{
    await _context.SaveChangesAsync(); // Critical: must succeed
}
catch (Exception ex)
{
    // Critical failure - propagate
    throw;
}

// Non-critical: Retry with exponential backoff
try
{
    await _publishEndpoint.Publish(deliveryEvent);
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Event publish failed, queuing for retry");
    await _retryQueue.EnqueueAsync(deliveryEvent);
}

try
{
    await _orderServiceClient.UpdateDeliveryDateAsync(orderId, deliveryDate);
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "OrderService update failed, will retry");
    // Return partial success to user with warning
}
```

**Rationale**:
- **User Experience**: Users aren't blocked by transient failures
- **Reliability**: Critical data (delivery note) persists regardless of integration issues
- **Eventual Consistency**: Failed integrations retry until successful
- **Transparency**: Users informed of partial failures

### Event Publishing Strategy

**Decision**: Fire-and-forget with retry queue

**Pattern**:
1. Delivery note operation commits to database (critical path)
2. Events published asynchronously
3. Failed publishes queued for retry
4. Exponential backoff (1s, 2s, 4s, 8s, 16s)
5. Dead letter queue after 5 retries

**Rationale**:
- Decouples core business logic from messaging infrastructure
- Prevents cascading failures
- Maintains event ordering within retry window
- Supports observability via dead letter queue monitoring

## Observability Approach

### Decision: OpenTelemetry with Structured Logging

**Components**:

1. **Structured Logging**
   - JSON format logs
   - Correlation IDs on all log lines
   - Consistent field names: `operation`, `deliveryNoteId`, `userId`, `duration`, `outcome`

2. **Metrics**
   - Request counts by endpoint
   - Error rates by operation type
   - Operation latencies (p50, p95, p99)
   - Active delivery note counts by status
   - Integration call success rates

3. **Distributed Tracing**
   - Trace IDs propagated across all service calls
   - Span for each operation: database query, HTTP call, event publish
   - Automatic instrumentation via OpenTelemetry

**Rationale**:
- **Troubleshooting**: 95% of issues diagnosable within 5 minutes using logs/traces
- **Performance**: Identify slow queries and integration bottlenecks
- **Alerting**: Proactive notification on error rate spikes or latency degradation
- **Compliance**: Audit trail for all delivery note changes

**Aspire Integration**:
- OpenTelemetry configured automatically via ServiceDefaults
- Metrics visible in Aspire dashboard
- Distributed traces across all services
- No manual instrumentation needed

## Data Retention & Archival Strategy

### Decision: 7-Year Retention with 2-Year Archival Threshold

**Implementation**:

```
Year 0-2 (Active):    Primary PostgreSQL database (fast access <2s)
Year 3-7 (Archived):  Cheaper storage tier (acceptable <5s access)
Year 7+ (Deleted):    Permanent deletion with audit log
```

**Rationale**:
- **Compliance**: Thai business/tax regulations typically require 5-7 years retention
- **Performance**: Keeps primary database fast by limiting to 2 years of data
- **Cost Optimization**: Archival storage is significantly cheaper
- **Audit Trail**: Deletion events logged for regulatory compliance

**Archival Process**:
1. Scheduled job runs monthly
2. Identifies delivery notes >2 years old
3. Copies to archival storage
4. Soft-deletes from primary database (but not hard-deleted yet)
5. Delivery notes >7 years permanently deleted with audit log

**Access Pattern**:
- Recent notes (0-2 years): Direct database query
- Archived notes (3-7 years): Check primary DB first, then archival storage
- Deleted notes (7+ years): Return "not found" with audit log entry

## API Versioning Strategy

### Decision: URL Path Versioning

**Pattern**: `/delivery/v1/delivery-notes`, `/delivery/v2/delivery-notes`

**Rationale**:
- **Explicit**: Version immediately visible in URL
- **Tooling**: Works well with API gateways, documentation tools, client SDKs
- **Industry Standard**: Most RESTful APIs use this pattern
- **Already Established**: Matches existing Maliev API pattern

**Versioning Policy**:
1. New version when making breaking changes
2. Both versions run concurrently during migration period
3. Minimum 6-month deprecation notice before removing old version
4. No more than 2 versions supported simultaneously

**Breaking vs Non-Breaking**:
- **Breaking**: Removing fields, changing field types, changing semantics
- **Non-Breaking**: Adding optional fields, adding new endpoints

## Banned Libraries Compliance

### Rationale for Bans

| Banned Library | Why Banned | Alternative |
|----------------|-----------|-------------|
| AutoMapper | Hides mapping logic; hard to debug; runtime performance cost | Manual mapping extensions |
| FluentValidation | Separates validation from domain logic | Inline validation in service methods |
| MediatR | Unnecessary abstraction; makes call graph opaque | Direct service method calls |
| Moq/NSubstitute | Over-abstraction; brittle tests | Manual test doubles (Fakes) |

### Implementation

**Manual DTO Mapping**:
```csharp
public static class DtoMappingExtensions
{
    public static DeliveryNoteResponse ToResponse(this DeliveryNote entity)
    {
        return new DeliveryNoteResponse
        {
            DeliveryNoteId = entity.DeliveryNoteId,
            OrderId = entity.OrderId,
            Status = entity.Status.ToString(),
            // ... explicit mapping
        };
    }
}
```

**Inline Validation**:
```csharp
private void ValidateCreateRequest(CreateDeliveryNoteRequest request)
{
    if (request.Items == null || !request.Items.Any())
        throw new ValidationException("At least one item required");

    foreach (var item in request.Items)
    {
        if (item.QuantityDelivered > item.QuantityManufactured)
            throw new ValidationException($"Cannot deliver more than manufactured for {item.ProductCode}");
    }
}
```

**Manual Test Doubles**:
```csharp
public class FakeOrderServiceClient : IOrderServiceClient
{
    public List<string> UpdatedOrderIds { get; } = new();

    public Task UpdateDeliveryDateAsync(string orderId, DateTime date)
    {
        UpdatedOrderIds.Add(orderId);
        return Task.CompletedTask;
    }
}
```

## Testing Approach

### Decision: Testcontainers for Integration Tests

**Rationale**:
- **Real Dependencies**: Tests run against actual PostgreSQL 18 and RabbitMQ
- **CI/CD Friendly**: Containers managed by test framework
- **Isolation**: Each test suite gets fresh containers
- **Confidence**: Tests validate real database migrations, queries, and messaging

**Test Structure**:
```csharp
public class DeliveryServiceTestFixture : IAsyncLifetime
{
    private PostgreSqlContainer _postgresContainer;
    private RabbitMqContainer _rabbitMqContainer;

    public async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:18")
            .Build();
        await _postgresContainer.StartAsync();

        // Run EF migrations
        // ...
    }
}
```

### Unit vs Integration Test Split

**Unit Tests** (fast, isolated):
- Business logic validation
- DTO mapping correctness
- Status transition rules
- Edge case handling
- Use in-memory database for speed

**Integration Tests** (slower, comprehensive):
- Full API endpoints via WebApplicationFactory
- Real database operations with Testcontainers
- Event publishing to RabbitMQ
- Authorization checks
- End-to-end workflows

**Target**: 80%+ total coverage with majority from unit tests

## Performance Optimization Strategies

### Database Optimization

1. **Partial Indexes**: Only index non-deleted records
   ```sql
   CREATE INDEX idx_delivery_notes_customer_id ON delivery_notes(customer_id)
   WHERE NOT is_deleted;
   ```

2. **Eager Loading**: Include related entities when needed
   ```csharp
   await _context.DeliveryNotes
       .Include(dn => dn.Items)
       .Include(dn => dn.Files)
       .FirstOrDefaultAsync(dn => dn.DeliveryNoteId == id);
   ```

3. **Compiled Queries**: For frequently-used queries
   ```csharp
   private static readonly Func<DeliveryDbContext, string, Task<DeliveryNote?>>
       GetByIdQuery = EF.CompileAsyncQuery(
           (DeliveryDbContext ctx, string id) =>
               ctx.DeliveryNotes.FirstOrDefault(dn => dn.DeliveryNoteId == id));
   ```

### Caching Strategy

**Cache Read-Heavy Data**:
- Delivery note details (5-minute TTL)
- Customer information (15-minute TTL)
- Product catalog (1-hour TTL)

**Cache Invalidation**:
- On update: Invalidate specific delivery note
- On status change: Invalidate and republish
- On delete: Invalidate immediately

### HTTP Client Optimization

**Connection Pooling**:
```csharp
builder.Services.AddHttpClient<IOrderServiceClient, OrderServiceClient>()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    });
```

**Timeout Configuration**:
- Short timeout for health checks (2s)
- Medium timeout for data operations (10s)
- Long timeout for PDF generation (30s)

## Aspire Integration

### ServiceDefaults Benefits

Automatically configured:
- OpenTelemetry (logs, metrics, traces)
- Health checks (liveness, readiness)
- Service discovery
- Configuration management
- Resilience patterns (retry, circuit breaker)

### AppHost Configuration

```csharp
// In Aspire AppHost
var deliveryDb = postgresServer.AddDatabase("delivery-db");

var deliveryService = builder.AddProject<Projects.Maliev_DeliveryService_Api>("deliveryservice")
    .WithReference(deliveryDb)
    .WithReference(redis)
    .WithReference(messaging)
    .WaitFor(deliveryDb)
    .WaitFor(redis)
    .WaitFor(messaging);

// BFF references DeliveryService
var intranetBff = builder.AddProject<Projects.Maliev_Intranet_Bff>("intranetbff")
    .WithReference(deliveryService);
```

## Summary of Research Outcomes

All "NEEDS CLARIFICATION" items from Technical Context have been resolved:

✅ **Architecture**: New microservice (Maliev.DeliveryService)
✅ **Tech Stack**: .NET 10, C# 13, PostgreSQL 18, MassTransit, Redis
✅ **Sequential IDs**: Database-driven with serializable transactions
✅ **Service Integration**: Graceful degradation with retry
✅ **Observability**: OpenTelemetry (logs, metrics, traces)
✅ **Data Retention**: 7-year retention with 2-year archival threshold
✅ **API Versioning**: URL path versioning (/v1/, /v2/)
✅ **Testing**: Testcontainers with manual test doubles
✅ **Performance**: <200ms p95 API, <50ms p95 DB queries
✅ **Compliance**: Banned libraries avoided, Maliev standards followed

---

**Next Phase**: Create data models, API contracts, and developer quickstart guide
