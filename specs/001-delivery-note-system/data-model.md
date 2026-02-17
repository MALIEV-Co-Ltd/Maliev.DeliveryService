# Phase 1: Data Model

**Feature**: Delivery Note System
**Date**: 2026-02-16

## Overview

This document defines the data model for the Delivery Note System, including entities, relationships, validation rules, and state transitions.

## Entity Relationship Diagram

```
┌─────────────────┐
│    Address      │
│─────────────────│
│ Id (PK)         │
│ CompanyName     │
│ ContactName     │
│ AddressLine1    │
│ AddressLine2    │
│ City            │
│ StateProvince   │
│ PostalCode      │
│ Country         │
│ PhoneNumber     │
│ EmailAddress    │
└─────────────────┘
         ▲
         │ 0..1
         │
         │ (snapshot reference)
         │
┌─────────────────────────────┐
│      DeliveryNote           │
│─────────────────────────────│
│ DeliveryNoteId (PK)         │◄──────────┐
│ OrderId (FK, nullable)      │           │
│ PurchaseOrderId (nullable)  │           │ 1
│ CustomerId                  │           │
│ CustomerName                │           │
│ DeliveryDate                │           │
│ ActualDeliveryTime          │           │
│ Status                      │           │
│ ShippingAddressId (FK)      │           │
│ ShippingAddress* (snapshot) │           │
│ DeliveryContact*            │           │
│ CarrierName                 │           │
│ TrackingNumber              │           │
│ ShippingCost                │           │
│ ReceivedByName              │           │
│ SignatureFileId             │           │
│ SignedAt                    │           │
│ InternalNotes               │           │
│ DeliveryInstructions        │           │
│ CreatedAt, CreatedBy        │           │
│ UpdatedAt, UpdatedBy        │           │
│ RowVersion                  │           │
│ IsDeleted, DeletedAt        │           │
└─────────────────────────────┘           │
         │ 1                              │
         │                                │
         │ *                              │
         ▼                                │
┌────────────────────────┐                │
│  DeliveryNoteItem      │                │
│────────────────────────│                │
│ Id (PK)                │                │
│ DeliveryNoteId (FK)    │────────────────┘
│ OrderId                │
│ PurchaseOrderItemId    │
│ ProductCode            │
│ ProductName            │
│ ProductDescription     │
│ QuantityOrdered        │
│ QuantityManufactured   │
│ QuantityDelivered      │
│ UnitOfMeasure          │
│ ItemNotes              │
└────────────────────────┘

         │ 1
         │
         │ *
         ▼
┌────────────────────────┐
│  DeliveryNoteFile      │
│────────────────────────│
│ Id (PK)                │
│ DeliveryNoteId (FK)    │────────────────┘
│ FileName               │
│ StorageUrl             │
│ ContentType            │
│ FileSize               │
│ FileType               │
│ UploadedAt             │
│ UploadedBy             │
│ IsDeleted              │
└────────────────────────┘
```

## Entities

### DeliveryNote

Primary aggregate root representing a shipment of goods.

**Table**: `delivery_notes`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| DeliveryNoteId | VARCHAR(50) | PK, NOT NULL | Unique identifier "DN-YYYY-XXXXXX" |
| OrderId | VARCHAR(50) | NULL | Reference to OrderService (customer orders) |
| PurchaseOrderId | INTEGER | NULL | Reference to PurchaseOrderService (supplier deliveries) |
| CustomerId | UUID | NOT NULL | Customer receiving goods |
| CustomerName | VARCHAR(500) | NULL | Cached customer name for display |
| DeliveryDate | TIMESTAMP | NOT NULL | Scheduled delivery date |
| ActualDeliveryTime | TIMESTAMP | NULL | When goods were actually delivered |
| Status | VARCHAR(50) | NOT NULL | Current delivery status (enum) |
| ShippingAddressId | UUID | NULL | Reference to Address entity |
| ShippingAddressLine1 | VARCHAR(500) | NULL | Denormalized address snapshot |
| ShippingAddressLine2 | VARCHAR(500) | NULL | Denormalized address snapshot |
| ShippingCity | VARCHAR(200) | NULL | Denormalized address snapshot |
| ShippingProvince | VARCHAR(200) | NULL | Denormalized address snapshot |
| ShippingPostalCode | VARCHAR(20) | NULL | Denormalized address snapshot |
| ShippingCountry | VARCHAR(100) | NULL | Denormalized address snapshot |
| DeliveryContactName | VARCHAR(200) | NULL | Who to contact at delivery location |
| DeliveryContactPhone | VARCHAR(50) | NULL | Contact phone number |
| DeliveryContactEmail | VARCHAR(200) | NULL | Contact email |
| CarrierName | VARCHAR(200) | NULL | Shipping company (e.g., "Kerry Express") |
| TrackingNumber | VARCHAR(200) | NULL | Carrier's tracking number |
| ShippingCost | DECIMAL(18,2) | NULL | Cost of shipping |
| ShippingCostCurrency | VARCHAR(10) | NULL | Currency code (e.g., "THB", "USD") |
| ReceivedByName | VARCHAR(200) | NULL | Who signed for delivery |
| SignatureFileId | UUID | NULL | Reference to signature image |
| SignedAt | TIMESTAMP | NULL | When delivery was signed for |
| InternalNotes | TEXT | NULL | Employee-only notes |
| DeliveryInstructions | TEXT | NULL | Customer-visible delivery instructions |
| CreatedAt | TIMESTAMP | NOT NULL, DEFAULT NOW() | When record was created |
| CreatedBy | VARCHAR(200) | NOT NULL | Who created the record |
| UpdatedAt | TIMESTAMP | NULL | Last update timestamp |
| UpdatedBy | VARCHAR(200) | NULL | Who last updated |
| RowVersion | SERIAL | NOT NULL | Optimistic concurrency token |
| IsDeleted | BOOLEAN | NOT NULL, DEFAULT FALSE | Soft delete flag |
| DeletedAt | TIMESTAMP | NULL | When record was soft-deleted |
| DeletedBy | VARCHAR(200) | NULL | Who soft-deleted the record |

