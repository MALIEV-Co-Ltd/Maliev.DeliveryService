# Tasks: Barcode Scan Tracking

**Input**: Design documents from `/specs/002-barcode-scan/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Included — the spec explicitly lists 4 required test cases.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1–US4)
- Exact file paths included in all task descriptions

---

## Phase 1: Foundational (Blocking Prerequisites)

**Purpose**: DTOs and interface contract that ALL user stories depend on. No implementation
work can start until T003 is complete (the interface method defines the contract the
controller and tests depend on).

**⚠️ CRITICAL**: Phases 2–4 cannot begin until this phase is complete.

- [x] T001 [P] Create `Maliev.DeliveryService.Api/DTOs/BarcodeScanRequest.cs` — `BarcodeValue` property with `[Required]` attribute only (no `[MaxLength]` on DTO — length is validated in the service after trimming), namespace `Maliev.DeliveryService.Api.DTOs`
- [x] T002 [P] Create `Maliev.DeliveryService.Api/DTOs/BarcodeScanResponse.cs` — properties `DeliveryNoteId`, `TrackingNumber`, `CarrierName`, `Status` (all `string`), namespace `Maliev.DeliveryService.Api.DTOs`
- [x] T003 Add `Task<BarcodeScanResponse> ScanBarcodeAsync(string deliveryNoteId, string barcodeValue, string scannedBy, CancellationToken ct = default)` to `Maliev.DeliveryService.Api/Services/IDeliveryNoteService.cs` (depends on T002 for return type)

**Checkpoint**: DTOs exist, interface compiles — implementation and test tasks can now begin in parallel.

---

## Phase 2: User Story 1 — Scan Barcode Happy Path (Priority: P1) 🎯 MVP

**Goal**: An employee scans a Flash Express barcode and the delivery note's tracking number,
carrier, status, and audit fields are updated. A `DeliveryStatusChangedEvent` is published.
The caller receives a 200 OK with the updated summary.

**Independent Test**: Seed a `DeliveryNote` in Pending status → call `ScanBarcodeAsync` with
a valid barcode → assert `TrackingNumber`, `CarrierName = "Flash Express"`, `Status = InTransit`,
`UpdatedBy` on the entity, and that `DeliveryStatusChangedEvent` was published with the correct
`PreviousStatus`, `NewStatus`, and `ChangedBy`.

### Implementation for User Story 1

- [x] T004 [P] [US1] Implement `ScanBarcodeAsync` in `Maliev.DeliveryService.Api/Services/DeliveryNoteService.cs`: trim barcodeValue, throw `ArgumentException` if empty after trim or > 100 chars, load entity with `FirstOrDefaultAsync`, throw `KeyNotFoundException` if null, throw `InvalidOperationException("This delivery note has already been dispatched.")` if Status is InTransit/Delivered/PartiallyDelivered/Cancelled, capture previousStatus, set `TrackingNumber = trimmed`, `CarrierName = "Flash Express"`, `Status = InTransit`, `UpdatedAt = DateTime.UtcNow`, `UpdatedBy = scannedBy`, call `SaveChangesAsync`, call `_cache.RemoveAsync($"delivery-note:{deliveryNoteId}", ct)`, call `PublishEventAsync(new DeliveryStatusChangedEvent { ... }, ct)`, return new `BarcodeScanResponse`
- [x] T005 [P] [US1] Add `ScanBarcode` action to `Maliev.DeliveryService.Api/Controllers/DeliveryNotesController.cs`: `[HttpPost("{deliveryNoteId}/barcode-scan")]`, `[ProducesResponseType(typeof(BarcodeScanResponse), 200)]` + 400 + 404 + 409, call `User.GetUserId()`, call `_deliveryNoteService.ScanBarcodeAsync(deliveryNoteId, request.BarcodeValue, userId, ct)`, return `Ok(result)`, catch `KeyNotFoundException` → `NotFound()`, catch `InvalidOperationException` → `Conflict(new { error = ex.Message })`, catch `ArgumentException` → `BadRequest(new { error = ex.Message })` — Note: no explicit `[Authorize]` attribute; auth is enforced at middleware level consistent with all other actions in this controller (research.md Decision 7)
- [x] T006 [US1] Add private `SeedDeliveryNoteAsync(DeliveryStatus status = DeliveryStatus.Pending)` helper to `Maliev.DeliveryService.Tests/Unit/Services/DeliveryNoteServiceTests.cs` — creates and saves a minimal `DeliveryNote` entity directly via `_context` (not via `CreateAsync`), returns the generated `DeliveryNoteId`
- [x] T007 [US1] Add `ScanBarcodeAsync_PendingNote_SetsTrackingAndTransitionsToInTransit` to `Maliev.DeliveryService.Tests/Unit/Services/DeliveryNoteServiceTests.cs`: seed Pending note, call `_service.ScanBarcodeAsync(id, "TH123456789XY", "user-42")`, assert `result.TrackingNumber == "TH123456789XY"`, `result.CarrierName == "Flash Express"`, `result.Status == "InTransit"`, assert entity in `_context` reflects all 5 mutations, assert `FakePublishEndpoint` has one `DeliveryStatusChangedEvent` with `PreviousStatus == "Pending"`, `NewStatus == "InTransit"`, `ChangedBy == "user-42"`

**Checkpoint**: `ScanBarcodeAsync` implemented and passing US1 test. The endpoint is fully functional for the happy path. MVP deliverable.

---

## Phase 3: User Story 2 — Prevent Double-Scanning Dispatched Notes (Priority: P2)

**Goal**: If the delivery note is already In Transit, Delivered, Partially Delivered, or Cancelled, the scan is
rejected with a `409 Conflict` and no data is changed.

**Independent Test**: Seed a `DeliveryNote` in `InTransit` status → call `ScanBarcodeAsync` →
assert `InvalidOperationException` with message `"This delivery note has already been dispatched."`.
The guard logic is already present in the service from T004; this phase adds the confirming test.

### Implementation for User Story 2

- [x] T008 [US2] Add `ScanBarcodeAsync_InTransitNote_ThrowsInvalidOperationException` to `Maliev.DeliveryService.Tests/Unit/Services/DeliveryNoteServiceTests.cs`: seed note with `DeliveryStatus.InTransit`, assert `InvalidOperationException` is thrown, assert message equals `"This delivery note has already been dispatched."`

**Checkpoint**: Guard behaviour confirmed by test. No data-change risk from double-scan.

---

## Phase 4: User Story 3 + User Story 4 — Input Validation & Not Found (Both Priority: P3)

**Goal (US3)**: Empty or oversized barcode values are rejected before any database access.
**Goal (US4)**: A non-existent delivery note ID produces a clear not-found error.

**Independent Test (US3)**: Call `ScanBarcodeAsync` with a whitespace-only string →
assert `ArgumentException` mentioning `"must not be empty"`.

**Independent Test (US4)**: Call `ScanBarcodeAsync` with `"DN-DOES-NOT-EXIST"` →
assert `KeyNotFoundException`.

### Implementation for User Story 3

- [x] T009 [US3] Add `ScanBarcodeAsync_EmptyBarcode_ThrowsArgumentException` to `Maliev.DeliveryService.Tests/Unit/Services/DeliveryNoteServiceTests.cs`: seed Pending note, call `ScanBarcodeAsync` with `"   "` (whitespace only), assert `ArgumentException` thrown, assert `ex.Message` contains `"must not be empty"`

### Implementation for User Story 4

- [x] T010 [US4] Add `ScanBarcodeAsync_NonExistentNote_ThrowsKeyNotFoundException` to `Maliev.DeliveryService.Tests/Unit/Services/DeliveryNoteServiceTests.cs`: call `ScanBarcodeAsync("DN-DOES-NOT-EXIST", "TH123456789XY", "user-42")` with no seeded note, assert `KeyNotFoundException` thrown

**Checkpoint**: All 4 spec test cases passing. All user stories fully covered.

---

## Phase 5: Polish & Build Verification

**Purpose**: Confirm the complete change set compiles cleanly and no pre-existing tests are broken.

- [x] T011 Run `dotnet build Maliev.DeliveryService.slnx` from the repository root — zero errors required before proceeding
- [x] T012 Run `dotnet test Maliev.DeliveryService.slnx` from the repository root — all pre-existing tests plus the 4 new test methods must pass

**Note on SC-001** (employees record tracking number in under 10 seconds): this criterion is validated through operational monitoring and manual testing against the running system — not covered by unit tests. No additional task needed here.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 1)**: No dependencies — start immediately
  - T001 and T002 are fully parallel (different files)
  - T003 depends on T002 (return type)
- **US1 (Phase 2)**: Requires Phase 1 complete
  - T004 and T005 are parallel (different files, both depend on T001 + T002 + T003)
  - T006 can start as soon as Phase 1 is complete (independent of T004/T005)
  - T007 depends on T004 and T006
- **US2 (Phase 3)**: Requires T004 (service method) and T006 (seed helper)
- **US3+US4 (Phase 4)**: Requires T004 and T006; T009 and T010 touch the same test file — do sequentially if solo
- **Polish (Phase 5)**: Requires all prior phases complete

### User Story Dependencies

- **US1 (P1)**: Starts after Phase 1 — no dependency on other stories
- **US2 (P2)**: Can start after T004 and T006 are done — no dependency on US1 test passing
- **US3 (P3)**: Can start after T004 and T006 — parallel with US2 and US4
- **US4 (P3)**: Can start after T004 and T006 — parallel with US2 and US3

### Within Each Phase

- DTOs before interface (T001/T002 before T003)
- Interface before service and controller implementation (T003 before T004/T005)
- Seed helper before test methods (T006 before T007/T008/T009/T010)
- Service method must be complete before build verification (T004 before T011)

---

## Parallel Opportunities

### Phase 1

```
T001 (BarcodeScanRequest.cs)  ──┐
                                ├──► T003 (IDeliveryNoteService)
