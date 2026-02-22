# Data Model: Barcode Scan Tracking

**Branch**: `002-barcode-scan` | **Date**: 2026-02-21

---

## Affected Entity: DeliveryNote

No schema migration is required. All fields used by the barcode scan already exist on `DeliveryNote`.

| Field | Type | Nullable | Change |
|-------|------|----------|--------|
| `TrackingNumber` | `string?` | Yes | Written: barcode value (trimmed) |
| `CarrierName` | `string?` | Yes | Written: hardcoded `"Flash Express"` |
| `Status` | `DeliveryStatus` | No | Mutated: `Pending` → `InTransit` |
| `UpdatedAt` | `DateTime?` | Yes | Written: `DateTime.UtcNow` |
| `UpdatedBy` | `string?` | Yes | Written: internal user ID from JWT |

### Status Transition (Barcode Scan)

```
Pending ──[barcode scan]──► InTransit
```

Blocked statuses (return 409):
- `InTransit`
- `Delivered`
- `PartiallyDelivered`
- `Cancelled`

---

## New DTOs

### BarcodeScanRequest

**File**: `Maliev.DeliveryService.Api/DTOs/BarcodeScanRequest.cs`

| Property | Type | Validation |
|----------|------|------------|
| `BarcodeValue` | `string` | `[Required]` only — length validated in service after trimming |

Trimming: The service trims `BarcodeValue` before validation and storage. After trimming: if empty → `ArgumentException` (400); if > 100 chars → `ArgumentException` (400). `[MaxLength(100)]` is omitted from the DTO to avoid pre-trim false passes.

### BarcodeScanResponse

**File**: `Maliev.DeliveryService.Api/DTOs/BarcodeScanResponse.cs`

| Property | Type | Source |
|----------|------|--------|
| `DeliveryNoteId` | `string` | `deliveryNote.DeliveryNoteId` |
| `TrackingNumber` | `string` | `deliveryNote.TrackingNumber` (just set) |
| `CarrierName` | `string` | `"Flash Express"` |
| `Status` | `string` | `"InTransit"` |

---

## New Service Method

### IDeliveryNoteService

```
Task<BarcodeScanResponse> ScanBarcodeAsync(
    string deliveryNoteId,
    string barcodeValue,
    string scannedBy,
    CancellationToken ct = default)
```

**Throws**:
- `KeyNotFoundException` — delivery note not found
- `InvalidOperationException` — status is InTransit, Delivered, PartiallyDelivered, or Cancelled (message: `"This delivery note has already been dispatched."`)
- `ArgumentException` — barcode is empty/whitespace after trimming, or exceeds 100 chars

---

## Published Event: DeliveryStatusChangedEvent

Namespace: `Maliev.MessagingContracts.Contracts.Delivery`

| Field | Value for Barcode Scan |
|-------|----------------------|
| `DeliveryNoteId` | `deliveryNote.DeliveryNoteId` |
| `OrderId` | `deliveryNote.OrderId` |
| `PreviousStatus` | captured before mutation (e.g. `"Pending"`) |
| `NewStatus` | `"InTransit"` |
| `ActualDeliveryTime` | `null` |
| `ReceivedByName` | `null` |
| `ChangedAt` | `DateTime.UtcNow` |
| `ChangedBy` | internal user ID |

Publishing is **best-effort** — failure does not roll back the database save (see research.md Decision 5).
