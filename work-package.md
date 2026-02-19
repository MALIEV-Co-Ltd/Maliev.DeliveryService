# WP-5: DeliveryService Backend Implementation

**Priority:** Critical (Thai business operations requirement — ใบส่งของ)
**Build:** `dotnet build Maliev.DeliveryService.slnx`
**Estimated Time:** 19-26 days (4-5 weeks)

---

## WP-5.1: Project Setup & Infrastructure

**Goal:** Create solution structure and configure Aspire integration

- [ ] Create `Maliev.DeliveryService` directory at `B:\maliev\Maliev.DeliveryService\`
- [ ] Create solution file: `Maliev.DeliveryService.slnx`
- [ ] Create `Maliev.DeliveryService.Api` project (ASP.NET Core 10.0 Web API)
  - [ ] Target framework: `net10.0`
  - [ ] Enable nullable reference types: `<Nullable>enable</Nullable>`
  - [ ] Treat warnings as errors: `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
  - [ ] Add project reference to `Maliev.Aspire.ServiceDefaults`
  - [ ] Add project reference to `Maliev.MessagingContracts`
- [ ] Create `Maliev.DeliveryService.Data` project (Class Library .NET 10.0)
  - [ ] Target framework: `net10.0`
  - [ ] Enable nullable reference types
  - [ ] Treat warnings as errors
  - [ ] Add NuGet packages:
    - [ ] `Microsoft.EntityFrameworkCore` version 10.0.x
    - [ ] `Npgsql.EntityFrameworkCore.PostgreSQL` version 10.0.x
    - [ ] `Microsoft.EntityFrameworkCore.Tools` version 10.0.x (for migrations)
- [ ] Create `Maliev.DeliveryService.Tests` project (xUnit .NET 10.0)
  - [ ] Add NuGet packages:
    - [ ] `xunit` latest version
    - [ ] `xunit.runner.visualstudio` latest version
    - [ ] `FluentAssertions` latest version
    - [ ] `Testcontainers.PostgreSql` latest version
    - [ ] `Testcontainers.RabbitMq` latest version
    - [ ] `Microsoft.AspNetCore.Mvc.Testing` version 10.0.x
  - [ ] Add project reference to `Maliev.DeliveryService.Api`
  - [ ] Add project reference to `Maliev.DeliveryService.Data`
- [ ] Add all projects to solution: `dotnet sln Maliev.DeliveryService.slnx add ...`
- [ ] Configure Aspire integration in `B:\maliev\Maliev.Aspire\Maliev.Aspire.AppHost\Program.cs`:
  - [ ] Add DeliveryService database: `var deliveryDb = postgresServer.AddDatabase("delivery-db");`
  - [ ] Add DeliveryService project: `var deliveryService = builder.AddProject<Projects.Maliev_DeliveryService_Api>("deliveryservice")`
  - [ ] Add database reference: `.WithReference(deliveryDb)`
  - [ ] Add Redis reference: `.WithReference(redis)`
  - [ ] Add messaging reference: `.WithReference(messaging)`
  - [ ] Add wait dependencies: `.WaitFor(deliveryDb).WaitFor(redis).WaitFor(messaging)`
  - [ ] Update BFF references: `intranetBff.WithReference(deliveryService)`
- [ ] Register service with IAMService in `Program.cs` startup (self-registration pattern)
- [ ] Verify build: `dotnet build Maliev.DeliveryService.slnx` — zero warnings

---

## WP-5.2: Database Schema & Entities

**Goal:** Create entity classes, EF configurations, and initial migration

### Entity Classes

- [ ] Create `Maliev.DeliveryService.Data/Entities/` directory
- [ ] Create `DeliveryNote.cs` entity:
  - [ ] Add `[Table("delivery_notes")]` attribute
  - [ ] Add property: `string DeliveryNoteId` (PK, max 50 chars) with `[Key]`
  - [ ] Add property: `string? OrderId` (FK to OrderService, nullable, max 50 chars)
  - [ ] Add property: `int? PurchaseOrderId` (FK to PurchaseOrderService, nullable)
  - [ ] Add property: `Guid CustomerId` (customer reference, required)
  - [ ] Add property: `string? CustomerName` (cached, max 500 chars)
  - [ ] Add property: `DateTime DeliveryDate` (scheduled delivery date, required)
  - [ ] Add property: `DateTime? ActualDeliveryTime` (actual delivery timestamp, nullable)
  - [ ] Add property: `DeliveryStatus Status` (enum, required, max 50 chars)
  - [ ] Add shipping address fields (denormalized snapshot):
    - [ ] `Guid? ShippingAddressId`
    - [ ] `string? ShippingAddressLine1` (max 500 chars)
    - [ ] `string? ShippingAddressLine2` (max 500 chars)
    - [ ] `string? ShippingCity` (max 200 chars)
    - [ ] `string? ShippingProvince` (max 200 chars)
    - [ ] `string? ShippingPostalCode` (max 20 chars)
    - [ ] `string? ShippingCountry` (max 100 chars)
  - [ ] Add contact information fields:
    - [ ] `string? DeliveryContactName` (max 200 chars)
    - [ ] `string? DeliveryContactPhone` (max 50 chars)
    - [ ] `string? DeliveryContactEmail` (max 200 chars)
  - [ ] Add logistics fields:
    - [ ] `string? CarrierName` (max 200 chars)
    - [ ] `string? TrackingNumber` (max 200 chars)
    - [ ] `decimal? ShippingCost` (precision 18, scale 2)
    - [ ] `string? ShippingCostCurrency` (max 10 chars)
  - [ ] Add proof of delivery fields:
    - [ ] `string? ReceivedByName` (max 200 chars)
    - [ ] `Guid? SignatureFileId` (FK to file storage)
    - [ ] `DateTime? SignedAt`
  - [ ] Add notes fields:
    - [ ] `string? InternalNotes` (text, employee-only)
    - [ ] `string? DeliveryInstructions` (text, customer-visible)
  - [ ] Add metadata fields:
    - [ ] `DateTime CreatedAt` (required, default CURRENT_TIMESTAMP)
    - [ ] `string CreatedBy` (required, max 200 chars)
    - [ ] `DateTime? UpdatedAt`
    - [ ] `string? UpdatedBy` (max 200 chars)
    - [ ] `int RowVersion` (concurrency token, `[ConcurrencyCheck]`)
  - [ ] Add soft delete fields:
    - [ ] `bool IsDeleted` (required, default false)
    - [ ] `DateTime? DeletedAt`
    - [ ] `string? DeletedBy` (max 200 chars)
  - [ ] Add navigation properties:
    - [ ] `ICollection<DeliveryNoteItem> Items`
    - [ ] `ICollection<DeliveryNoteFile> Files`
    - [ ] `Address? ShippingAddress`
