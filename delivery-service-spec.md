# Delivery Note System Specification

## Overview

The Maliev platform requires a formal delivery note (ใบส่งของ) system to manage shipments from the factory to customers. This system must track when goods are shipped, what quantities are delivered, and provide formal documentation for Thai business operations.

## Business Requirements

### Problem Statement

Currently, the Maliev platform tracks:
- **Orders** (OrderService) with promised delivery dates
- **Purchase Orders** (PurchaseOrderService) with actual delivery dates
- **Manufacturing** through various production tracking services

However, there is **no formal system** for:
1. Creating delivery documentation when goods leave the factory
2. Tracking partial deliveries (multiple shipments for a single order)
3. Recording item-level delivery details (ordered vs manufactured vs delivered quantities)
4. Generating PDF delivery notes for customers and logistics providers
5. Maintaining an audit trail of all deliveries

### Business Goals

1. **Compliance**: Generate formal delivery notes (ใบส่งของ) as required by Thai business practices
2. **Traceability**: Track what was shipped, when, to whom, and in what condition
3. **Reconciliation**: Match delivered quantities against orders and manufacturing output
4. **Customer Communication**: Provide formal documentation for customers to acknowledge receipt
5. **Logistics Integration**: Support tracking numbers and carrier information

### Use Cases

#### UC-1: Create Delivery Note from Order
**Actor**: Warehouse Staff, Logistics Coordinator
**Preconditions**: Order exists with manufactured items ready to ship
**Flow**:
1. User selects an order to create delivery note
2. System displays order items with:
   - Quantity ordered
   - Quantity manufactured
   - Quantity previously delivered (if partial delivery)
3. User specifies:
   - Quantity to deliver for each item
   - Delivery date
   - Shipping address (defaults from order)
   - Delivery contact information
   - Carrier and tracking number
   - Delivery instructions
4. System validates:
   - Delivered quantity ≤ manufactured quantity
   - At least one item selected
   - Required fields completed
5. System generates unique delivery note ID (DN-YYYY-XXXXXX)
6. System creates delivery note record
7. System publishes event to notify other services

**Postconditions**: Delivery note created with "Pending" status

#### UC-2: Update Delivery Status
**Actor**: Warehouse Staff, Logistics Coordinator
**Preconditions**: Delivery note exists
**Flow**:
1. User selects delivery note to update
2. User changes status:
   - **Pending** → **InTransit**: When shipment leaves factory
   - **InTransit** → **Delivered**: When customer receives goods
   - **InTransit** → **PartiallyDelivered**: If only some items delivered
3. For "Delivered" status, user records:
   - Actual delivery time
   - Name of person who received goods
   - Signature/photo (optional)
4. System validates state transition is allowed
5. System updates delivery note
6. System publishes status change event

**Postconditions**: Delivery note status updated, related services notified

#### UC-3: Handle Partial Delivery
**Actor**: Warehouse Staff
**Preconditions**: Order requires multiple shipments
**Flow**:
1. User creates first delivery note with partial quantities
2. System marks order as "PartiallyDelivered"
3. When remaining items ready, user creates second delivery note
4. System tracks cumulative delivered quantities
5. When all items delivered, system marks order as "Delivered"

**Postconditions**: Multiple delivery notes exist for single order, all quantities reconciled

#### UC-4: Generate Delivery Note PDF
**Actor**: Warehouse Staff, Logistics Coordinator, Customer
**Preconditions**: Delivery note exists
**Flow**:
1. User requests PDF generation for delivery note
2. System fetches delivery note data
3. System sends request to PdfService
4. PdfService generates Thai-language PDF with:
   - Delivery note number and date
   - Order reference
   - Customer information
   - Delivery address and contact
   - Items table (ordered/manufactured/delivered quantities)
   - Carrier and tracking information
   - Signature lines for sender and receiver
5. System returns PDF URL
6. User downloads or prints PDF

**Postconditions**: PDF available for download, printing, or email

#### UC-5: Track Delivery History
**Actor**: Customer Service, Management
**Preconditions**: None
**Flow**:
1. User searches delivery notes by:
   - Order ID
   - Customer
   - Date range
   - Status
   - Tracking number
2. System returns matching delivery notes
3. User views delivery note details including:
   - Full delivery information
   - Item-level details
   - Status history
   - Attached files (signatures, photos)

**Postconditions**: User has visibility into delivery history

## Domain Model

### Entities

