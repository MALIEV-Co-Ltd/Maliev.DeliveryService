# Implementation Plan: Barcode Scan Tracking

**Branch**: `002-barcode-scan` | **Date**: 2026-02-21 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-barcode-scan/spec.md`

---

## Summary

Add `POST delivery/v1/delivery-notes/{deliveryNoteId}/barcode-scan` to the existing
`DeliveryNotesController`. The endpoint accepts a scanned barcode value, validates it,
loads the `DeliveryNote` entity, guards against re-dispatching, sets `TrackingNumber`,
`CarrierName = "Flash Express"`, `Status = InTransit`, and audit fields, saves to PostgreSQL,
invalidates the Redis cache, and publishes `DeliveryStatusChangedEvent` via MassTransit
(best-effort). Business logic lives in `DeliveryNoteService` following the established
service-layer pattern. Four unit tests cover all spec scenarios.

---

## Technical Context

**Language/Version**: C# 13, .NET 10
**Primary Dependencies**: ASP.NET Core, Entity Framework Core 10, MassTransit, StackExchange.Redis (via `IDistributedCache`)
**Storage**: PostgreSQL (via EF Core); Redis for distributed caching
**Testing**: xUnit, in-memory EF Core, `FakePublishEndpoint` (no mocking library)
**Target Platform**: Linux container (Docker)
**Performance Goals**: Response within 10 seconds end-to-end (user-facing, not server-side latency)
**Constraints**: No new DB migrations; no new entity types; no new event types
**Scale/Scope**: Single endpoint addition; ~6 files touched

---

## Constitution Check

*No constitution.md found in `.specify/memory/`. Proceeding without gate validation.*

Voluntary checks applied:
- ✅ No new projects added (single project modified)
- ✅ No new entity types or DB migrations
- ✅ No new event types (reuses `DeliveryStatusChangedEvent`)
- ✅ Follows established exception/response mapping pattern
- ✅ Follows established service-layer pattern

---

## Project Structure

### Documentation (this feature)

```text
specs/002-barcode-scan/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 — decisions and findings
├── data-model.md        # Phase 1 — affected entities and new DTOs
├── quickstart.md        # Phase 1 — build/run/test guide
├── contracts/
│   └── barcode-scan-endpoint.yaml   # OpenAPI contract
└── checklists/
    └── requirements.md  # Spec quality checklist
```

### Source Code

```text
Maliev.DeliveryService.Api/
├── DTOs/
│   ├── BarcodeScanRequest.cs        [CREATE]
│   └── BarcodeScanResponse.cs       [CREATE]
├── Services/
│   ├── IDeliveryNoteService.cs      [EDIT — add ScanBarcodeAsync]
│   └── DeliveryNoteService.cs       [EDIT — implement ScanBarcodeAsync]
└── Controllers/
    └── DeliveryNotesController.cs   [EDIT — add ScanBarcode action]

Maliev.DeliveryService.Tests/
└── Unit/
    └── Services/
        └── DeliveryNoteServiceTests.cs  [EDIT — add 4 test methods]
```

**Structure Decision**: Single-project modification. No new projects, namespaces, or directories.

---

## Phase 1: DTOs

### Step 1.1 — Create `BarcodeScanRequest.cs`

**File**: `Maliev.DeliveryService.Api/DTOs/BarcodeScanRequest.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace Maliev.DeliveryService.Api.DTOs;

public class BarcodeScanRequest
{
    [Required]
    public string BarcodeValue { get; set; } = string.Empty;
}
```

**Notes**:
- `[Required]` causes model binding to reject null → automatic 400.
- `[MaxLength(100)]` is intentionally omitted from the DTO: model binding validates before the service trims whitespace, so a 100-char all-whitespace value would pass the DTO check but produce an empty string after trimming. The service guards both empty-after-trim and length-after-trim with `ArgumentException`.

---

### Step 1.2 — Create `BarcodeScanResponse.cs`

**File**: `Maliev.DeliveryService.Api/DTOs/BarcodeScanResponse.cs`

```csharp
namespace Maliev.DeliveryService.Api.DTOs;