- [ ] Create `DeliveryStatus` enum:
  - [ ] `Pending` = created but not yet shipped
  - [ ] `InTransit` = shipped, on the way
  - [ ] `Delivered` = fully delivered and signed for
  - [ ] `PartiallyDelivered` = some items delivered, more to come
  - [ ] `Cancelled` = delivery cancelled
- [ ] Create `DeliveryNoteItem.cs` entity:
  - [ ] Add `[Table("delivery_note_items")]` attribute
  - [ ] Add property: `long Id` (PK, auto-increment) with `[Key]`
  - [ ] Add property: `string DeliveryNoteId` (FK, required, max 50 chars)
  - [ ] Add property: `string? OrderId` (FK to OrderService order line, nullable)
  - [ ] Add property: `long? PurchaseOrderItemId` (FK to PurchaseOrderService line, nullable)
  - [ ] Add product info fields (cached/denormalized):
    - [ ] `string? ProductCode` (max 100 chars)
    - [ ] `string? ProductName` (max 500 chars)
    - [ ] `string? ProductDescription` (text)
  - [ ] Add quantity tracking fields:
    - [ ] `decimal QuantityOrdered` (precision 18, scale 4, required)
    - [ ] `decimal QuantityManufactured` (precision 18, scale 4, required)
    - [ ] `decimal QuantityDelivered` (precision 18, scale 4, required)
    - [ ] `string UnitOfMeasure` (required, max 50 chars)
  - [ ] Add property: `string? ItemNotes` (text, item-specific notes)
  - [ ] Add property: `DateTime CreatedAt` (required, default CURRENT_TIMESTAMP)
  - [ ] Add navigation property: `DeliveryNote DeliveryNote`
- [ ] Create `DeliveryNoteFile.cs` entity:
  - [ ] Add `[Table("delivery_note_files")]` attribute
  - [ ] Add property: `Guid Id` (PK) with `[Key]`
  - [ ] Add property: `string DeliveryNoteId` (FK, required, max 50 chars)
  - [ ] Add file metadata fields:
    - [ ] `string FileName` (required, max 500 chars)
    - [ ] `string StorageUrl` (required, max 2000 chars, GCS URL)
    - [ ] `string ContentType` (required, max 200 chars, MIME type)
    - [ ] `long FileSize` (required, bytes)
    - [ ] `DeliveryFileType FileType` (enum, required, max 50 chars)
  - [ ] Add metadata fields:
    - [ ] `DateTime UploadedAt` (required, default CURRENT_TIMESTAMP)
    - [ ] `string UploadedBy` (required, max 200 chars)
  - [ ] Add soft delete fields:
    - [ ] `bool IsDeleted` (required, default false)
    - [ ] `DateTime? DeletedAt`
  - [ ] Add navigation property: `DeliveryNote DeliveryNote`
- [ ] Create `DeliveryFileType` enum:
  - [ ] `Signature` = delivery signature
  - [ ] `Photo` = package photo
  - [ ] `Invoice` = related invoice
  - [ ] `PackingList` = packing list
  - [ ] `Other` = other supporting documents