**Indexes**:
- `idx_delivery_notes_order_id` ON (order_id) WHERE NOT is_deleted
- `idx_delivery_notes_customer_id` ON (customer_id) WHERE NOT is_deleted
- `idx_delivery_notes_status` ON (status) WHERE NOT is_deleted
- `idx_delivery_notes_delivery_date` ON (delivery_date) WHERE NOT is_deleted
- `idx_delivery_notes_tracking_number` ON (tracking_number) WHERE NOT is_deleted

**Business Rules**:
1. Either OrderId OR PurchaseOrderId must be provided (not both null)
2. Status transitions must follow valid state machine
3. ActualDeliveryTime and ReceivedByName required when Status = "Delivered"
4. Only "Pending" status can be soft-deleted
5. Address snapshot captured at creation time (immutable for audit trail)

### DeliveryNoteItem

Individual line items within a delivery note.

**Table**: `delivery_note_items`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | BIGSERIAL | PK, NOT NULL | Auto-increment identifier |
| DeliveryNoteId | VARCHAR(50) | FK, NOT NULL | Parent delivery note |
| OrderId | VARCHAR(50) | NULL | Reference to order line item |
| PurchaseOrderItemId | BIGINT | NULL | Reference to purchase order line item |
| ProductCode | VARCHAR(100) | NULL | Product identifier |
| ProductName | VARCHAR(500) | NULL | Product name (cached) |
| ProductDescription | TEXT | NULL | Product description |
| QuantityOrdered | DECIMAL(18,4) | NOT NULL, CHECK >= 0 | How much customer ordered |
| QuantityManufactured | DECIMAL(18,4) | NOT NULL, CHECK >= 0 | How much was actually made |
| QuantityDelivered | DECIMAL(18,4) | NOT NULL, CHECK > 0 | How much is in THIS delivery |
| UnitOfMeasure | VARCHAR(50) | NOT NULL | Unit (pcs, kg, m, etc.) |
| ItemNotes | TEXT | NULL | Item-specific notes |
| CreatedAt | TIMESTAMP | NOT NULL, DEFAULT NOW() | When record was created |

**Indexes**:
- `idx_delivery_note_items_delivery_note_id` ON (delivery_note_id)

**Constraints**:
- `CHECK (quantity_ordered >= 0)`
- `CHECK (quantity_manufactured >= 0)`
- `CHECK (quantity_delivered > 0)`
- `CHECK (quantity_delivered <= quantity_manufactured)`

**Business Rules**:
1. QuantityDelivered must be positive (cannot deliver zero or negative)
2. QuantityDelivered ≤ QuantityManufactured (cannot deliver more than manufactured)
3. Sum of QuantityDelivered across all delivery notes for an order item ≤ QuantityOrdered (application-level check)

### DeliveryNoteFile

Attached files (signatures, photos, packing lists).

