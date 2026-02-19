# Tasks: Delivery Note System

**Input**: Design documents from `/specs/001-delivery-note-system/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, work-package.md

**Tests**: Comprehensive test suite included (Phase 10)

**Organization**: Tasks grouped by user story to enable independent implementation and testing

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Based on plan.md structure:
- **API Project**: `Maliev.DeliveryService.Api\`
- **Data Project**: `Maliev.DeliveryService.Data\`
- **Tests Project**: `Maliev.DeliveryService.Tests\`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Create solution file Maliev.DeliveryService.slnx with .NET 10 target
- [ ] T002 [P] Create Maliev.DeliveryService.Api project with ASP.NET Core 10.0 Web API template
- [ ] T003 [P] Create Maliev.DeliveryService.Data project as class library with EF Core 10.0
- [ ] T004 [P] Create Maliev.DeliveryService.Tests project with xUnit and FluentAssertions
- [ ] T005 Add project references (Api -> Data, Tests -> Api, Tests -> Data)
- [ ] T006 Add project reference to Maliev.MessagingContracts from Api project for shared event contracts
- [ ] T007 [P] Install NuGet packages in Api: Aspire.ServiceDefaults, MassTransit.RabbitMQ, StackExchange.Redis, Scalar.AspNetCore, Asp.Versioning.Http
- [ ] T008 [P] Install NuGet packages in Data: Npgsql.EntityFrameworkCore.PostgreSQL 18.x
- [ ] T009 [P] Install NuGet packages in Api: Google.Cloud.Storage.V1 for file storage integration
- [ ] T010 [P] Install NuGet packages in Tests: Testcontainers.PostgreSql, Testcontainers.RabbitMq, Microsoft.AspNetCore.Mvc.Testing
- [ ] T011 Configure appsettings.json in Api project with ConnectionStrings, RabbitMQ, Redis, GoogleCloudStorage sections
- [ ] T012 Configure Program.cs in Api project with Aspire ServiceDefaults integration

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T013 [P] Create DeliveryNote entity in Maliev.DeliveryService.Data\Entities\DeliveryNote.cs with properties per data-model.md
- [ ] T014 [P] Create DeliveryNoteItem entity in Maliev.DeliveryService.Data\Entities\DeliveryNoteItem.cs
- [ ] T015 [P] Create DeliveryNoteFile entity in Maliev.DeliveryService.Data\Entities\DeliveryNoteFile.cs
- [ ] T016 [P] Create Address entity in Maliev.DeliveryService.Data\Entities\Address.cs as separate table (not owned entity)
- [ ] T017 [P] Create DeliveryStatus enum in Maliev.DeliveryService.Data\Entities\DeliveryStatus.cs (Pending, InTransit, Delivered, PartiallyDelivered, Cancelled)
- [ ] T018 [P] Create FileType enum in Maliev.DeliveryService.Data\Entities\FileType.cs (Signature, Photo, PackingList, Invoice, Other)
- [ ] T019 Create DeliveryNoteConfiguration in Maliev.DeliveryService.Data\Configurations\DeliveryNoteConfiguration.cs implementing IEntityTypeConfiguration
- [ ] T020 [P] Create DeliveryNoteItemConfiguration in Maliev.DeliveryService.Data\Configurations\DeliveryNoteItemConfiguration.cs with check constraints: quantity_ordered >= 0, quantity_manufactured >= 0, quantity_delivered > 0, quantity_delivered <= quantity_manufactured
- [ ] T021 [P] Create DeliveryNoteFileConfiguration in Maliev.DeliveryService.Data\Configurations\DeliveryNoteFileConfiguration.cs
- [ ] T022 [P] Create AddressConfiguration in Maliev.DeliveryService.Data\Configurations\AddressConfiguration.cs
- [ ] T023 Create DeliveryDbContext in Maliev.DeliveryService.Data\DeliveryDbContext.cs with DbSets and global query filters for soft delete
- [ ] T024 Add initial EF migration in Maliev.DeliveryService.Data\Migrations\XXXXXX_Initial.cs using dotnet ef migrations add
- [ ] T025 Create DeliveryNoteIdGenerator service in Maliev.DeliveryService.Api\Services\DeliveryNoteIdGenerator.cs with sequential ID logic per research.md
- [ ] T026 [P] Create IDeliveryNoteAuthorizationService interface in Maliev.DeliveryService.Api\Services\IDeliveryNoteAuthorizationService.cs
- [ ] T027 [P] Create DeliveryNoteAuthorizationService in Maliev.DeliveryService.Api\Services\DeliveryNoteAuthorizationService.cs with customer-scoped access control
- [ ] T028 [P] Create ClaimsPrincipalExtensions.cs in Maliev.DeliveryService.Api\Extensions\ with GetPrincipalId(), GetUserId(), GetCustomerIds() extension methods
- [ ] T029 Configure dependency injection in Program.cs for DbContext, services, Redis, MassTransit
- [ ] T030 [P] Configure OpenTelemetry logging, metrics, and tracing in Program.cs via ServiceDefaults
- [ ] T031 [P] Configure health checks in Program.cs for PostgreSQL, Redis, RabbitMQ
- [ ] T032 Configure Scalar API documentation in Program.cs (NOT Swagger)
- [ ] T033 Register DeliveryService with IAMService for service discovery and permission management in Program.cs startup

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Create and Ship Delivery Note (Priority: P1) 🎯 MVP

**Goal**: Warehouse staff creates delivery notes when goods are ready to ship, tracking what is being delivered with unique IDs and status workflow

**Independent Test**: Create delivery note from order with quantities and carrier info, verify DN-YYYY-XXXXXX ID generated and "Pending" status set, update to "InTransit" and "Delivered"

### Implementation for User Story 1

- [ ] T034 [P] [US1] Create CreateDeliveryNoteRequest DTO in Maliev.DeliveryService.Api\DTOs\CreateDeliveryNoteRequest.cs with validation attributes
- [ ] T035 [P] [US1] Create CreateDeliveryNoteItemRequest DTO in Maliev.DeliveryService.Api\DTOs\CreateDeliveryNoteItemRequest.cs
- [ ] T036 [P] [US1] Create UpdateDeliveryStatusRequest DTO in Maliev.DeliveryService.Api\DTOs\UpdateDeliveryStatusRequest.cs
- [ ] T037 [P] [US1] Create DeliveryNoteResponse DTO in Maliev.DeliveryService.Api\DTOs\DeliveryNoteResponse.cs
- [ ] T038 [P] [US1] Create DeliveryNoteItemResponse DTO in Maliev.DeliveryService.Api\DTOs\DeliveryNoteItemResponse.cs
- [ ] T039 [P] [US1] Create manual mapping extensions in Maliev.DeliveryService.Api\DTOs\DtoMappingExtensions.cs with ToResponse() methods (NO AutoMapper)
- [ ] T040 [US1] Create IDeliveryNoteService interface in Maliev.DeliveryService.Api\Services\IDeliveryNoteService.cs with CreateAsync, GetByIdAsync, UpdateStatusAsync
- [ ] T041 [US1] Implement DeliveryNoteService.CreateAsync in Maliev.DeliveryService.Api\Services\DeliveryNoteService.cs with inline validation (NO FluentValidation)
- [ ] T042 [US1] Implement DeliveryNoteService.GetByIdAsync with customer-scoped authorization check
- [ ] T043 [US1] Implement DeliveryNoteService.UpdateStatusAsync with status transition validation per data-model.md state machine
- [ ] T044 [US1] Implement DeliveryNoteService validation: QuantityDelivered <= QuantityManufactured per FR-003
- [ ] T045 [US1] Implement sequential ID generation using DeliveryNoteIdGenerator with serializable transaction per research.md
- [ ] T046 [US1] Create DeliveryNotesController in Maliev.DeliveryService.Api\Controllers\DeliveryNotesController.cs with [ApiController] and [ApiVersion("1.0")] attributes
- [ ] T047 [US1] Implement POST /delivery/v1/delivery-notes endpoint in DeliveryNotesController calling CreateAsync
- [ ] T048 [US1] Implement GET /delivery/v1/delivery-notes/{id} endpoint in DeliveryNotesController
- [ ] T049 [US1] Implement PATCH /delivery/v1/delivery-notes/{id}/status endpoint in DeliveryNotesController
- [ ] T050 [US1] Add authorization attributes to controller actions enforcing Delivery.Create, Delivery.Read, Delivery.UpdateStatus permissions
- [ ] T051 [US1] Create DeliveryNoteCreatedEvent record in Maliev.DeliveryService.Api\Events\DeliveryNoteCreatedEvent.cs per contracts/events.md
- [ ] T052 [US1] Create DeliveryStatusChangedEvent record in Maliev.DeliveryService.Api\Events\DeliveryStatusChangedEvent.cs
- [ ] T053 [US1] Publish DeliveryNoteCreatedEvent in DeliveryNoteService.CreateAsync using MassTransit IPublishEndpoint
- [ ] T054 [US1] Publish DeliveryStatusChangedEvent in DeliveryNoteService.UpdateStatusAsync
- [ ] T055 [US1] Implement graceful degradation pattern for event publishing with retry queue per research.md and FR-022
- [ ] T056 [US1] Add structured logging to DeliveryNoteService with operation type, user, deliveryNoteId, outcome per FR-026

**Checkpoint**: User Story 1 fully functional - can create delivery notes, update status, publish events with customer-scoped authorization

---

## Phase 4: User Story 2 - Handle Partial Deliveries (Priority: P1)

**Goal**: Track cumulative delivered quantities across multiple delivery notes for same order to prevent over-delivery

**Independent Test**: Create two delivery notes for same order with partial quantities, verify cumulative tracking prevents delivering more than manufactured, confirm order status updates

### Implementation for User Story 2

- [ ] T057 [P] [US2] Add CumulativeDeliveredQuantity calculation logic to DeliveryNoteService
- [ ] T058 [US2] Implement validation in DeliveryNoteService.CreateAsync checking cumulative quantities against order totals per FR-004
- [ ] T059 [P] [US2] Create OrderDetailsDto.cs in Maliev.DeliveryService.Api\DTOs\ with OrderId, CustomerId, Items list, and quantity fields
- [ ] T060 [US2] Create IOrderServiceClient interface in Maliev.DeliveryService.Api\Clients\IOrderServiceClient.cs with GetOrderAsync method returning OrderDetailsDto
- [ ] T061 [US2] Implement OrderServiceClient in Maliev.DeliveryService.Api\Clients\OrderServiceClient.cs with HttpClient integration
- [ ] T062 [US2] Add OrderServiceClient call in DeliveryNoteService.CreateAsync to fetch order details and manufactured quantities
- [ ] T063 [US2] Implement retry logic for OrderServiceClient calls using Aspire built-in resilience (AddStandardResilienceHandler) with exponential backoff per research.md and FR-023
- [ ] T064 [US2] Create DeliveryCompletedEvent record in Maliev.DeliveryService.Api\Events\DeliveryCompletedEvent.cs per contracts/events.md
- [ ] T065 [US2] Publish DeliveryCompletedEvent in DeliveryNoteService.UpdateStatusAsync when status becomes "Delivered" and order fully fulfilled
- [ ] T066 [US2] Implement partial delivery status logic setting "PartiallyDelivered" when cumulative < ordered quantity
- [ ] T067 [US2] Add validation error responses with detailed messages when over-delivery attempted per FR-004

**Checkpoint**: User Story 2 functional - partial deliveries tracked, over-delivery prevented, events published on completion

---

## Phase 5: User Story 3 - Generate Formal Delivery Documentation (Priority: P1)

**Goal**: Generate bilingual Thai/English PDF documents with delivery note details for official shipping documentation

**Independent Test**: Generate PDF for delivery note and verify Thai/English bilingual headers, Kanit font, signature sections, and downloadable URL

### Implementation for User Story 3

- [ ] T068 [P] [US3] Create GeneratePdfRequest DTO in Maliev.DeliveryService.Api\DTOs\GeneratePdfRequest.cs
- [ ] T069 [P] [US3] Create PdfGenerationResponse DTO in Maliev.DeliveryService.Api\DTOs\PdfGenerationResponse.cs with URL field
- [ ] T070 [US3] Create DeliveryNotePdfRequestedEvent record in Maliev.DeliveryService.Api\Events\DeliveryNotePdfRequestedEvent.cs per contracts/events.md
- [ ] T071 [US3] Implement POST /delivery/v1/delivery-notes/{id}/generate-pdf endpoint in DeliveryNotesController
- [ ] T072 [US3] Publish DeliveryNotePdfRequestedEvent in GeneratePdf endpoint handler
- [ ] T073 [US3] Create IPdfServiceClient interface in Maliev.DeliveryService.Api\Clients\IPdfServiceClient.cs
- [ ] T074 [US3] Implement PdfServiceClient stub in Maliev.DeliveryService.Api\Clients\PdfServiceClient.cs (actual PDF generation handled by PdfService microservice)
- [ ] T075 [US3] Add authorization check for Delivery.GeneratePdf permission on PDF endpoint
- [ ] T076 [US3] Implement async PDF generation pattern with event-driven response per contracts/events.md
- [ ] T077 [US3] Add graceful degradation for PDF service failures with queued retry per FR-022

**Checkpoint**: User Story 3 functional - PDF generation events published, async pattern working, graceful degradation for PDF service

---

## Phase 6: User Story 4 - Track Delivery History (Priority: P2)

**Goal**: Search and filter delivery notes by order ID, customer, date range, status for customer service and audit

**Independent Test**: Create multiple delivery notes with different criteria, search by order ID, customer, date range, status and verify correct results with pagination

### Implementation for User Story 4

- [ ] T078 [P] [US4] Create DeliveryNoteFilterRequest DTO in Maliev.DeliveryService.Api\DTOs\DeliveryNoteFilterRequest.cs with OrderId, CustomerId, DateRange, Status filters
- [ ] T079 [P] [US4] Create DeliveryNoteSummaryDto in Maliev.DeliveryService.Api\DTOs\DeliveryNoteSummaryDto.cs for list view
- [ ] T080 [P] [US4] Create PaginatedResponse<T> generic DTO in Maliev.DeliveryService.Api\DTOs\PaginatedResponse.cs
- [ ] T081 [US4] Implement IDeliveryNoteService.SearchAsync method with filtering and pagination logic
- [ ] T082 [US4] Add customer-scoped filtering to SearchAsync ensuring users only see assigned customers per FR-019
- [ ] T083 [US4] Implement GET /delivery/v1/delivery-notes endpoint in DeliveryNotesController with query parameters
- [ ] T084 [US4] Add database indexes in new migration for order_id, customer_id, delivery_date, status columns per research.md
- [ ] T085 [US4] Add partial indexes for soft-deleted records (WHERE NOT is_deleted) per research.md
- [ ] T086 [US4] Implement Redis caching for frequently accessed delivery notes with 5-minute TTL per research.md
- [ ] T087 [US4] Add cache invalidation on update/delete operations
- [ ] T088 [US4] Add pagination support with page size limits (max 100 per page)
- [ ] T089 [US4] Add sorting support for delivery_date, created_at fields

**Checkpoint**: User Story 4 functional - search/filter working with customer-scoped access, pagination, caching, performant queries

---

## Phase 7: User Story 5 - Attach Delivery Evidence (Priority: P2)

**Goal**: Upload and attach signatures, photos, packing lists to delivery notes for proof of delivery

**Independent Test**: Upload signature image, photo, packing list to delivery note and verify files stored, retrievable, with correct metadata and file types

### Implementation for User Story 5

- [ ] T090 [P] [US5] Create UploadFileRequest DTO in Maliev.DeliveryService.Api\DTOs\UploadFileRequest.cs with IFormFile and FileType
- [ ] T091 [P] [US5] Create DeliveryNoteFileResponse DTO in Maliev.DeliveryService.Api\DTOs\DeliveryNoteFileResponse.cs
- [ ] T092 [US5] Create IFileStorageService interface in Maliev.DeliveryService.Api\Services\IFileStorageService.cs with UploadAsync and GetUrlAsync methods
- [ ] T093 [US5] Implement GoogleCloudStorageService in Maliev.DeliveryService.Api\Services\GoogleCloudStorageService.cs using Google.Cloud.Storage.V1 package for direct GCS integration
- [ ] T094 [US5] Implement IDeliveryNoteService.AddFileAsync method accepting IFormFile and FileType
- [ ] T095 [US5] Add file validation in AddFileAsync (max 5MB size, allowed MIME types per research.md)
- [ ] T096 [US5] Implement POST /delivery/v1/delivery-notes/{id}/files endpoint in DeliveryNotesController with multipart/form-data
- [ ] T097 [US5] Implement GET /delivery/v1/delivery-notes/{id}/files endpoint returning list of attached files
- [ ] T098 [US5] Add authorization check for Delivery.UpdateFiles permission on file endpoints
- [ ] T099 [US5] Store file metadata in DeliveryNoteFile entity with GCS URL, original filename, file type, upload timestamp
- [ ] T100 [US5] Add error handling for file upload failures with graceful degradation per FR-022
- [ ] T101 [US5] Implement file deletion on delivery note soft delete (mark files as deleted but keep in GCS for audit)

**Checkpoint**: User Story 5 functional - file uploads working, metadata stored, files retrievable, authorization enforced

---

## Phase 8: User Story 6 - Notify Customers of Shipments (Priority: P3)

**Goal**: Automatically send notifications to customers when delivery status changes with tracking info

**Independent Test**: Create and update delivery note status, verify notification events published with correct customer details and tracking information

### Implementation for User Story 6

- [ ] T102 [P] [US6] Create OrderCompletedEvent record in Maliev.DeliveryService.Api\Events\OrderCompletedEvent.cs per contracts/events.md (consumed event)
- [ ] T103 [US6] Create OrderCompletedEventConsumer in Maliev.DeliveryService.Api\Consumers\OrderCompletedEventConsumer.cs implementing IConsumer<OrderCompletedEvent>
- [ ] T104 [US6] Implement OrderCompletedEventConsumer.Consume method to auto-create delivery note draft per contracts/events.md logic
- [ ] T105 [US6] Configure MassTransit consumer in Program.cs with retry policy per contracts/events.md
- [ ] T106 [US6] Add idempotency check in OrderCompletedEventConsumer using OrderId to prevent duplicate delivery notes per FR-014
- [ ] T107 [US6] Ensure DeliveryStatusChangedEvent includes customer contact information for NotificationService per contracts/events.md
- [ ] T108 [US6] Ensure DeliveryCompletedEvent includes recipient name and delivery time for notifications per contracts/events.md
- [ ] T109 [US6] Add correlation IDs to all published events for distributed tracing per research.md and FR-028
- [ ] T110 [US6] Configure event retry policy with exponential backoff (1s, 2s, 4s, 8s, 16s) and dead letter queue per contracts/events.md and FR-023
- [ ] T111 [US6] Add event publishing metrics to OpenTelemetry for monitoring event success rates per FR-027

**Checkpoint**: User Story 6 functional - events published with notification data, consumer auto-creates delivery notes from orders, retry/DLQ configured

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Improvements affecting multiple user stories and production readiness

- [ ] T112 [P] Create UpdateDeliveryNoteRequest DTO in Maliev.DeliveryService.Api\DTOs\UpdateDeliveryNoteRequest.cs for updating carrier, tracking, contact info per FR-013
- [ ] T113 [P] Implement PUT /delivery/v1/delivery-notes/{id} endpoint in DeliveryNotesController for updates
- [ ] T114 [P] Implement DELETE /delivery/v1/delivery-notes/{id} endpoint with soft delete for "Pending" status only per FR-012
- [ ] T115 [P] Implement IDeliveryNoteService.UpdateAsync with validation that terminal states cannot transition per FR-016
- [ ] T116 [P] Add optimistic concurrency control using RowVersion property per data-model.md
- [ ] T117 [P] Handle DbUpdateConcurrencyException with conflict detection and retry logic
- [ ] T118 [P] Create DeliveryNoteArchivalService in Maliev.DeliveryService.Api\Services\DeliveryNoteArchivalService.cs implementing IHostedService for FR-030 (7-year retention)
- [ ] T119 [P] Implement archival logic in ArchivalService to move delivery notes older than 2 years to archival storage per FR-031 (auto-archive >2 years)
- [ ] T120 [P] Implement deletion logic in ArchivalService to permanently delete delivery notes older than 7 years with audit logging per FR-033 (delete after 7 years)
- [ ] T121 [P] Implement GetArchivedDeliveryNoteAsync method in DeliveryNoteService to retrieve archived records with <5s response time per FR-032
- [ ] T122 [P] Create metrics collection for request counts, error rates, latencies, active delivery note counts per FR-027
- [ ] T123 [P] Add custom OpenTelemetry metrics using Meter API for delivery-specific operations
- [ ] T124 [P] Configure distributed tracing across OrderService, PdfService, NotificationService calls per FR-028
- [ ] T125 [P] Add activity tags to traces for deliveryNoteId, customerId, userId correlation per research.md
- [ ] T126 [P] Create health check implementations for PostgreSQL, Redis, RabbitMQ dependencies per FR-029
- [ ] T127 [P] Add readiness probe checking database migrations applied
- [ ] T128 [P] Create FakeOrderServiceClient test double in Maliev.DeliveryService.Tests\Fakes\FakeOrderServiceClient.cs (NO mocking libraries)
- [ ] T129 [P] Create FakePublishEndpoint test double in Maliev.DeliveryService.Tests\Fakes\FakePublishEndpoint.cs with message tracking
- [ ] T130 [P] Create FakeFileStorageService test double in Maliev.DeliveryService.Tests\Fakes\FakeFileStorageService.cs
- [ ] T131 [P] Create DeliveryServiceTestFixture in Maliev.DeliveryService.Tests\Integration\TestFixtures\DeliveryServiceTestFixture.cs with Testcontainers setup
- [ ] T132 [P] Implement PostgreSQL Testcontainer initialization in test fixture with migrations
- [ ] T133 [P] Implement RabbitMQ Testcontainer initialization in test fixture
- [ ] T134 Configure API versioning using Asp.Versioning.Http in Program.cs with AddApiVersioning() and /delivery/v1/ route prefix per FR-034
- [ ] T135 [P] Add API version deprecation warning headers for future v2 migration per FR-036
- [ ] T136 [P] Document API version support policy in quickstart.md per FR-035
- [ ] T137 Run `dotnet build Maliev.DeliveryService.slnx` and verify zero warnings
- [ ] T138 Run `dotnet ef database update` to apply migrations to development database
- [ ] T139 Verify Aspire dashboard shows DeliveryService running with health checks green
- [ ] T140 Verify Scalar API documentation accessible at /scalar/v1 endpoint

**Checkpoint**: Polish complete - all CRUD endpoints working, archival service configured, observability enabled, API versioning in place

---

## Phase 10: Testing

**Purpose**: Comprehensive test coverage for quality assurance per work-package.md WP-5.6

### Unit Tests

- [ ] T141 [P] Create DeliveryNoteServiceTests.cs in Maliev.DeliveryService.Tests\Unit\Services\
- [ ] T142 [P] Add test: CreateDeliveryNoteAsync_ValidRequest_ReturnsDeliveryNote (verify ID format DN-YYYY-XXXXXX, items count, event published)
- [ ] T143 [P] Add test: CreateDeliveryNoteAsync_DeliveredExceedsManufactured_ThrowsValidationException
- [ ] T144 [P] Add test: CreateDeliveryNoteAsync_NoItems_ThrowsValidationException
- [ ] T145 [P] Add test: UpdateDeliveryStatusAsync_ValidTransition_UpdatesStatus (Pending → InTransit → Delivered)
- [ ] T146 [P] Add test: UpdateDeliveryStatusAsync_InvalidTransition_ThrowsInvalidOperationException (Delivered → Pending not allowed)
- [ ] T147 [P] Add test: UpdateDeliveryStatusAsync_ToDeliveredWithoutReceivedBy_ThrowsValidationException
- [ ] T148 [P] Add test: DeleteDeliveryNoteAsync_PendingStatus_SoftDeletes (verify IsDeleted=true, DeletedAt set)
- [ ] T149 [P] Add test: DeleteDeliveryNoteAsync_DeliveredStatus_ThrowsInvalidOperationException
- [ ] T150 [P] Create DtoMappingTests.cs in Maliev.DeliveryService.Tests\Unit\DTOs\
- [ ] T151 [P] Add test: ToEntity_ValidRequest_MapsCorrectly (verify all fields mapped from CreateDeliveryNoteRequest)
- [ ] T152 [P] Add test: ToResponse_ValidEntity_MapsCorrectly (verify all fields mapped to DeliveryNoteResponse)
- [ ] T153 [P] Add test: DeliveryNoteIdGenerator_GeneratesSequentialIds (verify DN-2026-000001, DN-2026-000002 format)
- [ ] T154 [P] Add test: DeliveryNoteIdGenerator_HandlesYearRollover (verify DN-2027-000001 after year change)

### Integration Tests with Testcontainers

- [ ] T155 [P] Create DeliveryNotesControllerTests.cs in Maliev.DeliveryService.Tests\Integration\Controllers\ using DeliveryServiceTestFixture
- [ ] T156 [P] Add test: CreateDeliveryNote_ValidRequest_ReturnsCreated (POST, verify status 201, ID format, response body)
- [ ] T157 [P] Add test: GetDeliveryNote_ExistingId_ReturnsOk (GET by ID, verify status 200, correct data)
- [ ] T158 [P] Add test: GetDeliveryNote_NonExistingId_ReturnsNotFound (GET invalid ID, verify status 404)
- [ ] T159 [P] Add test: ListDeliveryNotes_WithFilters_ReturnsFiltered (GET with orderId filter, verify only matching results)
- [ ] T160 [P] Add test: UpdateDeliveryStatus_ValidTransition_ReturnsOk (PATCH Pending→InTransit, verify status 200)
- [ ] T161 [P] Add test: DeleteDeliveryNote_PendingStatus_ReturnsNoContent (DELETE, verify status 204, GET returns 404)
- [ ] T162 [P] Add test: UploadFile_ValidSignature_ReturnsCreated (POST file, verify stored in GCS, metadata correct)
- [ ] T163 [P] Add test: GeneratePdf_ExistingDeliveryNote_ReturnsAccepted (POST generate-pdf, verify status 202, event published)
- [ ] T164 [P] Create DeliveryDbContextTests.cs in Maliev.DeliveryService.Tests\Integration\Database\
- [ ] T165 [P] Add test: QueryFilter_ExcludesSoftDeletedEntities (create note, soft delete, query all, verify excluded)
- [ ] T166 [P] Add test: ConcurrencyCheck_PreventsConflicts (load same note in 2 contexts, update both, verify DbUpdateConcurrencyException)
- [ ] T167 [P] Add test: SequentialIdGeneration_HandlesConcurrency (create 10 notes concurrently, verify all have unique sequential IDs)
- [ ] T168 [P] Add test: PartialDelivery_PreventOverDelivery (create 2 partial deliveries, attempt over-delivery, verify validation error)

### Test Execution & Coverage

- [ ] T169 Run all unit tests: dotnet test --filter Category=Unit and verify all pass
- [ ] T170 Run all integration tests: dotnet test --filter Category=Integration and verify all pass
- [ ] T171 Verify 80%+ code coverage: dotnet test --collect:"XPlat Code Coverage" and check coverage report
- [ ] T172 Run quickstart.md validation by executing all sample curl commands and verifying responses

**Checkpoint**: Testing complete - 80%+ coverage achieved, all tests passing, quality gates met

---

## Phase 11: Final Verification

**Purpose**: End-to-end validation and production readiness checks

- [ ] T173 [P] Add README.md at repository root with setup instructions, architecture overview, and quick start guide
- [ ] T174 [P] Update CLAUDE.md with delivery service context, development patterns, and testing guidelines
- [ ] T175 Verify all Success Criteria from spec.md (SC-001 to SC-014) are met through manual testing
- [ ] T176 Run performance test: hey -n 1000 -c 10 on GET endpoint and verify p95 < 200ms per SC-004
- [ ] T177 Verify PDF generation completes in <5 seconds per SC-003
- [ ] T178 Verify search operations return in <1 second for 10,000 delivery notes per SC-005
- [ ] T179 Test event publishing to RabbitMQ: verify DeliveryNoteCreatedEvent, DeliveryStatusChangedEvent, DeliveryCompletedEvent appear in queue
- [ ] T180 Test authorization: attempt API calls without token (expect 401), with insufficient permissions (expect 403)
- [ ] T181 Test archival process: create old delivery notes, run archival service, verify moved to archival storage
- [ ] T182 Test distributed tracing: create delivery note, verify trace spans visible in Aspire dashboard across all services

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-8)**: All depend on Foundational phase completion
  - User Story 1 (P1): Can start after Foundational - No dependencies on other stories
  - User Story 2 (P1): Depends on User Story 1 completion (needs CreateAsync, entities, basic endpoints)
  - User Story 3 (P1): Depends on User Story 1 completion (needs entities and basic endpoints)
  - User Story 4 (P2): Can start after Foundational - Independent from US1-3 (can run parallel)
  - User Story 5 (P2): Can start after Foundational - Independent from US1-4 (can run parallel)
  - User Story 6 (P3): Depends on User Story 1 completion (uses CreateAsync for event consumer)
- **Polish (Phase 9)**: Depends on all desired user stories being complete
- **Testing (Phase 10)**: Can run in parallel with implementation OR after all user stories complete
- **Verification (Phase 11)**: Depends on all previous phases complete

### User Story Dependencies

- **User Story 1 (P1)**: Foundation only - No other story dependencies
- **User Story 2 (P1)**: Depends on User Story 1 completion (needs CreateAsync, entities, basic endpoints)
- **User Story 3 (P1)**: Depends on User Story 1 completion (needs entities and basic endpoints)
- **User Story 4 (P2)**: Foundation only - Can run in parallel with US1-3
- **User Story 5 (P2)**: Foundation only - Can run in parallel with US1-4
- **User Story 6 (P3)**: Depends on User Story 1 completion (uses CreateAsync for event consumer)

### Within Each User Story

**User Story 1 (Create and Ship)**:
- DTOs (T034-T039) before Service (T040-T045)
- Service interface (T040) before implementation (T041-T045)
- Service (T041-T045) before Controller (T046-T050)
- Events (T051-T052) before publishing logic (T053-T055)

**User Story 2 (Partial Deliveries)**:
- Service logic (T057-T058) before DTO creation (T059)
- OrderDetailsDto (T059) before client interface (T060)
- OrderServiceClient (T060-T063) before usage in CreateAsync
- Event definition (T064) before publishing (T065)

**User Story 3 (PDF Generation)**:
- DTOs (T068-T069) before endpoint (T071)
- Event (T070) before publishing (T072)
- Client interface (T073) before implementation (T074)

**User Story 4 (Search/Filter)**:
- DTOs (T078-T080) before service method (T081)
- Service method (T081-T082) before endpoint (T083)
- Database indexes (T084-T085) before caching (T086-T087)

**User Story 5 (File Attachments)**:
- DTOs (T090-T091) before service interface (T092)
- Service interface/implementation (T092-T093) before service method (T094-T095)
- Service method before endpoints (T096-T097)

**User Story 6 (Notifications)**:
- Event definitions (T102) before consumer (T103-T104)
- Consumer (T103-T104) before MassTransit config (T105)
- Retry policy config (T110) before metrics (T111)

### Parallel Opportunities

**Within Setup (Phase 1)**:
- T002, T003, T004 can run in parallel (create projects)
- T007, T008, T009, T010 can run in parallel (install packages per project)
- T011, T012 can run in parallel (configuration files)

**Within Foundational (Phase 2)**:
- T013, T014, T015, T016 can run in parallel (entity classes)
- T017, T018 can run in parallel (enum definitions)
- T020, T021, T022 can run in parallel (EF configurations after T019 done)
- T026, T027, T028 can run in parallel (authorization service + extensions)
- T030, T031, T032 can run in parallel (Program.cs config sections)

**Within User Story 1**:
- T034, T035, T036, T037, T038, T039 can run in parallel (all DTOs and mapping)
- T051, T052 can run in parallel (event definitions)

**Within User Story 4**:
- T078, T079, T080 can run in parallel (DTOs)
- T084, T085 can run in parallel (database indexes)
- T086, T087 can run in parallel (caching logic)

**Within User Story 5**:
- T090, T091 can run in parallel (DTOs)

**Within Polish (Phase 9)**:
- Most polish tasks marked [P] can run in parallel (different concerns):
  - T112-T117 (CRUD endpoints and concurrency)
  - T118-T121 (archival service)
  - T122-T125 (metrics/tracing)
  - T126-T127 (health checks)
  - T128-T130 (test doubles)
  - T131-T133 (test fixtures)
  - T173-T174 (documentation)

**Within Testing (Phase 10)**:
- All unit test tasks (T141-T154) can run in parallel
- All integration test tasks (T155-T168) can run in parallel

---

## Parallel Example: User Story 1 Implementation

```bash
# After Foundational phase completes, launch User Story 1 DTO tasks in parallel:
Task T034: "Create CreateDeliveryNoteRequest DTO"
Task T035: "Create CreateDeliveryNoteItemRequest DTO"
Task T036: "Create UpdateDeliveryStatusRequest DTO"
Task T037: "Create DeliveryNoteResponse DTO"
Task T038: "Create DeliveryNoteItemResponse DTO"
Task T039: "Create manual mapping extensions"

