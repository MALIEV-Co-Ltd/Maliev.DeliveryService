# Research: Barcode Scan Tracking

**Branch**: `002-barcode-scan` | **Date**: 2026-02-21 | **Phase**: 0

---

## Decision 1: Where Does Business Logic Live?

**Decision**: Add `ScanBarcodeAsync` to `IDeliveryNoteService` and implement in `DeliveryNoteService`, not inline in the controller.

**Rationale**: The barcode scan operation requires loading the `DeliveryNote` entity from the database, mutating it, saving it, invalidating the Redis cache, and publishing an event. This is identical in shape to `UpdateStatusAsync`, which is implemented in the service. Implementing this inline in the controller would require injecting `DeliveryDbContext` and `IDistributedCache` directly into the controller — breaking the existing architectural boundary where the controller has no direct data access.

**Alternatives considered**:
- Inline controller logic (like `GeneratePdf`): Rejected because `GeneratePdf` only checks existence via a DTO result and publishes a single event — it never touches the entity. Barcode scan mutates the entity, which requires the service layer.

---

## Decision 2: Exception → HTTP Status Mapping

**Decision**: Map service exceptions to HTTP responses at the controller level:

| Exception | HTTP Status | Trigger |
|-----------|-------------|---------|
| `KeyNotFoundException` | 404 Not Found | Delivery note not found in DB |
| `InvalidOperationException` ("already been dispatched") | 409 Conflict | Status is InTransit, Delivered, PartiallyDelivered, or Cancelled |
| Model validation failure | 400 Bad Request | Handled automatically by `[ApiController]` attribute |
| `ArgumentException` | 400 Bad Request | Empty/whitespace barcode (service-level guard) |

**Rationale**: Follows the exception-mapping pattern already used in `UpdateDeliveryNote`, `DeleteDeliveryNote`, and `UpdateDeliveryStatus`.

---

## Decision 3: DeliveryStatus.PartiallyDelivered — Resolved

**Decision**: Block `PartiallyDelivered` in addition to `InTransit`, `Delivered`, and `Cancelled`.

**Gap identified and resolved**: `DeliveryStatus` has a fifth value: `PartiallyDelivered`. A delivery note in this state has already been partially processed on-site. Scanning a barcode for it would be semantically incorrect (it was already at least partially delivered, so a Flash Express dispatch scan is nonsensical). The analysis phase (`/speckit.analyze`) identified this as a coverage gap. FR-008 has been updated to explicitly include `PartiallyDelivered` in the blocked set.

---

## Decision 4: Cache Invalidation Required

**Decision**: Call `_cache.RemoveAsync($"delivery-note:{deliveryNoteId}", ct)` after `SaveChangesAsync`.

**Rationale**: All other mutation methods in `DeliveryNoteService` (UpdateStatusAsync, UpdateAsync, SoftDeleteAsync) invalidate the Redis cache after saving. Omitting this would return stale DTO data on the next `GetByIdAsync` call.

---

## Decision 5: Event Publishing — Best-Effort via Existing Helper

**Decision**: Use the existing `PublishEventAsync<T>` private helper in `DeliveryNoteService`.

**Rationale**: `PublishEventAsync` already implements the clarified behavior (best-effort: swallows publish exceptions, logs warning, scan still succeeds). Reusing it is consistent with all other events published by the service.

**Event fields** (from existing `DeliveryStatusChangedEvent` usage in `UpdateStatusAsync`):
```
DeliveryNoteId, OrderId, PreviousStatus, NewStatus,
ActualDeliveryTime (null for scan), ReceivedByName (null for scan),
ChangedAt, ChangedBy
```

---

## Decision 6: Route Prefix

**Decision**: The controller uses `delivery/v{version:apiVersion}/delivery-notes`, NOT `/api/delivery-notes/`.

**Finding**: The `[Route]` attribute on `DeliveryNotesController` is `delivery/v{version:apiVersion}/delivery-notes`. The new endpoint will be:
```
POST delivery/v1/delivery-notes/{deliveryNoteId}/barcode-scan
```
The spec's `/api/delivery-notes/{deliveryNoteId}/barcode-scan` was illustrative. The actual route follows the existing versioned prefix.

---

## Decision 7: Authorization

**Decision**: No explicit `[Authorize]` attribute needed on the new action.

**Finding**: The existing controller has no `[Authorize]` attribute at class or action level. Authorization is enforced at the middleware/policy level (configured in `Program.cs`). The new endpoint follows the same pattern.

---

## Decision 8: Test Approach

**Decision**: Write service-level unit tests in `DeliveryNoteServiceTests.cs` (extend existing class), not separate controller unit tests.

**Rationale**: The project has no mocking library (Moq/NSubstitute). All existing unit tests test `DeliveryNoteService` directly using an in-memory EF Core database and hand-crafted fakes (`FakePublishEndpoint`, etc.). Creating controller unit tests would require either a fake `IDeliveryNoteService` (significant boilerplate) or a mocking library (new dependency). Service tests cover all 4 spec scenarios and follow the established pattern.

**Empty barcode test**: The service method will include a defensive `ArgumentException` guard for empty/whitespace/oversized barcode, so test case 4 can be tested at the service level in addition to the automatic model binding validation.

---

## Decision 9: User Identity

**Decision**: Use `User.GetUserId()` from `ClaimsPrincipalExtensions` to obtain the internal user ID.

**Finding**: `ClaimsPrincipalExtensions.GetUserId()` returns `ClaimTypes.NameIdentifier` or `sub` claim value — the internal user ID. This matches the clarification: downstream services resolve full employee identity from the employee service using this ID.