**Table**: `delivery_note_files`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | UUID | PK, NOT NULL, DEFAULT gen_random_uuid() | Unique identifier |
| DeliveryNoteId | VARCHAR(50) | FK, NOT NULL | Parent delivery note |
| FileName | VARCHAR(500) | NOT NULL | Original file name |
| StorageUrl | VARCHAR(2000) | NOT NULL | Google Cloud Storage URL |
| ContentType | VARCHAR(200) | NOT NULL | MIME type (image/jpeg, application/pdf) |
| FileSize | BIGINT | NOT NULL | File size in bytes |
| FileType | VARCHAR(50) | NOT NULL | Type of attachment (enum) |
| UploadedAt | TIMESTAMP | NOT NULL, DEFAULT NOW() | When file was uploaded |
| UploadedBy | VARCHAR(200) | NOT NULL | Employee who uploaded |
| IsDeleted | BOOLEAN | NOT NULL, DEFAULT FALSE | Soft delete flag |
| DeletedAt | TIMESTAMP | NULL | When file was soft-deleted |

**Indexes**:
- `idx_delivery_note_files_delivery_note_id` ON (delivery_note_id) WHERE NOT is_deleted

**Business Rules**:
1. Files managed via UploadService (virus scanning, size limits)
2. Maximum file size: 5MB per file
3. Allowed MIME types: image/*, application/pdf

### Address

Shipping address entity (may be shared across multiple delivery notes).

**Table**: `addresses`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | UUID | PK, NOT NULL, DEFAULT gen_random_uuid() | Unique identifier |
| CompanyName | VARCHAR(500) | NULL | Company name |
| ContactName | VARCHAR(200) | NULL | Contact person |
| AddressLine1 | VARCHAR(500) | NOT NULL | Primary address line |
| AddressLine2 | VARCHAR(500) | NULL | Secondary address line (apt, suite) |
| City | VARCHAR(200) | NOT NULL | City |
| StateProvince | VARCHAR(200) | NOT NULL | State/Province |
| PostalCode | VARCHAR(20) | NOT NULL | Postal/ZIP code |
| Country | VARCHAR(100) | NOT NULL | Country |
| PhoneNumber | VARCHAR(50) | NULL | Phone number |
| EmailAddress | VARCHAR(200) | NULL | Email address |
| CreatedAt | TIMESTAMP | NOT NULL, DEFAULT NOW() | When record was created |
| CreatedBy | VARCHAR(200) | NOT NULL | Who created the record |
| UpdatedAt | TIMESTAMP | NULL | Last update timestamp |
| UpdatedBy | VARCHAR(200) | NULL | Who last updated |

**Business Rules**:
1. Address snapshot copied to DeliveryNote at creation time (denormalized fields)
2. Changes to Address entity do NOT affect existing delivery notes (immutable snapshot)

## Enumerations

### DeliveryStatus

Represents the current state of a delivery note.

```csharp
public enum DeliveryStatus
{
    Pending,            // Created but not yet shipped
    InTransit,          // Shipped, on the way
    Delivered,          // Fully delivered and signed for
    PartiallyDelivered, // Some items delivered, more to come
    Cancelled           // Delivery cancelled
}
```

**Storage**: Stored as VARCHAR(50) in PostgreSQL using string conversion.

### DeliveryFileType

Categorizes attached files.

```csharp
public enum DeliveryFileType
{
    Signature,      // Delivery signature
    Photo,          // Package photo
    Invoice,        // Related invoice
    PackingList,    // Packing list
    Other           // Other supporting documents
}
```

**Storage**: Stored as VARCHAR(50) in PostgreSQL using string conversion.

## State Machine: DeliveryStatus

```
┌──────────┐
│ Pending  │
└────┬─────┘
     │
     ├─────────► InTransit ──┬──────► Delivered (terminal)
     │                       │
     │                       └──────► PartiallyDelivered ───► Delivered (terminal)
     │                       │
     │                       └──────► Cancelled (terminal)
     │
     └─────────► Cancelled (terminal)
```

**Valid Transitions**:
- Pending → InTransit, Cancelled
- InTransit → Delivered, PartiallyDelivered, Cancelled
- PartiallyDelivered → Delivered, Cancelled
- Delivered → (no transitions, terminal state)
- Cancelled → (no transitions, terminal state)

**Transition Rules**:
1. Cannot transition FROM terminal states (Delivered, Cancelled)
2. Transition to "Delivered" requires ActualDeliveryTime and ReceivedByName
3. Transition to "InTransit" should have CarrierName and TrackingNumber (warning if missing)
4. Cannot skip states (e.g., Pending → Delivered directly is invalid)

## Validation Rules

### DeliveryNote Validation

**On Create**:
1. Must have either OrderId OR PurchaseOrderId (at least one)
2. Must have at least one item
3. All item quantities must be positive
4. QuantityDelivered ≤ QuantityManufactured for each item
5. DeliveryDate cannot be in the past (warning only, not error)

**On Update**:
1. Cannot modify DeliveryNoteId (immutable)
2. Cannot modify CreatedAt, CreatedBy (immutable)
3. Cannot modify RowVersion (managed by EF)

**On Status Change**:
1. Must follow valid status transitions
2. Transition to "Delivered" requires ActualDeliveryTime and ReceivedByName
3. Cannot transition from terminal states

**On Delete**:
1. Can only soft-delete records with Status = "Pending"
2. Cannot delete records with Status = "Delivered"

### DeliveryNoteItem Validation

1. QuantityDelivered > 0 (must deliver something)
2. QuantityDelivered ≤ QuantityManufactured (cannot over-deliver)
3. Sum of QuantityDelivered across all delivery notes for same order item ≤ QuantityOrdered (application-level check)

### Partial Delivery Validation

**Application-Level Checks**:
```csharp
// When creating delivery note for order
var existingDeliveries = await _context.DeliveryNotes
    .Include(dn => dn.Items)
    .Where(dn => dn.OrderId == request.OrderId && !dn.IsDeleted)
    .ToListAsync();

foreach (var requestItem in request.Items)
{
    var totalDelivered = existingDeliveries
        .SelectMany(dn => dn.Items)
        .Where(item => item.ProductCode == requestItem.ProductCode)
        .Sum(item => item.QuantityDelivered);

    var totalWithCurrent = totalDelivered + requestItem.QuantityDelivered;

    if (totalWithCurrent > requestItem.QuantityOrdered)
    {
        throw new ValidationException(
            $"Total delivered quantity ({totalWithCurrent}) exceeds ordered quantity ({requestItem.QuantityOrdered}) for {requestItem.ProductCode}");
    }
}
```

## Relationships

### DeliveryNote ↔ DeliveryNoteItem

- **Type**: One-to-Many
- **Ownership**: DeliveryNote owns DeliveryNoteItem (aggregate root pattern)
- **Cascade**: ON DELETE CASCADE (deleting delivery note deletes all items)
- **Loading**: Eager load with `.Include(dn => dn.Items)` when needed

### DeliveryNote ↔ DeliveryNoteFile

- **Type**: One-to-Many
- **Ownership**: DeliveryNote owns DeliveryNoteFile
- **Cascade**: ON DELETE CASCADE (deleting delivery note deletes all files)
- **Loading**: Explicit load when files are accessed

### DeliveryNote ↔ Address

- **Type**: Many-to-One (optional)
- **Ownership**: Address is independent entity
- **Cascade**: ON DELETE RESTRICT (cannot delete address if referenced)
- **Snapshot**: Address fields copied to DeliveryNote at creation (denormalized)
- **Immutability**: Address snapshot in DeliveryNote never changes (audit trail)

### External References

**DeliveryNote → OrderService.Order** (external):
- **Field**: OrderId (nullable)
- **Type**: String reference to external service
- **Resolution**: Via HTTP client call to OrderService API
- **Consistency**: Eventual consistency via events

**DeliveryNote → PurchaseOrderService.PurchaseOrder** (external):
- **Field**: PurchaseOrderId (nullable)
- **Type**: Integer reference to external service
- **Resolution**: Via HTTP client call to PurchaseOrderService API
- **Consistency**: Eventual consistency via events

## Concurrency Control

**Strategy**: Optimistic Concurrency via RowVersion

**Implementation**:
```csharp
[ConcurrencyCheck]
public int RowVersion { get; set; }
```

**Behavior**:
- RowVersion automatically incremented on each update
- If concurrent update detected, `DbUpdateConcurrencyException` thrown
- Client must reload entity and retry with latest version

**PostgreSQL Mapping**:
- Uses SERIAL column (auto-incrementing)
- Alternative: Use PostgreSQL's built-in `xmin` system column

## Soft Delete Pattern

**Implementation**:
```csharp
public bool IsDeleted { get; set; }
public DateTime? DeletedAt { get; set; }
public string? DeletedBy { get; set; }
```

**EF Query Filter**:
```csharp
builder.HasQueryFilter(e => !e.IsDeleted);
```

**Behavior**:
- All queries automatically exclude soft-deleted records
- Use `IgnoreQueryFilters()` to include soft-deleted records when needed
- Partial indexes exclude soft-deleted records for performance

## Data Migration Strategy

### Initial Migration

1. Create all tables with proper constraints
2. Create all indexes (including partial indexes)
3. Set default values where applicable
4. No seed data (production data comes from API)

### Future Migrations

**Breaking Changes** (require new API version):
- Removing columns
- Changing column types
- Removing constraints
- Changing nullable to NOT NULL

**Non-Breaking Changes** (same API version):
- Adding nullable columns
- Adding new tables
- Adding indexes
- Relaxing constraints

**Rollback Strategy**:
- EF migrations support `Up()` and `Down()` methods
- Always test migration and rollback in staging before production
- Keep database changes backward-compatible when possible

---

**Next Steps**: Create API contracts (OpenAPI schemas) and developer quickstart guide