- [ ] Create `Address.cs` entity (or reuse from PurchaseOrderService pattern):
  - [ ] Add `[Table("addresses")]` attribute
  - [ ] Add property: `Guid Id` (PK) with `[Key]`
  - [ ] Add property: `string? CompanyName` (max 500 chars)
  - [ ] Add property: `string? ContactName` (max 200 chars)
  - [ ] Add property: `string AddressLine1` (required, max 500 chars)
  - [ ] Add property: `string? AddressLine2` (max 500 chars)
  - [ ] Add property: `string City` (required, max 200 chars)
  - [ ] Add property: `string StateProvince` (required, max 200 chars)
  - [ ] Add property: `string PostalCode` (required, max 20 chars)
  - [ ] Add property: `string Country` (required, max 100 chars)
  - [ ] Add property: `string? PhoneNumber` (max 50 chars)
  - [ ] Add property: `string? EmailAddress` (max 200 chars)
  - [ ] Add metadata fields: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`

### EF Configurations

- [ ] Create `Maliev.DeliveryService.Data/Configurations/` directory
- [ ] Create `DeliveryNoteConfiguration.cs`:
  - [ ] Implement `IEntityTypeConfiguration<DeliveryNote>`
  - [ ] Configure table name: `.ToTable("delivery_notes")`
  - [ ] Configure primary key: `.HasKey(e => e.DeliveryNoteId)`
  - [ ] Configure status enum conversion: `.HasConversion<string>()`
  - [ ] Configure default values:
    - [ ] `CreatedAt`: `.HasDefaultValueSql("CURRENT_TIMESTAMP")`
    - [ ] `IsDeleted`: `.HasDefaultValue(false)`
  - [ ] Configure indexes:
    - [ ] `.HasIndex(e => e.OrderId).HasFilter("NOT is_deleted")`
    - [ ] `.HasIndex(e => e.CustomerId).HasFilter("NOT is_deleted")`
    - [ ] `.HasIndex(e => e.Status).HasFilter("NOT is_deleted")`
    - [ ] `.HasIndex(e => e.DeliveryDate).HasFilter("NOT is_deleted")`
    - [ ] `.HasIndex(e => e.TrackingNumber).HasFilter("NOT is_deleted")`
  - [ ] Configure relationships:
    - [ ] Items: `.HasMany(e => e.Items).WithOne(e => e.DeliveryNote).HasForeignKey(e => e.DeliveryNoteId).OnDelete(DeleteBehavior.Cascade)`
    - [ ] Files: `.HasMany(e => e.Files).WithOne(e => e.DeliveryNote).HasForeignKey(e => e.DeliveryNoteId).OnDelete(DeleteBehavior.Cascade)`
    - [ ] Address: `.HasOne(e => e.ShippingAddress).WithMany().HasForeignKey(e => e.ShippingAddressId).OnDelete(DeleteBehavior.Restrict)`
  - [ ] Configure query filter for soft delete: `.HasQueryFilter(e => !e.IsDeleted)`
- [ ] Create `DeliveryNoteItemConfiguration.cs`:
  - [ ] Implement `IEntityTypeConfiguration<DeliveryNoteItem>`
  - [ ] Configure table name: `.ToTable("delivery_note_items")`
  - [ ] Configure primary key: `.HasKey(e => e.Id)`
  - [ ] Configure ID generation: `.Property(e => e.Id).ValueGeneratedOnAdd()`
  - [ ] Configure default value for `CreatedAt`
  - [ ] Configure indexes:
    - [ ] `.HasIndex(e => e.DeliveryNoteId)`
  - [ ] Configure check constraints:
    - [ ] `CHECK (quantity_ordered >= 0)`
    - [ ] `CHECK (quantity_manufactured >= 0)`
    - [ ] `CHECK (quantity_delivered > 0)`
    - [ ] `CHECK (quantity_delivered <= quantity_manufactured)`
- [ ] Create `DeliveryNoteFileConfiguration.cs`:
  - [ ] Implement `IEntityTypeConfiguration<DeliveryNoteFile>`
  - [ ] Configure table name: `.ToTable("delivery_note_files")`
  - [ ] Configure primary key with default value: `.HasDefaultValueSql("gen_random_uuid()")`
  - [ ] Configure file type enum conversion
  - [ ] Configure indexes with soft delete filter
- [ ] Create `AddressConfiguration.cs`:
  - [ ] Implement `IEntityTypeConfiguration<Address>`
  - [ ] Configure table name, primary key, required fields

### DbContext

- [ ] Create `DeliveryDbContext.cs` in `Maliev.DeliveryService.Data/`:
  - [ ] Inherit from `DbContext`
  - [ ] Add constructor: `public DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : base(options)`
  - [ ] Add DbSets:
    - [ ] `public DbSet<DeliveryNote> DeliveryNotes { get; set; }`
    - [ ] `public DbSet<DeliveryNoteItem> DeliveryNoteItems { get; set; }`
    - [ ] `public DbSet<DeliveryNoteFile> DeliveryNoteFiles { get; set; }`
    - [ ] `public DbSet<Address> Addresses { get; set; }`
  - [ ] Override `OnModelCreating`:
    - [ ] Apply all configurations: `modelBuilder.ApplyConfigurationsFromAssembly(typeof(DeliveryDbContext).Assembly);`
    - [ ] Set default schema: `modelBuilder.HasDefaultSchema("public");`

### Migration

- [ ] Create initial migration:
  ```bash
  dotnet ef migrations add Initial --project Maliev.DeliveryService.Data --startup-project Maliev.DeliveryService.Api
  ```
- [ ] Verify migration file created in `Maliev.DeliveryService.Data/Migrations/`
- [ ] Review generated SQL:
  ```bash
  dotnet ef migrations script --project Maliev.DeliveryService.Data --startup-project Maliev.DeliveryService.Api
  ```
- [ ] Test migration:
  ```bash
  dotnet ef database update --project Maliev.DeliveryService.Data --startup-project Maliev.DeliveryService.Api
  ```

---

## WP-5.3: Business Logic & Services

**Goal:** Implement DTOs, service layer, and business logic

### DTOs

- [ ] Create `Maliev.DeliveryService.Api/DTOs/` directory
- [ ] Create `CreateDeliveryNoteRequest.cs`:
  - [ ] Add `sealed record` with properties:
    - [ ] `string? OrderId`
    - [ ] `int? PurchaseOrderId`
    - [ ] `DateTime DeliveryDate`
    - [ ] `Guid? ShippingAddressId`
    - [ ] `string? DeliveryContactName`
    - [ ] `string? DeliveryContactPhone`
    - [ ] `string? DeliveryContactEmail`
    - [ ] `string? CarrierName`
    - [ ] `string? TrackingNumber`
    - [ ] `string? DeliveryInstructions`
    - [ ] `List<CreateDeliveryNoteItemRequest> Items`
- [ ] Create `CreateDeliveryNoteItemRequest.cs`:
  - [ ] Add `sealed record` with properties:
    - [ ] `string? OrderId`
    - [ ] `long? PurchaseOrderItemId`
    - [ ] `string? ProductCode`
    - [ ] `string? ProductName`
    - [ ] `decimal QuantityOrdered`
    - [ ] `decimal QuantityManufactured`
    - [ ] `decimal QuantityDelivered`
    - [ ] `string UnitOfMeasure`
    - [ ] `string? ItemNotes`
- [ ] Create `UpdateDeliveryNoteRequest.cs`:
  - [ ] Add `sealed record` with nullable properties for partial updates:
    - [ ] `DateTime? DeliveryDate`
    - [ ] `Guid? ShippingAddressId`
    - [ ] `string? DeliveryContactName`
    - [ ] `string? DeliveryContactPhone`
    - [ ] `string? CarrierName`
    - [ ] `string? TrackingNumber`
    - [ ] `decimal? ShippingCost`
    - [ ] `string? DeliveryInstructions`
    - [ ] `string? InternalNotes`
- [ ] Create `UpdateDeliveryStatusRequest.cs`:
  - [ ] Add `sealed record` with properties:
    - [ ] `DeliveryStatus Status` (required)
    - [ ] `DateTime? ActualDeliveryTime`
    - [ ] `string? ReceivedByName`
    - [ ] `string? Notes`
- [ ] Create `DeliveryNoteResponse.cs`:
  - [ ] Add `sealed record` with all delivery note fields
  - [ ] Add `List<DeliveryNoteItemResponse> Items`
- [ ] Create `DeliveryNoteItemResponse.cs`:
  - [ ] Add `sealed record` with item fields
- [ ] Create `DeliveryNoteSummaryDto.cs`:
  - [ ] Add `sealed record` with summary fields for list view:
    - [ ] `string DeliveryNoteId`
    - [ ] `string? OrderId`
    - [ ] `string? CustomerName`
    - [ ] `DateTime DeliveryDate`
    - [ ] `string Status`
    - [ ] `int ItemCount`
    - [ ] `string? TrackingNumber`
- [ ] Create `DeliveryNoteFileResponse.cs`:
  - [ ] Add `sealed record` with file metadata
- [ ] Create `DtoMappingExtensions.cs`:
  - [ ] Implement `ToEntity()` extension method for `CreateDeliveryNoteRequest`
  - [ ] Implement `ToResponse()` extension method for `DeliveryNote`
  - [ ] Implement `ToSummaryDto()` extension method for `DeliveryNote`
  - [ ] Implement `ToItemResponse()` extension method for `DeliveryNoteItem`
  - [ ] Implement `ToFileResponse()` extension method for `DeliveryNoteFile`
  - [ ] NO AutoMapper — all mapping is manual

### Service Layer

- [ ] Create `Maliev.DeliveryService.Api/Services/` directory
- [ ] Create `IDeliveryNoteService.cs` interface:
  - [ ] Add method: `Task<DeliveryNoteResponse> CreateDeliveryNoteAsync(CreateDeliveryNoteRequest request, string createdBy, CancellationToken ct = default)`
  - [ ] Add method: `Task<DeliveryNoteResponse?> GetDeliveryNoteAsync(string deliveryNoteId, CancellationToken ct = default)`
  - [ ] Add method: `Task<PagedResponse<DeliveryNoteSummaryDto>> GetDeliveryNotesAsync(int page, int pageSize, string? orderId, Guid? customerId, DeliveryStatus? status, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default)`
  - [ ] Add method: `Task<DeliveryNoteResponse> UpdateDeliveryNoteAsync(string deliveryNoteId, UpdateDeliveryNoteRequest request, string updatedBy, CancellationToken ct = default)`
  - [ ] Add method: `Task<DeliveryNoteResponse> UpdateDeliveryStatusAsync(string deliveryNoteId, UpdateDeliveryStatusRequest request, string updatedBy, CancellationToken ct = default)`
  - [ ] Add method: `Task DeleteDeliveryNoteAsync(string deliveryNoteId, string deletedBy, CancellationToken ct = default)`
  - [ ] Add method: `Task<DeliveryNoteFileResponse> AddFileAsync(string deliveryNoteId, IFormFile file, DeliveryFileType fileType, string uploadedBy, CancellationToken ct = default)`
  - [ ] Add method: `Task<List<DeliveryNoteFileResponse>> GetFilesAsync(string deliveryNoteId, CancellationToken ct = default)`
  - [ ] Add method: `Task<string> GeneratePdfAsync(string deliveryNoteId, string requestedBy, CancellationToken ct = default)`
- [ ] Create `DeliveryNoteService.cs`:
  - [ ] Add constructor with dependencies: `DeliveryDbContext`, `IPublishEndpoint`, `DeliveryNoteIdGenerator`, `ILogger`
  - [ ] Implement `CreateDeliveryNoteAsync`:
    - [ ] Validate request (at least one item, positive quantities, delivered ≤ manufactured)
    - [ ] Generate sequential ID via `DeliveryNoteIdGenerator`
    - [ ] Map request to entity using `DtoMappingExtensions`
    - [ ] Set metadata: `CreatedAt = DateTime.UtcNow`, `CreatedBy = createdBy`
    - [ ] Set initial status: `Status = DeliveryStatus.Pending`
    - [ ] Add to DbContext and save
    - [ ] Publish `DeliveryNoteCreatedEvent` via MassTransit
    - [ ] Return response
  - [ ] Implement `GetDeliveryNoteAsync`:
    - [ ] Query DeliveryNote by ID with `.Include(dn => dn.Items).Include(dn => dn.Files)`
    - [ ] Return null if not found
    - [ ] Map to response DTO
  - [ ] Implement `GetDeliveryNotesAsync`:
    - [ ] Build query with filters (orderId, customerId, status, date range)
    - [ ] Apply pagination: `.Skip((page - 1) * pageSize).Take(pageSize)`
    - [ ] Get total count for `PagedResponse`
    - [ ] Map to summary DTOs
  - [ ] Implement `UpdateDeliveryNoteAsync`:
    - [ ] Load delivery note by ID
    - [ ] Throw `NotFoundException` if not found
    - [ ] Update fields from request (only non-null values)
    - [ ] Set `UpdatedAt = DateTime.UtcNow`, `UpdatedBy = updatedBy`
    - [ ] Save changes
    - [ ] Return response
  - [ ] Implement `UpdateDeliveryStatusAsync`:
    - [ ] Load delivery note by ID
    - [ ] Validate status transition (see `ValidateStatusTransition` method)
    - [ ] If status = Delivered, require `ActualDeliveryTime` and `ReceivedByName`
    - [ ] Update status and related fields
    - [ ] Set `UpdatedAt`, `UpdatedBy`
    - [ ] Save changes
    - [ ] Publish `DeliveryStatusChangedEvent`
    - [ ] If status = Delivered, publish `DeliveryCompletedEvent`
    - [ ] Return response
  - [ ] Implement `DeleteDeliveryNoteAsync`:
    - [ ] Load delivery note by ID
    - [ ] Check status: can only delete if status = Pending
    - [ ] Soft delete: set `IsDeleted = true`, `DeletedAt = DateTime.UtcNow`, `DeletedBy = deletedBy`
    - [ ] Save changes
  - [ ] Implement `AddFileAsync`:
    - [ ] Upload file via UploadService (or store GCS URL directly)
    - [ ] Create `DeliveryNoteFile` entity
    - [ ] Add to DbContext and save
    - [ ] Return file response
  - [ ] Implement `GetFilesAsync`:
    - [ ] Query files for delivery note ID
    - [ ] Filter out soft-deleted files
    - [ ] Map to response DTOs
  - [ ] Implement `GeneratePdfAsync`:
    - [ ] Fetch delivery note data
    - [ ] Publish `DeliveryNotePdfRequestedEvent` to PdfService
    - [ ] Return placeholder PDF URL (actual generation is async)
- [ ] Create `DeliveryNoteIdGenerator.cs`:
  - [ ] Add constructor with `DeliveryDbContext`
  - [ ] Implement `GenerateNextIdAsync()`:
    - [ ] Get current year: `var year = DateTime.UtcNow.Year;`
    - [ ] Build prefix: `var prefix = $"DN-{year}-";`
    - [ ] Query last ID for this year: `var lastId = await _context.DeliveryNotes.Where(dn => dn.DeliveryNoteId.StartsWith(prefix)).OrderByDescending(dn => dn.DeliveryNoteId).Select(dn => dn.DeliveryNoteId).FirstOrDefaultAsync(ct);`
    - [ ] Parse number from last ID, increment by 1
    - [ ] Return formatted ID: `$"{prefix}{nextNumber:D6}"`
    - [ ] Handle concurrency with retry logic (up to 3 attempts)
- [ ] Create validation helper methods:
  - [ ] `ValidateCreateRequest(CreateDeliveryNoteRequest request)`:
    - [ ] Check: `request.OrderId != null || request.PurchaseOrderId != null`
    - [ ] Check: `request.Items != null && request.Items.Any()`
    - [ ] For each item: `item.QuantityDelivered > 0 && item.QuantityDelivered <= item.QuantityManufactured`
    - [ ] Throw `ValidationException` with descriptive message if validation fails
  - [ ] `ValidateStatusTransition(DeliveryStatus currentStatus, DeliveryStatus newStatus)`:
    - [ ] Define valid transitions dictionary
    - [ ] Check if transition is allowed
    - [ ] Throw `InvalidOperationException` if not allowed

### Authorization Service

- [ ] Create `IDeliveryNoteAuthorizationService.cs` interface:
  - [ ] Add method: `Task<bool> CanCreateDeliveryNoteAsync(string principalId, string? orderId, CancellationToken ct = default)`
  - [ ] Add method: `Task<bool> CanAccessDeliveryNoteAsync(string principalId, Guid customerId, CancellationToken ct = default)`
  - [ ] Add method: `Task<bool> CanUpdateDeliveryNoteAsync(string principalId, string deliveryNoteId, CancellationToken ct = default)`
  - [ ] Add method: `Task<bool> CanDeleteDeliveryNoteAsync(string principalId, string deliveryNoteId, CancellationToken ct = default)`
- [ ] Create `DeliveryNoteAuthorizationService.cs`:
  - [ ] Implement permission checks via IAMService
  - [ ] Implement customer-scoped access (users can only access delivery notes for their assigned customers)
  - [ ] Use HTTP client to call IAMService for permission verification

### Service Registration

- [ ] Update `Program.cs` in `Maliev.DeliveryService.Api`:
  - [ ] Register `IDeliveryNoteService` with DI: `builder.Services.AddScoped<IDeliveryNoteService, DeliveryNoteService>();`
  - [ ] Register `IDeliveryNoteAuthorizationService`: `builder.Services.AddScoped<IDeliveryNoteAuthorizationService, DeliveryNoteAuthorizationService>();`
  - [ ] Register `DeliveryNoteIdGenerator`: `builder.Services.AddScoped<DeliveryNoteIdGenerator>();`

---

## WP-5.4: API Controllers & Endpoints

**Goal:** Create API endpoints with authorization and documentation

- [ ] Create `Maliev.DeliveryService.Api/Controllers/` directory
- [ ] Create `DeliveryNotesController.cs`:
  - [ ] Add `[ApiController]` attribute
  - [ ] Add `[ApiVersion("1.0")]` attribute
  - [ ] Add `[Route("delivery/v{version:apiVersion}/delivery-notes")]` attribute
  - [ ] Add `[Produces("application/json")]` attribute
  - [ ] Add constructor with dependencies: `IDeliveryNoteService`, `IDeliveryNoteAuthorizationService`, `ILogger`
  - [ ] Implement `POST /delivery/v1/delivery-notes`:
    - [ ] Add `[HttpPost]` attribute
    - [ ] Add `[ProducesResponseType(typeof(DeliveryNoteResponse), StatusCodes.Status201Created)]`
    - [ ] Add `[ProducesResponseType(StatusCodes.Status400BadRequest)]`
    - [ ] Add `[ProducesResponseType(StatusCodes.Status403Forbidden)]`
    - [ ] Add XML doc comment: `/// <summary>Create a new delivery note</summary>`
    - [ ] Extract principal ID from User claims: `var principalId = User.GetPrincipalId();`
    - [ ] Check authorization: `if (!await _authorizationService.CanCreateDeliveryNoteAsync(principalId, request.OrderId, ct)) return Forbid();`
    - [ ] Call service: `var result = await _deliveryNoteService.CreateDeliveryNoteAsync(request, principalId, ct);`
    - [ ] Return: `CreatedAtAction(nameof(GetDeliveryNote), new { deliveryNoteId = result.DeliveryNoteId }, result);`
  - [ ] Implement `GET /delivery/v1/delivery-notes/{deliveryNoteId}`:
    - [ ] Add `[HttpGet("{deliveryNoteId}")]` attribute
    - [ ] Add response type attributes
    - [ ] Add XML doc comment: `/// <summary>Get delivery note by ID</summary>`
    - [ ] Call service
    - [ ] Return `NotFound()` if null
    - [ ] Check authorization before returning
    - [ ] Return `Ok(result)`
  - [ ] Implement `GET /delivery/v1/delivery-notes`:
    - [ ] Add `[HttpGet]` attribute
    - [ ] Add query parameters: `[FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? orderId, [FromQuery] Guid? customerId, [FromQuery] DeliveryStatus? status, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate`
    - [ ] Add XML doc comment: `/// <summary>List delivery notes with filters</summary>`
    - [ ] Call service with filters
    - [ ] Return `Ok(result)`
  - [ ] Implement `PUT /delivery/v1/delivery-notes/{deliveryNoteId}`:
    - [ ] Add `[HttpPut("{deliveryNoteId}")]` attribute
    - [ ] Add authorization check
    - [ ] Call service
    - [ ] Return `Ok(result)` or `NotFound()`
  - [ ] Implement `PATCH /delivery/v1/delivery-notes/{deliveryNoteId}/status`:
    - [ ] Add `[HttpPatch("{deliveryNoteId}/status")]` attribute
    - [ ] Add XML doc comment: `/// <summary>Update delivery status</summary>`
    - [ ] Add authorization check
    - [ ] Call service
    - [ ] Return `Ok(result)` or `NotFound()`
  - [ ] Implement `DELETE /delivery/v1/delivery-notes/{deliveryNoteId}`:
    - [ ] Add `[HttpDelete("{deliveryNoteId}")]` attribute
    - [ ] Add authorization check
    - [ ] Call service
    - [ ] Return `NoContent()`
  - [ ] Implement `POST /delivery/v1/delivery-notes/{deliveryNoteId}/files`:
    - [ ] Add `[HttpPost("{deliveryNoteId}/files")]` attribute
    - [ ] Accept `IFormFile` parameter
    - [ ] Add authorization check
    - [ ] Call service
    - [ ] Return `CreatedAtAction()`
  - [ ] Implement `GET /delivery/v1/delivery-notes/{deliveryNoteId}/files`:
    - [ ] Add `[HttpGet("{deliveryNoteId}/files")]` attribute
    - [ ] Call service
    - [ ] Return `Ok(result)`
  - [ ] Implement `POST /delivery/v1/delivery-notes/{deliveryNoteId}/generate-pdf`:
    - [ ] Add `[HttpPost("{deliveryNoteId}/generate-pdf")]` attribute
    - [ ] Add `[ProducesResponseType(StatusCodes.Status202Accepted)]`
    - [ ] Call service
    - [ ] Return `Accepted(new { pdfUrl = result })`

