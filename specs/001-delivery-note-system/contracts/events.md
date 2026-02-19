# Event Contracts

**Feature**: Delivery Note System
**Date**: 2026-02-16

## Overview

This document defines all events published and consumed by the Delivery Note System. Events enable loose coupling with other services and support eventual consistency across the platform.

## Published Events

### DeliveryNoteCreatedEvent

Published when a new delivery note is created.

**Topic**: `delivery-note-created`
**Exchange**: `maliev.delivery.events`

**Schema**:
```csharp
public sealed record DeliveryNoteCreatedEvent
{
    public string DeliveryNoteId { get; init; }
    public string? OrderId { get; init; }
    public int? PurchaseOrderId { get; init; }
    public Guid CustomerId { get; init; }
    public DateTime DeliveryDate { get; init; }
    public int ItemCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; }
}
```

**JSON Example**:
```json
{
  "deliveryNoteId": "DN-2026-000001",
  "orderId": "ORD-2026-001",
  "purchaseOrderId": null,
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "deliveryDate": "2026-02-20T10:00:00Z",
  "itemCount": 5,
  "createdAt": "2026-02-16T14:30:00Z",
  "createdBy": "user@maliev.com"
}
```

**Consumers**:
- **NotificationService**: Sends shipment notification to customer
- **OrderService**: Updates order tracking information (optional)
- **AnalyticsService**: Records delivery note creation metrics

**Retry Policy**:
- Exponential backoff: 1s, 2s, 4s, 8s, 16s
- Max retries: 5
- Dead letter queue after 5 failures

---

### DeliveryStatusChangedEvent

Published when delivery status transitions to a new state.

**Topic**: `delivery-status-changed`
**Exchange**: `maliev.delivery.events`

**Schema**:
```csharp
public sealed record DeliveryStatusChangedEvent
{
    public string DeliveryNoteId { get; init; }
    public string? OrderId { get; init; }
    public string PreviousStatus { get; init; }
    public string NewStatus { get; init; }
    public DateTime? ActualDeliveryTime { get; init; }
    public string? ReceivedByName { get; init; }
    public DateTime ChangedAt { get; init; }
    public string ChangedBy { get; init; }
}
```

**JSON Example**:
```json
{
  "deliveryNoteId": "DN-2026-000001",
  "orderId": "ORD-2026-001",
  "previousStatus": "InTransit",
  "newStatus": "Delivered",
  "actualDeliveryTime": "2026-02-20T14:30:00Z",
  "receivedByName": "John Smith",
  "changedAt": "2026-02-20T14:30:00Z",
  "changedBy": "warehouse@maliev.com"
}
```

**Consumers**:
- **NotificationService**: Sends status update notification to customer
- **OrderService**: Updates order status if delivery complete
- **AnalyticsService**: Tracks delivery performance metrics

**Business Logic**:
- If `newStatus` = "Delivered", consumer may mark order as fulfilled
- If `newStatus` = "PartiallyDelivered", consumer tracks remaining items
- If `newStatus` = "Cancelled", consumer may need to adjust inventory

---

### DeliveryCompletedEvent

Published when a delivery is fully completed (status = "Delivered").

**Topic**: `delivery-completed`
**Exchange**: `maliev.delivery.events`

**Schema**:
```csharp
public sealed record DeliveryCompletedEvent
{
    public string DeliveryNoteId { get; init; }
    public string? OrderId { get; init; }
    public int? PurchaseOrderId { get; init; }
    public DateTime CompletedAt { get; init; }
    public string ReceivedByName { get; init; }
}
```

**JSON Example**:
```json
{
  "deliveryNoteId": "DN-2026-000001",
  "orderId": "ORD-2026-001",
  "purchaseOrderId": null,
  "completedAt": "2026-02-20T14:30:00Z",
  "receivedByName": "John Smith"
}
```

**Consumers**:
- **OrderService**: Updates `ActualDeliveryDate` on order
- **PurchaseOrderService**: Updates delivery status on purchase order
- **InvoiceService**: May trigger invoice generation
- **AnalyticsService**: Records delivery completion metrics