public class BarcodeScanResponse
{
    public string DeliveryNoteId { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public string CarrierName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
```

---

## Phase 2: Service Layer

### Step 2.1 — Add `ScanBarcodeAsync` to `IDeliveryNoteService`

**File**: `Maliev.DeliveryService.Api/Services/IDeliveryNoteService.cs`

Add one method to the interface:

```csharp
Task<BarcodeScanResponse> ScanBarcodeAsync(
    string deliveryNoteId,
    string barcodeValue,
    string scannedBy,
    CancellationToken ct = default);
```

---

### Step 2.2 — Implement `ScanBarcodeAsync` in `DeliveryNoteService`

**File**: `Maliev.DeliveryService.Api/Services/DeliveryNoteService.cs`

Add the method after `UpdateStatusAsync`. Full implementation:

```csharp
public async Task<BarcodeScanResponse> ScanBarcodeAsync(
    string deliveryNoteId,
    string barcodeValue,
    string scannedBy,
    CancellationToken ct = default)
{
    // Trim before validation (clarification: store trimmed value)
    var trimmedBarcode = barcodeValue?.Trim() ?? string.Empty;

    if (string.IsNullOrEmpty(trimmedBarcode))
        throw new ArgumentException("Barcode value must not be empty.");

    if (trimmedBarcode.Length > 100)
        throw new ArgumentException("Barcode value must not exceed 100 characters.");

    var deliveryNote = await _context.DeliveryNotes
        .FirstOrDefaultAsync(dn => dn.DeliveryNoteId == deliveryNoteId, ct);

    if (deliveryNote == null)
        throw new KeyNotFoundException($"Delivery note {deliveryNoteId} not found.");

    if (deliveryNote.Status is DeliveryStatus.InTransit
                            or DeliveryStatus.Delivered
                            or DeliveryStatus.PartiallyDelivered
                            or DeliveryStatus.Cancelled)
    {
        throw new InvalidOperationException("This delivery note has already been dispatched.");
    }

    var previousStatus = deliveryNote.Status;

    deliveryNote.TrackingNumber = trimmedBarcode;
    deliveryNote.CarrierName = "Flash Express";
    deliveryNote.Status = DeliveryStatus.InTransit;
    deliveryNote.UpdatedAt = DateTime.UtcNow;
    deliveryNote.UpdatedBy = scannedBy;

    await _context.SaveChangesAsync(ct);

    _logger.LogInformation(
        "Barcode scanned for delivery note {DeliveryNoteId}: TrackingNumber={TrackingNumber}, ScannedBy={ScannedBy}",
        deliveryNoteId, trimmedBarcode, scannedBy);

    // Invalidate cache
    var cacheKey = $"delivery-note:{deliveryNoteId}";
    await _cache.RemoveAsync(cacheKey, ct);

    // Publish DeliveryStatusChangedEvent (best-effort)
    await PublishEventAsync(new DeliveryStatusChangedEvent
    {
        DeliveryNoteId = deliveryNote.DeliveryNoteId,
        OrderId = deliveryNote.OrderId,
        PreviousStatus = previousStatus.ToString(),
        NewStatus = DeliveryStatus.InTransit.ToString(),
        ChangedAt = deliveryNote.UpdatedAt ?? DateTime.UtcNow,
        ChangedBy = scannedBy
    }, ct);

    return new BarcodeScanResponse
    {
        DeliveryNoteId = deliveryNote.DeliveryNoteId,
        TrackingNumber = deliveryNote.TrackingNumber,
        CarrierName = deliveryNote.CarrierName!,
        Status = deliveryNote.Status.ToString()
    };
}
```

**Key points**:
- Uses the existing `PublishEventAsync` helper (best-effort, no rollback on publish failure).
- Uses the existing `_cache` for invalidation.
- Throws `KeyNotFoundException` for 404, `InvalidOperationException` for 409.
- Trims barcode value before storing (per clarification).
- Does NOT set `ActualDeliveryTime` or `ReceivedByName` (those are delivery-confirmation fields, not applicable to dispatch).

---

## Phase 3: Controller Endpoint

### Step 3.1 — Add `ScanBarcode` action to `DeliveryNotesController`

**File**: `Maliev.DeliveryService.Api/Controllers/DeliveryNotesController.cs`

Add after the `GeneratePdf` action (or at end of file before closing brace):

```csharp
/// <summary>
/// Record the tracking number by scanning a Flash Express shipping label barcode
/// </summary>
[HttpPost("{deliveryNoteId}/barcode-scan")]
[ProducesResponseType(typeof(BarcodeScanResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status409Conflict)]
public async Task<ActionResult<BarcodeScanResponse>> ScanBarcode(
    [FromRoute] string deliveryNoteId,
    [FromBody] BarcodeScanRequest request,
    CancellationToken ct)
{
    try
    {
        var userId = User.GetUserId();
        var result = await _deliveryNoteService.ScanBarcodeAsync(
            deliveryNoteId, request.BarcodeValue, userId, ct);

        return Ok(result);
    }
    catch (KeyNotFoundException)
    {
        return NotFound();
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogWarning(ex,
            "Barcode scan rejected for delivery note {DeliveryNoteId}: already dispatched", deliveryNoteId);
        return Conflict(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        _logger.LogWarning(ex,
            "Invalid barcode scan request for delivery note {DeliveryNoteId}", deliveryNoteId);
        return BadRequest(new { error = ex.Message });
    }
}
```

**Notes**:
- Route: `delivery/v1/delivery-notes/{deliveryNoteId}/barcode-scan` (versioned prefix from class attribute).
- Model validation (`[Required]`, `[MaxLength(100)]`) produces 400 automatically via `[ApiController]` before the action runs.
- No `[Authorize]` attribute — consistent with existing actions in this controller.

---

## Phase 4: Unit Tests

### Step 4.1 — Add 4 test methods to `DeliveryNoteServiceTests.cs`

**File**: `Maliev.DeliveryService.Tests/Unit/Services/DeliveryNoteServiceTests.cs`

Add the following 4 test methods to the existing `DeliveryNoteServiceTests` class.

**Helper**: All tests that need a `DeliveryNote` seed it directly into `_context` to avoid
coupling to `CreateAsync` logic:

```csharp
private async Task<string> SeedDeliveryNoteAsync(DeliveryStatus status = DeliveryStatus.Pending)
{
    var id = $"DN-TEST-{Guid.NewGuid():N}";
    _context.DeliveryNotes.Add(new DeliveryNote
    {
        DeliveryNoteId = id,
        OrderId = "ORD-TEST-001",
        CustomerId = Guid.NewGuid(),
        DeliveryDate = DateTime.UtcNow.AddDays(1),
        Status = status,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "seed-user"
    });
    await _context.SaveChangesAsync();
    return id;
}
```

**Test 1: Happy path**

```csharp
[Fact]
public async Task ScanBarcodeAsync_PendingNote_SetsTrackingAndTransitionsToInTransit()
{
    // Arrange
    var deliveryNoteId = await SeedDeliveryNoteAsync(DeliveryStatus.Pending);
    _fakePublishEndpoint.Clear();

    // Act
    var result = await _service.ScanBarcodeAsync(deliveryNoteId, "TH123456789XY", "user-42");

    // Assert
    Assert.Equal(deliveryNoteId, result.DeliveryNoteId);
    Assert.Equal("TH123456789XY", result.TrackingNumber);
    Assert.Equal("Flash Express", result.CarrierName);
    Assert.Equal("InTransit", result.Status);

    var entity = await _context.DeliveryNotes.FindAsync(deliveryNoteId);
    Assert.NotNull(entity);
    Assert.Equal(DeliveryStatus.InTransit, entity!.Status);
    Assert.Equal("TH123456789XY", entity.TrackingNumber);
    Assert.Equal("Flash Express", entity.CarrierName);
    Assert.Equal("user-42", entity.UpdatedBy);

    var events = _fakePublishEndpoint
        .GetPublishedMessages<Maliev.MessagingContracts.Contracts.Delivery.DeliveryStatusChangedEvent>();
    Assert.Single(events);
    Assert.Equal("Pending", events[0].PreviousStatus);
    Assert.Equal("InTransit", events[0].NewStatus);
    Assert.Equal("user-42", events[0].ChangedBy);
}
```

**Test 2: Already dispatched → 409**

```csharp
[Fact]
public async Task ScanBarcodeAsync_InTransitNote_ThrowsInvalidOperationException()
{
    // Arrange
    var deliveryNoteId = await SeedDeliveryNoteAsync(DeliveryStatus.InTransit);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<InvalidOperationException>(
        () => _service.ScanBarcodeAsync(deliveryNoteId, "TH123456789XY", "user-42"));

    Assert.Equal("This delivery note has already been dispatched.", ex.Message);
}
```

**Test 3: Not found → 404**

```csharp
[Fact]
public async Task ScanBarcodeAsync_NonExistentNote_ThrowsKeyNotFoundException()
{
    // Act & Assert
    await Assert.ThrowsAsync<KeyNotFoundException>(
        () => _service.ScanBarcodeAsync("DN-DOES-NOT-EXIST", "TH123456789XY", "user-42"));
}
```

**Test 4: Empty barcode → 400**

```csharp
[Fact]
public async Task ScanBarcodeAsync_EmptyBarcode_ThrowsArgumentException()
{
    // Arrange
    var deliveryNoteId = await SeedDeliveryNoteAsync(DeliveryStatus.Pending);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<ArgumentException>(
        () => _service.ScanBarcodeAsync(deliveryNoteId, "   ", "user-42"));

    Assert.Contains("must not be empty", ex.Message);
}
```

---

## Phase 5: Build Verification

```bash
# Step 1: Build — must produce zero errors
dotnet build Maliev.DeliveryService.slnx

# Step 2: Test — all tests must pass (including pre-existing)
dotnet test Maliev.DeliveryService.slnx
```

Both must succeed before the feature is considered complete.

---

## Implementation Sequence

Execute steps in this order to keep the build green at each stage:

1. Create `BarcodeScanRequest.cs` and `BarcodeScanResponse.cs` (no compile dependencies yet)
2. Add `ScanBarcodeAsync` to `IDeliveryNoteService.cs`
3. Implement `ScanBarcodeAsync` in `DeliveryNoteService.cs` (now satisfies the interface)
4. Add `ScanBarcode` action to `DeliveryNotesController.cs`
5. Add 4 test methods to `DeliveryNoteServiceTests.cs`
6. `dotnet build` — verify zero errors
7. `dotnet test` — verify all tests pass

---

## Risks & Notes

| Risk | Mitigation |
|------|------------|
| `PartiallyDelivered` status not blocked per spec | Documented in research.md; add to backlog for future discussion |
| Event publish failure | Already handled by `PublishEventAsync` (best-effort, swallows exception, logs warning) |
| Cache stale if `RemoveAsync` fails | Redis failure would be rare and Redis is already used project-wide; acceptable risk |
| Concurrent scans for same note | Second scan gets 409 (status already InTransit after first save) — no special handling needed |