### Program.cs Configuration

- [ ] Update `Program.cs` in `Maliev.DeliveryService.Api`:
  - [ ] Add Aspire ServiceDefaults: `builder.AddServiceDefaults();`
  - [ ] Add PostgreSQL DbContext: `builder.AddNpgsqlDbContext<DeliveryDbContext>("delivery-db");`
  - [ ] Add Redis: `builder.AddRedisClient("redis");`
  - [ ] Configure MassTransit with RabbitMQ:
    - [ ] Add consumers: `x.AddConsumer<OrderCompletedEventConsumer>();`
    - [ ] Configure RabbitMQ: `x.UsingRabbitMq((context, cfg) => { cfg.Host(builder.Configuration.GetConnectionString("messaging")); cfg.ConfigureEndpoints(context); });`
  - [ ] Add HTTP client for OrderService: `builder.Services.AddHttpClient<IOrderServiceClient, OrderServiceClient>(...)`
  - [ ] Add API versioning: `builder.Services.AddApiVersioning(...)`
  - [ ] Add Scalar (API documentation): `builder.Services.AddEndpointsApiExplorer(); builder.Services.AddOpenApi();`
  - [ ] Configure app:
    - [ ] Map default endpoints: `app.MapDefaultEndpoints();`
    - [ ] Map Scalar UI (dev only): `if (app.Environment.IsDevelopment()) { app.MapOpenApi(); app.MapScalarApiReference(); }`
    - [ ] Run migrations on startup: `using (var scope = app.Services.CreateScope()) { var dbContext = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>(); await dbContext.Database.MigrateAsync(); }`
    - [ ] Add authorization: `app.UseAuthorization();`
    - [ ] Map controllers: `app.MapControllers();`