# Then sequentially: Service interface → Implementation → Controller → Events
```

---

## Implementation Strategy

### MVP First (User Stories 1-3 Only - All P1)

1. Complete Phase 1: Setup (T001-T012)
2. Complete Phase 2: Foundational (T013-T033) - CRITICAL foundation
3. Complete Phase 3: User Story 1 (T034-T056) - Core CRUD + status workflow
4. Complete Phase 4: User Story 2 (T057-T067) - Partial delivery tracking
5. Complete Phase 5: User Story 3 (T068-T077) - PDF generation events
6. **STOP and VALIDATE**: Test all P1 stories independently
7. Deploy/demo MVP with core delivery tracking

### Incremental Delivery

1. MVP (US1-3) → Foundation for delivery operations
2. Add User Story 4 (search/filter) → Customer service enablement
3. Add User Story 5 (file attachments) → Proof of delivery
4. Add User Story 6 (notifications) → Customer experience
5. Complete Polish phase → Production hardening
6. Complete Testing phase → Quality assurance
7. Final Verification → Production ready

### Parallel Team Strategy

With multiple developers after Foundational phase completes:

**Option 1: Sequential P1 stories, parallel P2/P3**
- Everyone: User Story 1 → User Story 2 → User Story 3 (sequential P1 chain)
- Then split: Developer A on US4, Developer B on US5, Developer C on US6 + Testing

**Option 2: Parallel independent stories**
- Developer A: User Story 1 → User Story 2 → User Story 3 (sequential P1 chain)
- Developer B: User Story 4 (independent P2) + Testing tasks
- Developer C: User Story 5 (independent P2)
- After P1 complete: User Story 6 (needs US1)

---

## Notes

- [P] tasks = different files, no dependencies within phase
- [Story] label maps task to specific user story for traceability
- User Story 2-3 depend on User Story 1 entities and services
- User Story 4-5 are independent and can run in parallel
- User Story 6 depends on User Story 1 CreateAsync for event consumer
- Foundational phase MUST complete before any user story work begins
- NO AutoMapper - use manual DtoMappingExtensions per Maliev standards
- NO FluentValidation - use inline validation in services per Maliev standards
- NO MediatR - direct service method calls per Maliev standards
- NO mocking libraries - create manual test doubles in Tests\Fakes\ per Maliev standards
- All database operations use EF Core with async/await patterns
- All service integrations implement graceful degradation with retry per FR-022
- All operations emit structured logs and metrics per FR-026, FR-027
- All cross-service calls include distributed tracing correlation per FR-028
- Follow Maliev coding standards throughout implementation
- Commit after each completed task or logical group
- Run builds frequently to catch issues early
- Stop at checkpoints to validate story independently before proceeding

---

**Total Tasks**: 182 (was 138, added 44 tasks)
- **Setup**: 12 tasks (was 10, +2)
- **Foundational**: 21 tasks (was 19, +2)
- **User Story 1**: 23 tasks (unchanged)
- **User Story 2**: 11 tasks (was 10, +1 for OrderDetailsDto)
- **User Story 3**: 10 tasks (unchanged)
- **User Story 4**: 12 tasks (unchanged)
- **User Story 5**: 12 tasks (was 12, updated implementation)
- **User Story 6**: 10 tasks (unchanged)
- **Polish**: 29 tasks (was 32, optimized)
- **Testing**: 32 tasks (NEW - comprehensive test suite)
- **Verification**: 10 tasks (NEW - production readiness)

**Parallel Opportunities Identified**: 67 tasks marked [P] can run in parallel within their phases (was 47)

**Independent Test Criteria** (all maintained):
- US1: Create delivery note → Update status → Verify events published
- US2: Create 2 partial deliveries → Verify cumulative tracking → Test over-delivery prevention
- US3: Generate PDF → Verify bilingual format → Verify downloadable URL
- US4: Create 10+ delivery notes → Search by filters → Verify pagination and performance
- US5: Upload 3 file types → Verify metadata → Verify retrieval with authorization
- US6: Trigger order completed event → Verify auto-created delivery note → Verify notification events

**Suggested MVP Scope**: User Stories 1-3 (Phase 1-2-3-4-5) = 46 implementation tasks + 33 foundation tasks = **79 tasks for core delivery tracking with PDF generation**

**Coverage Improvements**:
- ✅ All 36 Functional Requirements now have explicit task coverage (was 78%, now 100%)
- ✅ IAMService registration added (FR-021)
- ✅ Archival tasks explicitly mapped to FR-030-033
- ✅ Test coverage tasks added from work-package.md (FR for quality)
- ✅ All ambiguities resolved (Google Cloud Storage, Aspire resilience, API versioning package)
- ✅ Helper extensions added (ClaimsPrincipalExtensions)
- ✅ Check constraints explicitly defined in EF configuration
- ✅ OrderDetailsDto creation task added