#### DeliveryNote
The central entity representing a shipment of goods.

**Key Attributes**:
- `DeliveryNoteId` (string, PK): Unique identifier "DN-YYYY-XXXXXX"
- `OrderId` (string, nullable): Reference to order in OrderService
- `PurchaseOrderId` (int, nullable): Reference to purchase order in PurchaseOrderService
- `CustomerId` (Guid): Customer receiving the goods
- `CustomerName` (string): Cached customer name for display
- `DeliveryDate` (DateTime): Scheduled delivery date
- `ActualDeliveryTime` (DateTime, nullable): When goods were actually delivered
- `Status` (DeliveryStatus enum): Current status of delivery
- `ShippingAddressId` (Guid, nullable): Reference to shipping address
- `ShippingAddressSnapshot` (fields): Denormalized address for audit trail
- `DeliveryContactName` (string): Who to contact at delivery location
- `DeliveryContactPhone` (string): Contact phone number
- `DeliveryContactEmail` (string): Contact email
- `CarrierName` (string): Shipping company (e.g., "Kerry Express")
- `TrackingNumber` (string): Carrier's tracking number
- `ShippingCost` (decimal, nullable): Cost of shipping
- `ReceivedByName` (string): Who signed for delivery
- `SignatureFileId` (Guid, nullable): Reference to signature image
- `SignedAt` (DateTime, nullable): When delivery was signed for
- `InternalNotes` (string): Employee-only notes
- `DeliveryInstructions` (string): Customer-visible delivery instructions
- Standard metadata: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `RowVersion`
- Soft delete: `IsDeleted`, `DeletedAt`, `DeletedBy`

**Relationships**:
- Has many `DeliveryNoteItem`
- Has many `DeliveryNoteFile`
- References one `Address` (shipping address)
- References one `Order` (in OrderService) - external
- References one `PurchaseOrder` (in PurchaseOrderService) - external

#### DeliveryNoteItem
Individual line items in a delivery note.

**Key Attributes**:
- `Id` (long, PK): Auto-increment identifier
- `DeliveryNoteId` (string, FK): Parent delivery note
- `OrderId` (string, nullable): Reference to order line item
- `PurchaseOrderItemId` (long, nullable): Reference to purchase order line item
- `ProductCode` (string): Product identifier
- `ProductName` (string): Product name (cached)
- `ProductDescription` (string): Product description
- `QuantityOrdered` (decimal): How much customer ordered
- `QuantityManufactured` (decimal): How much was actually made
- `QuantityDelivered` (decimal): How much is in THIS delivery
- `UnitOfMeasure` (string): Unit (pcs, kg, m, etc.)
- `ItemNotes` (string): Item-specific notes (condition, packaging, etc.)

**Business Rules**:
- `QuantityDelivered` ≤ `QuantityManufactured` ≤ `QuantityOrdered` (typically)
- `QuantityDelivered` > 0
- Sum of `QuantityDelivered` across all delivery notes for an order item ≤ `QuantityOrdered`

#### DeliveryNoteFile
Attached files (signatures, photos, packing lists).

**Key Attributes**:
- `Id` (Guid, PK)
- `DeliveryNoteId` (string, FK): Parent delivery note
- `FileName` (string): Original file name
- `StorageUrl` (string): GCS storage URL
- `ContentType` (string): MIME type
- `FileSize` (long): File size in bytes
- `FileType` (DeliveryFileType enum): Type of attachment
- `UploadedAt` (DateTime)
- `UploadedBy` (string): Employee who uploaded

#### Address
Shipping address snapshot (may be shared entity).

**Key Attributes**:
- `Id` (Guid, PK)
- `CompanyName` (string)
- `ContactName` (string)
- `AddressLine1` (string)
- `AddressLine2` (string)
- `City` (string)
- `StateProvince` (string)
- `PostalCode` (string)
- `Country` (string)
- `PhoneNumber` (string)
- `EmailAddress` (string)

### Enumerations

#### DeliveryStatus
```
Pending            // Created but not yet shipped
InTransit          // Shipped, on the way
Delivered          // Fully delivered and signed for
PartiallyDelivered // Some items delivered, more to come
Cancelled          // Delivery cancelled
```

**Valid Transitions**:
- Pending → InTransit, Cancelled
- InTransit → Delivered, PartiallyDelivered, Cancelled
- PartiallyDelivered → Delivered, Cancelled
- Delivered → (terminal state)
- Cancelled → (terminal state)