### Verification

- [ ] Run `dotnet build Maliev.DeliveryService.slnx` — zero warnings
- [ ] Run Aspire: `dotnet run --project B:\maliev\Maliev.Aspire\Maliev.Aspire.AppHost\Maliev.Aspire.AppHost.csproj`
- [ ] Verify DeliveryService appears in Aspire dashboard
- [ ] Access Scalar API docs at `https://localhost:<port>/scalar/v1`
- [ ] Verify all endpoints documented

---

## WP-5.5: Messaging Integration

**Goal:** Add event contracts and implement publishers/consumers

### Event Contracts

- [ ] Open `B:\maliev\Maliev.MessagingContracts\generated\csharp\Contracts\`
- [ ] Create `DeliveryNoteEvents.cs`:
  - [ ] Add namespace: `namespace Maliev.MessagingContracts.Contracts;`
  - [ ] Create `DeliveryNoteCreatedEvent` record:
    - [ ] Add XML doc comment: `/// <summary>Published when a delivery note is created</summary>`
    - [ ] Add properties: `DeliveryNoteId`, `OrderId`, `PurchaseOrderId`, `CustomerId`, `DeliveryDate`, `ItemCount`, `CreatedAt`, `CreatedBy`
  - [ ] Create `DeliveryStatusChangedEvent` record:
    - [ ] Add XML doc comment: `/// <summary>Published when delivery status changes</summary>`
    - [ ] Add properties: `DeliveryNoteId`, `OrderId`, `PreviousStatus`, `NewStatus`, `ActualDeliveryTime`, `ReceivedByName`, `ChangedAt`, `ChangedBy`
  - [ ] Create `DeliveryCompletedEvent` record:
    - [ ] Add XML doc comment: `/// <summary>Published when delivery is completed (all items delivered)</summary>`
    - [ ] Add properties: `DeliveryNoteId`, `OrderId`, `PurchaseOrderId`, `CompletedAt`, `ReceivedByName`
  - [ ] Create `DeliveryNotePdfRequestedEvent` record:
    - [ ] Add XML doc comment: `/// <summary>Request PDF generation for delivery note</summary>`
    - [ ] Add properties: `DeliveryNoteId`, `RequestedBy`, `RequestedAt`