**Idempotency**:
- Consumers must handle duplicate events (same deliveryNoteId)
- Use event ID or deliveryNoteId + timestamp for deduplication

---

### DeliveryNotePdfRequestedEvent

Published when PDF generation is requested.

**Topic**: `delivery-pdf-requested`
**Exchange**: `maliev.delivery.events`

**Schema**:
```csharp
public sealed record DeliveryNotePdfRequestedEvent
{
    public string DeliveryNoteId { get; init; }
    public string RequestedBy { get; init; }
    public DateTime RequestedAt { get; init; }
}
```

**JSON Example**:
```json
{
  "deliveryNoteId": "DN-2026-000001",
  "requestedBy": "user@maliev.com",
  "requestedAt": "2026-02-16T15:00:00Z"
}
```

**Consumers**:
- **PdfService**: Generates bilingual Thai/English PDF
  - Fetches delivery note data via API
  - Generates PDF using template
  - Uploads PDF to Google Cloud Storage
  - Returns PDF URL to DeliveryService (via callback or message)

**Async Pattern**:
1. DeliveryService publishes event
2. PdfService consumes and processes
3. PdfService publishes `DeliveryNotePdfGeneratedEvent` (response)
4. DeliveryService consumes response and returns URL to client

---

## Consumed Events

### OrderCompletedEvent (Optional)

**Description**: Auto-create delivery note draft when order is completed.

**Source**: OrderService
**Topic**: `order-completed`
**Exchange**: `maliev.order.events`

**Schema** (assumed):
```csharp
public sealed record OrderCompletedEvent
{
    public string OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public DateTime CompletedAt { get; init; }
    public List<OrderLineItem> Items { get; init; }
}

public sealed record OrderLineItem
{
    public string ProductCode { get; init; }
    public string ProductName { get; init; }
    public decimal QuantityOrdered { get; init; }
    public decimal QuantityManufactured { get; init; }
    public string UnitOfMeasure { get; init; }
}
```

**Consumer Logic**:
```csharp
public class OrderCompletedEventConsumer : IConsumer<OrderCompletedEvent>
{
    public async Task Consume(ConsumeContext<OrderCompletedEvent> context)
    {
        var orderEvent = context.Message;

        // Auto-create delivery note in "Pending" status
        var deliveryNote = new CreateDeliveryNoteRequest
        {
            OrderId = orderEvent.OrderId,
            DeliveryDate = DateTime.UtcNow.AddDays(1), // Next day
            Items = orderEvent.Items.Select(item => new CreateDeliveryNoteItemRequest
            {
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                QuantityOrdered = item.QuantityOrdered,
                QuantityManufactured = item.QuantityManufactured,
                QuantityDelivered = item.QuantityManufactured, // Default: deliver all
                UnitOfMeasure = item.UnitOfMeasure
            }).ToList()
        };

        await _deliveryNoteService.CreateDeliveryNoteAsync(
            deliveryNote,
            "system-auto",
            context.CancellationToken);
    }
}
```

**Idempotency**: Use OrderId to prevent duplicate delivery notes

---

## Event Publishing Strategy

### Publish After Commit

**Pattern**: Outbox Pattern

```csharp
public async Task<DeliveryNoteResponse> CreateDeliveryNoteAsync(...)
{
    using var transaction = await _context.Database.BeginTransactionAsync();

    // 1. Save delivery note to database
    _context.DeliveryNotes.Add(deliveryNote);
    await _context.SaveChangesAsync();

    // 2. Commit transaction
    await transaction.CommitAsync();

    // 3. Publish event AFTER commit succeeds
    try
    {
        await _publishEndpoint.Publish(new DeliveryNoteCreatedEvent
        {
            DeliveryNoteId = deliveryNote.DeliveryNoteId,
            // ... other fields
        });
    }
    catch (Exception ex)
    {
        // Log error and queue for retry
        _logger.LogWarning(ex, "Failed to publish DeliveryNoteCreatedEvent");
        await _retryQueue.EnqueueAsync(deliveryNoteCreatedEvent);
    }

    return deliveryNote.ToResponse();
}
```