#### DeliveryFileType
```
Signature      // Delivery signature
Photo          // Package photo
Invoice        // Related invoice
PackingList    // Packing list
Other          // Other supporting documents
```

## API Contracts

### Endpoints

#### Create Delivery Note
```
POST /delivery/v1/delivery-notes
Authorization: Bearer {token}
Permission: Delivery.Create

Request Body: CreateDeliveryNoteRequest
Response: 201 Created, DeliveryNoteResponse
```

#### Get Delivery Note
```
GET /delivery/v1/delivery-notes/{deliveryNoteId}
Authorization: Bearer {token}
Permission: Delivery.Read

Response: 200 OK, DeliveryNoteResponse
```

#### List Delivery Notes
```
GET /delivery/v1/delivery-notes
Query Parameters:
  - page (int, default=1)
  - pageSize (int, default=20)
  - orderId (string, optional)
  - customerId (Guid, optional)
  - status (DeliveryStatus, optional)
  - fromDate (DateTime, optional)
  - toDate (DateTime, optional)

Authorization: Bearer {token}
Permission: Delivery.Read

Response: 200 OK, PagedResponse<DeliveryNoteSummaryDto>
```

#### Update Delivery Note
```
PUT /delivery/v1/delivery-notes/{deliveryNoteId}
Authorization: Bearer {token}
Permission: Delivery.Update

Request Body: UpdateDeliveryNoteRequest
Response: 200 OK, DeliveryNoteResponse
```

#### Update Delivery Status
```
PATCH /delivery/v1/delivery-notes/{deliveryNoteId}/status
Authorization: Bearer {token}
Permission: Delivery.UpdateStatus

Request Body: UpdateDeliveryStatusRequest
Response: 200 OK, DeliveryNoteResponse
```

#### Delete Delivery Note
```
DELETE /delivery/v1/delivery-notes/{deliveryNoteId}
Authorization: Bearer {token}
Permission: Delivery.Delete

Response: 204 No Content
```

#### Add File Attachment
```
POST /delivery/v1/delivery-notes/{deliveryNoteId}/files
Authorization: Bearer {token}
Permission: Delivery.UpdateFiles
Content-Type: multipart/form-data

Form Data:
  - file: binary file data
  - fileType: DeliveryFileType

Response: 201 Created, DeliveryNoteFileResponse
```

#### List Files
```
GET /delivery/v1/delivery-notes/{deliveryNoteId}/files
Authorization: Bearer {token}
Permission: Delivery.Read

Response: 200 OK, List<DeliveryNoteFileResponse>
```

#### Generate PDF
```
POST /delivery/v1/delivery-notes/{deliveryNoteId}/generate-pdf
Authorization: Bearer {token}
Permission: Delivery.GeneratePdf

Response: 202 Accepted
Body: { "pdfUrl": "https://storage.googleapis.com/..." }
```

### Data Transfer Objects

#### CreateDeliveryNoteRequest
```json
{
  "orderId": "string (optional)",
  "purchaseOrderId": 0 (optional),
  "deliveryDate": "2026-02-16T00:00:00Z",
  "shippingAddressId": "guid (optional)",
  "deliveryContactName": "string",
  "deliveryContactPhone": "string",
  "deliveryContactEmail": "string",
  "carrierName": "string",
  "trackingNumber": "string",
  "deliveryInstructions": "string",
  "items": [
    {
      "orderId": "string (optional)",
      "purchaseOrderItemId": 0 (optional),
      "productCode": "string",
      "productName": "string",
      "quantityOrdered": 0.00,
      "quantityManufactured": 0.00,
      "quantityDelivered": 0.00,
      "unitOfMeasure": "string",
      "itemNotes": "string"
    }
  ]
}
```

#### UpdateDeliveryNoteRequest
```json
{
  "deliveryDate": "2026-02-16T00:00:00Z (optional)",
  "shippingAddressId": "guid (optional)",
  "deliveryContactName": "string (optional)",
  "deliveryContactPhone": "string (optional)",
  "carrierName": "string (optional)",
  "trackingNumber": "string (optional)",
  "shippingCost": 0.00 (optional),
  "deliveryInstructions": "string (optional)",
  "internalNotes": "string (optional)"
}
```