- [ ] Rebuild MessagingContracts: `dotnet build Maliev.MessagingContracts.slnx`
- [ ] Verify build succeeds and all projects reference updated contracts

### Event Publishers

- [ ] In `DeliveryNoteService.CreateDeliveryNoteAsync`, add after `SaveChangesAsync()`:
  - [ ] Publish event: `await _publishEndpoint.Publish(new DeliveryNoteCreatedEvent { DeliveryNoteId = deliveryNote.DeliveryNoteId, OrderId = deliveryNote.OrderId, PurchaseOrderId = deliveryNote.PurchaseOrderId, CustomerId = deliveryNote.CustomerId, DeliveryDate = deliveryNote.DeliveryDate, ItemCount = deliveryNote.Items.Count, CreatedAt = deliveryNote.CreatedAt, CreatedBy = deliveryNote.CreatedBy }, ct);`
  - [ ] Log event: `_logger.LogInformation("Published DeliveryNoteCreatedEvent for {DeliveryNoteId}", deliveryNote.DeliveryNoteId);`
- [ ] In `DeliveryNoteService.UpdateDeliveryStatusAsync`, add after `SaveChangesAsync()`:
  - [ ] Publish status changed event: `await _publishEndpoint.Publish(new DeliveryStatusChangedEvent { DeliveryNoteId = deliveryNote.DeliveryNoteId, OrderId = deliveryNote.OrderId, PreviousStatus = previousStatus.ToString(), NewStatus = newStatus.ToString(), ActualDeliveryTime = deliveryNote.ActualDeliveryTime, ReceivedByName = deliveryNote.ReceivedByName, ChangedAt = DateTime.UtcNow, ChangedBy = updatedBy }, ct);`
  - [ ] If status = Delivered, publish completed event: `if (newStatus == DeliveryStatus.Delivered) { await _publishEndpoint.Publish(new DeliveryCompletedEvent { ... }, ct); }`
- [ ] In `DeliveryNoteService.GeneratePdfAsync`, add:
  - [ ] Publish PDF request event: `await _publishEndpoint.Publish(new DeliveryNotePdfRequestedEvent { DeliveryNoteId = deliveryNoteId, RequestedBy = requestedBy, RequestedAt = DateTime.UtcNow }, ct);`

### Event Consumers (Optional)