T002 (BarcodeScanResponse.cs) ──┘
```

### Phase 2 (after Phase 1 complete)

```
T004 (DeliveryNoteService)     ──┐
                                 ├──► T007 (US1 test)
T005 (DeliveryNotesController)   │
                                 │
T006 (SeedDeliveryNoteAsync) ───┘
```

### Phase 3 + 4 (after T004 + T006 complete)

```
T008 (US2 test) ─────┐
T009 (US3 test) ─────┤──► T011 build ──► T012 test
T010 (US4 test) ─────┘
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Foundational (T001–T003)
2. Complete Phase 2: User Story 1 (T004–T007)
3. **STOP and VALIDATE**: `dotnet test --filter ScanBarcodeAsync_PendingNote`
4. The happy-path endpoint is live and testable — this is the MVP

### Incremental Delivery

1. Phase 1 → Foundation ready
2. Phase 2 → Happy path works (US1) — **deploy/demo**
3. Phase 3 → Guard confirmed (US2)
4. Phase 4 → Validation + not-found confirmed (US3/US4)
5. Phase 5 → Full build + test gate passes — **production ready**

### Single-Developer Sequence

```
T001 → T002 → T003 → T004 → T005 → T006 → T007 → T008 → T009 → T010 → T011 → T012
```

---

## Notes

- T004 and T005 touch different files and can be done in parallel by two developers
- T009 and T010 touch the same test class file — do them sequentially if solo, or parallel on separate branches
- The guard logic for US2, US3, US4 is implemented inside `ScanBarcodeAsync` (T004). Phases 3 and 4 only add test coverage — no new implementation code
- `[P]` marks tasks with no intra-phase file conflicts; T009 and T010 are NOT marked `[P]` because they both write to the same test class file
- Each phase checkpoint delivers an independently testable increment