#### UpdateDeliveryStatusRequest
```json
{
  "status": "InTransit|Delivered|PartiallyDelivered|Cancelled",
  "actualDeliveryTime": "2026-02-16T14:30:00Z (optional, required for Delivered)",
  "receivedByName": "string (optional, required for Delivered)",
  "notes": "string (optional)"
}
```

#### DeliveryNoteResponse
```json
{
  "deliveryNoteId": "DN-2026-000001",
  "orderId": "string",
  "purchaseOrderId": 0,
  "customerId": "guid",
  "customerName": "string",
  "deliveryDate": "2026-02-16T00:00:00Z",
  "actualDeliveryTime": "2026-02-16T14:30:00Z",
  "status": "Delivered",
  "shippingAddressLine1": "string",
  "shippingCity": "string",
  "shippingProvince": "string",
  "deliveryContactName": "string",
  "deliveryContactPhone": "string",
  "carrierName": "string",
  "trackingNumber": "string",
  "shippingCost": 0.00,
  "receivedByName": "string",
  "signedAt": "2026-02-16T14:30:00Z",
  "deliveryInstructions": "string",
  "items": [
    {
      "id": 1,
      "productCode": "string",
      "productName": "string",
      "quantityOrdered": 0.00,
      "quantityManufactured": 0.00,
      "quantityDelivered": 0.00,
      "unitOfMeasure": "string",
      "itemNotes": "string"
    }
  ],
  "createdAt": "2026-02-16T10:00:00Z",
  "createdBy": "string"
}
```

#### DeliveryNoteSummaryDto
```json
{
  "deliveryNoteId": "DN-2026-000001",
  "orderId": "string",
  "customerName": "string",
  "deliveryDate": "2026-02-16T00:00:00Z",
  "status": "Delivered",
  "itemCount": 5,
  "trackingNumber": "string"
}
```

## Integration Points

### Services to Integrate With

#### OrderService
- **Read**: Fetch order details to pre-populate delivery note
- **Write**: Update `ActualDeliveryDate` when delivery status changes to "Delivered"
- **Event**: Consume `OrderCompletedEvent` to auto-create delivery note draft (optional)

#### PurchaseOrderService
- **Read**: Fetch purchase order details for supplier deliveries
- **Write**: Update delivery status on purchase orders

#### CustomerService
- **Read**: Fetch customer information (name, addresses, contacts)

#### PdfService
- **Write**: Request PDF generation for delivery note
- **Event**: Publish `DeliveryNotePdfRequestedEvent`

#### NotificationService
- **Event**: Publish delivery events for customer notifications
  - `DeliveryNoteCreatedEvent` → Send shipment notification
  - `DeliveryStatusChangedEvent` → Send tracking updates
  - `DeliveryCompletedEvent` → Send delivery confirmation

#### UploadService
- **Write**: Upload signature files, photos, packing lists
- **Read**: Retrieve uploaded files

#### IAMService
- **Read**: Verify permissions for delivery note operations
- **Startup**: Register service and permissions on startup

### Events to Publish

#### DeliveryNoteCreatedEvent
Published when a delivery note is created.

```json
{
  "deliveryNoteId": "DN-2026-000001",
  "orderId": "string",
  "purchaseOrderId": 0,
  "customerId": "guid",
  "deliveryDate": "2026-02-16T00:00:00Z",
  "itemCount": 5,
  "createdAt": "2026-02-16T10:00:00Z",
  "createdBy": "string"
}
```

#### DeliveryStatusChangedEvent
Published when delivery status changes.

```json
{
  "deliveryNoteId": "DN-2026-000001",
  "orderId": "string",
  "previousStatus": "InTransit",
  "newStatus": "Delivered",
  "actualDeliveryTime": "2026-02-16T14:30:00Z",
  "receivedByName": "string",
  "changedAt": "2026-02-16T14:30:00Z",
  "changedBy": "string"
}
```

#### DeliveryCompletedEvent
Published when delivery is fully completed.

```json
{
  "deliveryNoteId": "DN-2026-000001",
  "orderId": "string",
  "purchaseOrderId": 0,
  "completedAt": "2026-02-16T14:30:00Z",
  "receivedByName": "string"
}
```

#### DeliveryNotePdfRequestedEvent
Published when PDF generation is requested.

```json
{
  "deliveryNoteId": "DN-2026-000001",
  "requestedBy": "string",
  "requestedAt": "2026-02-16T10:00:00Z"
}
```

## Business Rules

### Validation Rules