- [ ] Create `Maliev.DeliveryService.Api/Consumers/` directory
- [ ] Create `OrderCompletedEventConsumer.cs` (optional feature):
  - [ ] Implement `IConsumer<OrderCompletedEvent>`
  - [ ] In `Consume()` method:
    - [ ] Fetch order details from OrderService
    - [ ] Auto-create draft delivery note with all order items
    - [ ] Set status to Pending
    - [ ] Log: `_logger.LogInformation("Auto-created delivery note draft for order {OrderId}", message.OrderId);`
- [ ] Register consumer in `Program.cs` MassTransit configuration (already done in WP-5.4)

### HTTP Client to OrderService

- [ ] Create `Maliev.DeliveryService.Api/Clients/` directory
- [ ] Create `IOrderServiceClient.cs` interface:
  - [ ] Add method: `Task<OrderDetailsDto?> GetOrderAsync(string orderId, CancellationToken ct = default);`
  - [ ] Add method: `Task UpdateActualDeliveryDateAsync(string orderId, DateTime deliveryDate, CancellationToken ct = default);`
- [ ] Create `OrderServiceClient.cs`:
  - [ ] Add constructor with `HttpClient`
  - [ ] Implement `GetOrderAsync`: `return await _httpClient.GetFromJsonAsync<OrderDetailsDto>($"/order/v1/orders/{orderId}", ct);`
  - [ ] Implement `UpdateActualDeliveryDateAsync`: `var response = await _httpClient.PatchAsJsonAsync($"/order/v1/orders/{orderId}/actual-delivery-date", new { actualDeliveryDate = deliveryDate }, ct); response.EnsureSuccessStatusCode();`
- [ ] Register client in `Program.cs` (already done in WP-5.4)

---

## WP-5.6: Testing

**Goal:** Write unit tests and integration tests with Testcontainers

### Unit Tests

- [ ] Create `Maliev.DeliveryService.Tests/Unit/Services/` directory
- [ ] Create `DeliveryNoteServiceTests.cs`:
  - [ ] Add test: `CreateDeliveryNoteAsync_ValidRequest_ReturnsDeliveryNote`:
    - [ ] Arrange: Create in-memory DbContext, fake publisher, service instance
    - [ ] Act: Call `CreateDeliveryNoteAsync` with valid request
    - [ ] Assert: Result not null, ID starts with "DN-2026-", items count correct, event published
  - [ ] Add test: `CreateDeliveryNoteAsync_DeliveredExceedsManufactured_ThrowsValidationException`:
    - [ ] Arrange: Request with delivered > manufactured
    - [ ] Act & Assert: `await Assert.ThrowsAsync<ValidationException>(...)`
  - [ ] Add test: `CreateDeliveryNoteAsync_NoItems_ThrowsValidationException`:
    - [ ] Arrange: Request with empty items list
    - [ ] Act & Assert: Throws validation exception
  - [ ] Add test: `UpdateDeliveryStatusAsync_ValidTransition_UpdatesStatus`:
    - [ ] Arrange: Create delivery note with status Pending
    - [ ] Act: Update status to InTransit
    - [ ] Assert: Status updated, event published
  - [ ] Add test: `UpdateDeliveryStatusAsync_InvalidTransition_ThrowsInvalidOperationException`:
    - [ ] Arrange: Delivery note with status Delivered
    - [ ] Act: Attempt to change to Pending
    - [ ] Assert: Throws `InvalidOperationException`
  - [ ] Add test: `UpdateDeliveryStatusAsync_ToDeliveredWithoutReceivedBy_ThrowsValidationException`:
    - [ ] Arrange: Status change to Delivered without `ReceivedByName`
    - [ ] Assert: Throws validation exception
  - [ ] Add test: `DeleteDeliveryNoteAsync_PendingStatus_SoftDeletes`:
    - [ ] Arrange: Delivery note with status Pending
    - [ ] Act: Delete
    - [ ] Assert: `IsDeleted = true`, `DeletedAt` set
  - [ ] Add test: `DeleteDeliveryNoteAsync_DeliveredStatus_ThrowsInvalidOperationException`:
    - [ ] Arrange: Delivery note with status Delivered
    - [ ] Act: Attempt delete
    - [ ] Assert: Throws exception
- [ ] Create `Maliev.DeliveryService.Tests/Unit/DTOs/` directory
- [ ] Create `DtoMappingTests.cs`:
  - [ ] Add test: `ToEntity_ValidRequest_MapsCorrectly`:
    - [ ] Arrange: Create `CreateDeliveryNoteRequest`
    - [ ] Act: Call `request.ToEntity()`
    - [ ] Assert: All fields mapped correctly
  - [ ] Add test: `ToResponse_ValidEntity_MapsCorrectly`:
    - [ ] Arrange: Create `DeliveryNote` entity
    - [ ] Act: Call `entity.ToResponse()`
    - [ ] Assert: All fields mapped correctly
- [ ] Create `Maliev.DeliveryService.Tests/Fakes/` directory
- [ ] Create `FakePublishEndpoint.cs`:
  - [ ] Implement `IPublishEndpoint`
  - [ ] Track published messages in `List<object> PublishedMessages`
  - [ ] Implement `Publish()` to add message to list

### Integration Tests with Testcontainers

- [ ] Create `Maliev.DeliveryService.Tests/Integration/` directory
- [ ] Create `Maliev.DeliveryService.Tests/Integration/TestFixtures/` directory
- [ ] Create `DeliveryServiceTestFixture.cs`:
  - [ ] Implement `IAsyncLifetime`
  - [ ] Add fields: `PostgreSqlContainer _postgresContainer`, `RabbitMqContainer _rabbitMqContainer`, `WebApplicationFactory<Program> _factory`
  - [ ] Implement `InitializeAsync()`:
    - [ ] Start PostgreSQL container: `_postgresContainer = new PostgreSqlBuilder().WithImage("postgres:18").WithDatabase("test_delivery_db").Build(); await _postgresContainer.StartAsync();`
    - [ ] Start RabbitMQ container: `_rabbitMqContainer = new RabbitMqBuilder().WithImage("rabbitmq:3-management").Build(); await _rabbitMqContainer.StartAsync();`
    - [ ] Create `WebApplicationFactory` with overridden configuration
    - [ ] Replace DbContext with test container connection
    - [ ] Run migrations: `var dbContext = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>(); await dbContext.Database.MigrateAsync();`
  - [ ] Implement `DisposeAsync()`: Dispose containers and factory