### Retry Queue

Failed event publishes are queued for retry:

1. **First Attempt**: Immediate publish after commit
2. **On Failure**: Add to retry queue (database table or Redis)
3. **Background Worker**: Processes retry queue every 30 seconds
4. **Exponential Backoff**: 1s, 2s, 4s, 8s, 16s
5. **Dead Letter Queue**: After 5 failures, move to DLQ for manual investigation

### Monitoring

**Metrics**:
- Event publish success rate
- Event publish latency (p50, p95, p99)
- Retry queue depth
- Dead letter queue count
- Event processing latency (consumer side)

**Alerts**:
- Event publish failure rate > 5%
- Retry queue depth > 100
- Dead letter queue has items
- Event processing latency > 10 seconds

---

## Event Schema Evolution

### Versioning Strategy

**Backward Compatible Changes** (no version bump):
- Adding optional fields
- Deprecating fields (keep publishing for 6 months)

**Breaking Changes** (new event type):
- Removing required fields
- Changing field types
- Renaming fields
- Changing event semantics

**Migration Example**:
```csharp
// Old event (v1)
public sealed record DeliveryNoteCreatedEvent
{
    public string DeliveryNoteId { get; init; }
    public string OrderId { get; init; } // Made optional in v2
}

// New event (v2) - backward compatible
public sealed record DeliveryNoteCreatedEvent
{
    public string DeliveryNoteId { get; init; }
    public string? OrderId { get; init; }           // Now nullable
    public int? PurchaseOrderId { get; init; }      // New field
}
```

### Consumer Compatibility

Consumers must handle:
1. **Missing optional fields** (nullable properties)
2. **Unknown fields** (ignore extra properties during deserialization)
3. **Deprecated fields** (continue processing during deprecation period)

---

## Testing Events

### Unit Testing

```csharp
[Fact]
public async Task CreateDeliveryNote_PublishesDeliveryNoteCreatedEvent()
{
    // Arrange
    var fakePublisher = new FakePublishEndpoint();
    var service = new DeliveryNoteService(_context, fakePublisher, _idGenerator);

    // Act
    await service.CreateDeliveryNoteAsync(request, "user", ct);

    // Assert
    var publishedEvent = fakePublisher.PublishedMessages
        .OfType<DeliveryNoteCreatedEvent>()
        .Single();

    publishedEvent.DeliveryNoteId.Should().StartWith("DN-2026-");
    publishedEvent.ItemCount.Should().Be(request.Items.Count);
}
```

### Integration Testing

```csharp
[Fact]
public async Task DeliveryNoteCreated_NotificationServiceReceivesEvent()
{
    // Arrange
    var rabbitMqContainer = new RabbitMqContainer();
    await rabbitMqContainer.StartAsync();

    // Act
    await _client.PostAsJsonAsync("/delivery/v1/delivery-notes", request);

    // Assert
    // Wait for event to be consumed
    await Task.Delay(1000);

    // Verify notification was sent
    var sentNotifications = _notificationServiceFake.SentNotifications;
    sentNotifications.Should().ContainSingle(n => n.Type == "ShipmentCreated");
}
```

---

## Event Traceability

All events include correlation IDs for distributed tracing:

**MassTransit Configuration**:
```csharp
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        // Enable OpenTelemetry tracing
        cfg.ConfigurePublish(p => p.UseExecute(ctx =>
        {
            ctx.Headers.Set("TraceId", Activity.Current?.Id);
            ctx.Headers.Set("UserId", currentUser.Id);
        }));
    });
});
```

**Trace Correlation**:
- Each published event carries TraceId from current span
- Consumers create child spans with same TraceId
- Full distributed trace visible in Aspire dashboard

---

**Summary**:
- 4 published events (created, status changed, completed, PDF requested)
- 1 consumed event (order completed - optional)
- Outbox pattern ensures reliability
- Retry queue handles transient failures
- Schema evolution supports backward compatibility
- OpenTelemetry enables end-to-end tracing