1. **Delivery Note Creation**:
   - Must have either `OrderId` or `PurchaseOrderId` (at least one)
   - Must have at least one item
   - All item quantities must be positive
   - `QuantityDelivered` ≤ `QuantityManufactured` for each item
   - `DeliveryDate` cannot be in the past (warning only)

2. **Status Transitions**:
   - Cannot transition from "Delivered" or "Cancelled" to any other status
   - Transition to "Delivered" requires `ActualDeliveryTime` and `ReceivedByName`
   - Transition to "InTransit" should have `CarrierName` and `TrackingNumber` (warning if missing)

3. **Partial Deliveries**:
   - Sum of `QuantityDelivered` across all delivery notes for an order item cannot exceed `QuantityOrdered`
   - System should warn if creating multiple delivery notes for same order

4. **Soft Delete**:
   - Can only delete delivery notes with status "Pending"
   - Cannot delete delivery notes with status "Delivered"
   - Deleted delivery notes are soft-deleted (`IsDeleted = true`)

### Authorization Rules

**Permissions**:
- `Delivery.Read`: View delivery notes
- `Delivery.Create`: Create new delivery notes
- `Delivery.Update`: Update delivery note details (address, contact, carrier, etc.)
- `Delivery.UpdateStatus`: Change delivery status
- `Delivery.Delete`: Soft delete delivery notes
- `Delivery.UpdateFiles`: Upload and manage file attachments
- `Delivery.GeneratePdf`: Request PDF generation

**Data Isolation**:
- Users can only access delivery notes for customers they have permission to view
- Customer-scoped access based on IAMService tenant/customer assignments

## PDF Requirements

### Document Layout

The delivery note PDF must include:

1. **Header**:
   - Bilingual title: "ใบส่งของ / Delivery Note"
   - Delivery note number (e.g., "DN-2026-000001")
   - Delivery date
   - Order reference (if applicable)

2. **Customer Information**:
   - Customer name
   - Shipping address (full address with postal code)
   - Delivery contact name and phone

3. **Logistics Information**:
   - Carrier name
   - Tracking number
   - Estimated/actual delivery time

4. **Items Table**:
   - Columns: Item # | Product (Code + Name) | Ordered Qty | Manufactured Qty | **Delivered Qty** | Unit
   - Highlight delivered quantity as the key information
   - Include item notes if present

5. **Notes Section**:
   - Delivery instructions
   - Any special notes about the shipment

6. **Signature Section**:
   - "ผู้ส่งของ / Delivered by" signature line
   - "ผู้รับของ / Received by" signature line
   - Date/time fields

7. **Footer**:
   - Page numbers
   - "Generated by Maliev Platform" footer

### Font Requirements

- Use **Kanit** font for Thai text
- Use standard sans-serif (Arial, Helvetica) for English text
- Support bilingual content throughout

## Success Criteria

The delivery note system is considered complete when:

1. ✅ Employees can create delivery notes from existing orders
2. ✅ System tracks partial deliveries (multiple delivery notes per order)
3. ✅ Item-level tracking works: ordered qty, manufactured qty, delivered qty
4. ✅ PDF generation produces professional Thai-language documents
5. ✅ Status workflow functions correctly (Pending → InTransit → Delivered)
6. ✅ Integration with OrderService updates `ActualDeliveryDate`
7. ✅ Integration with NotificationService sends customer notifications
8. ✅ Frontend UI allows CRUD operations on delivery notes
9. ✅ File attachments (signatures, photos) can be uploaded and viewed
10. ✅ Search and filter functionality works in frontend
11. ✅ All API endpoints are documented and tested
12. ✅ Authorization and data isolation rules are enforced
13. ✅ Database migrations run successfully
14. ✅ Integration tests pass with Testcontainers

## Out of Scope

The following features are **not** included in this specification:

- Real-time GPS tracking integration with carriers
- Automatic tracking number lookup via carrier APIs
- Barcode/QR code generation for delivery notes
- Mobile app for delivery drivers
- Customer self-service portal for tracking
- Integration with accounting systems for shipping cost allocation
- Multi-currency support for international shipments
- Customs documentation for export shipments
- Returns management (RMA system)
- Integration with warehouse management systems (WMS)
- Advanced analytics and reporting dashboards

These features may be added in future iterations if business requirements evolve.

---

**Document Version**: 1.0
**Last Updated**: 2026-02-16
**Owner**: Maliev Platform Team
**Status**: Approved for Implementation