- [ ] Create `Maliev.DeliveryService.Tests/Integration/Controllers/` directory
- [ ] Create `DeliveryNotesControllerTests.cs`:
  - [ ] Use `DeliveryServiceTestFixture` as class fixture
  - [ ] Add test: `CreateDeliveryNote_ValidRequest_ReturnsCreated`:
    - [ ] Arrange: Create HTTP client, valid request
    - [ ] Act: POST to `/delivery/v1/delivery-notes`
    - [ ] Assert: Status code 201, response not null, ID starts with "DN-2026-"
  - [ ] Add test: `GetDeliveryNote_ExistingId_ReturnsOk`:
    - [ ] Arrange: Create delivery note via POST
    - [ ] Act: GET by ID
    - [ ] Assert: Status code 200, response matches created note
  - [ ] Add test: `GetDeliveryNote_NonExistingId_ReturnsNotFound`:
    - [ ] Act: GET with invalid ID
    - [ ] Assert: Status code 404
  - [ ] Add test: `ListDeliveryNotes_WithFilters_ReturnsFiltered`:
    - [ ] Arrange: Create multiple delivery notes
    - [ ] Act: GET with orderId filter
    - [ ] Assert: Returns only matching notes
  - [ ] Add test: `UpdateDeliveryStatus_ValidTransition_ReturnsOk`:
    - [ ] Arrange: Create delivery note with status Pending
    - [ ] Act: PATCH status to InTransit
    - [ ] Assert: Status code 200, status updated
  - [ ] Add test: `DeleteDeliveryNote_PendingStatus_ReturnsNoContent`:
    - [ ] Arrange: Create delivery note with status Pending
    - [ ] Act: DELETE
    - [ ] Assert: Status code 204
    - [ ] Verify: GET returns 404 (soft deleted)
- [ ] Create `Maliev.DeliveryService.Tests/Integration/Database/` directory
- [ ] Create `DeliveryDbContextTests.cs`:
  - [ ] Add test: `QueryFilter_ExcludesSoftDeletedEntities`:
    - [ ] Arrange: Create delivery note, soft delete it
    - [ ] Act: Query all delivery notes
    - [ ] Assert: Soft deleted note not included
  - [ ] Add test: `ConcurrencyCheck_PreventsConflicts`:
    - [ ] Arrange: Load same delivery note in two contexts
    - [ ] Act: Update in both contexts, save first
    - [ ] Assert: Second save throws `DbUpdateConcurrencyException`

### Test Execution

- [ ] Run unit tests: `dotnet test Maliev.DeliveryService.slnx --filter Category=Unit`
- [ ] Run integration tests: `dotnet test Maliev.DeliveryService.slnx --filter Category=Integration`
- [ ] Run all tests: `dotnet test Maliev.DeliveryService.slnx`
- [ ] Verify 80%+ code coverage (use `dotnet test --collect:"XPlat Code Coverage"`)
- [ ] Verify all tests pass: zero failures

---

## Final Verification

- [ ] Run `dotnet build Maliev.DeliveryService.slnx` — zero warnings, zero errors
- [ ] Run `dotnet test Maliev.DeliveryService.slnx` — all tests pass
- [ ] Run `dotnet build Maliev.Aspire.slnx` — verify DeliveryService integrated
- [ ] Run Aspire: `dotnet run --project B:\maliev\Maliev.Aspire\Maliev.Aspire.AppHost\Maliev.Aspire.AppHost.csproj`
- [ ] Verify in Aspire dashboard:
  - [ ] DeliveryService appears and is running
  - [ ] Database migration completed successfully
  - [ ] Redis connection healthy
  - [ ] RabbitMQ connection healthy
- [ ] Access Scalar API docs at `https://localhost:<port>/scalar/v1`
  - [ ] Verify all 9 endpoints documented
  - [ ] Test "Create Delivery Note" endpoint via Scalar UI
  - [ ] Verify sequential ID generation (DN-2026-000001, DN-2026-000002, etc.)
- [ ] Test event publishing:
  - [ ] Create delivery note via API
  - [ ] Verify `DeliveryNoteCreatedEvent` published to RabbitMQ (check RabbitMQ Management UI)
  - [ ] Update status to Delivered
  - [ ] Verify `DeliveryStatusChangedEvent` and `DeliveryCompletedEvent` published
- [ ] Test authorization:
  - [ ] Attempt API call without Bearer token → expect 401 Unauthorized
  - [ ] Attempt API call with token lacking `Delivery.Create` permission → expect 403 Forbidden
- [ ] Test soft delete:
  - [ ] Create delivery note with status Pending
  - [ ] Delete via API
  - [ ] Verify GET returns 404
  - [ ] Query database directly with `.IgnoreQueryFilters()` → verify `is_deleted = true`
- [ ] Test status transitions:
  - [ ] Create delivery note (status = Pending)
  - [ ] Update to InTransit → success
  - [ ] Update to Delivered with `ActualDeliveryTime` and `ReceivedByName` → success
  - [ ] Attempt to update to Pending → expect 400 Bad Request with validation error
- [ ] Performance check:
  - [ ] Use `ab` (ApacheBench) or `hey` to load test: `hey -n 1000 -c 10 https://localhost:<port>/delivery/v1/delivery-notes`
  - [ ] Verify p95 latency < 200ms for GET requests

---

## Success Criteria

- [x] All WP-5.1 tasks completed (project setup, Aspire integration)
- [x] All WP-5.2 tasks completed (database schema, entities, migrations)
- [x] All WP-5.3 tasks completed (DTOs, service layer, business logic)
- [x] All WP-5.4 tasks completed (API controllers, endpoints, Program.cs)
- [x] All WP-5.5 tasks completed (event contracts, publishers, consumers)
- [x] All WP-5.6 tasks completed (unit tests, integration tests with Testcontainers)
- [x] Build verification: `dotnet build Maliev.DeliveryService.slnx` — zero warnings
- [x] Test verification: `dotnet test Maliev.DeliveryService.slnx` — all tests pass, 80%+ coverage
- [x] Aspire dashboard shows DeliveryService running
- [x] Scalar API documentation accessible
- [x] Sequential ID generation works (DN-YYYY-XXXXXX format)
- [x] Event publishing to RabbitMQ confirmed
- [x] Authorization checks enforce permissions
- [x] Soft delete behavior works correctly
- [x] Status transitions validated correctly
- [x] Integration tests pass with real PostgreSQL and RabbitMQ containers

---

**Estimated Time**: 19-26 days (approximately 4-5 weeks)
**Priority**: Critical (Thai business operations requirement)
**Dependencies**: None (standalone microservice)
**Next Steps**: After WP-5 completion, proceed to WP-6 (PdfService modifications) and WP-7 (Intranet frontend)
