# Feature Specification: Delivery Note System

**Feature Branch**: `001-delivery-note-system`
**Created**: 2026-02-16
**Status**: Draft
**Input**: User description: "Delivery Note System for managing shipments from factory to customers with formal Thai business documentation"

## Clarifications

### Session 2026-02-16

- Q: How are delivery notes secured and who can access them? → A: Customer-scoped with role-based permissions - Users can only access delivery notes for their assigned customers, with different permission levels (read, create, update, delete)
- Q: How should system handle failures when integrating with OrderService, NotificationService, or PdfService? → A: Graceful degradation with retry - Core operations succeed even if non-critical services fail; automatic retry for failed integrations; user notified of partial failures
- Q: What monitoring/alerting is needed for delivery tracking operations? → A: Structured logging, metrics, and distributed tracing - Full observability with log aggregation, real-time metrics/alerts, and trace correlation across services
- Q: How long should delivery notes be retained? Is there an archival strategy? → A: 7 years with archival - Keep 2 years in active storage, archive 3-7 year old records to cheaper storage, delete after 7 years
- Q: How will API changes be versioned to support existing clients? → A: URL path versioning - Version in path (e.g., /v1/delivery-notes, /v2/delivery-notes) with support for multiple concurrent versions

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create and Ship Delivery Note (Priority: P1)

Warehouse staff creates a delivery note when goods are ready to ship to customers, documenting what is being delivered, to whom, and tracking shipment details.

**Why this priority**: Core functionality that enables the business to formally document shipments and comply with Thai business practices. Without this, no delivery tracking is possible.

**Independent Test**: Can be fully tested by creating a delivery note from an existing order, assigning quantities to ship, adding carrier information, and verifying the delivery note is created with unique ID and "Pending" status.

**Acceptance Scenarios**:

1. **Given** an order exists with manufactured items ready to ship, **When** warehouse staff creates a delivery note specifying quantities and carrier details, **Then** a unique delivery note ID (DN-YYYY-XXXXXX) is generated and the delivery note is saved with "Pending" status
2. **Given** a delivery note is being created, **When** staff enters a delivery quantity greater than manufactured quantity for any item, **Then** the system prevents creation and displays a validation error
3. **Given** a delivery note is created, **When** the shipment leaves the factory, **Then** staff can update status to "InTransit" with carrier tracking information
4. **Given** a delivery note is in "InTransit" status, **When** customer receives goods, **Then** staff can update status to "Delivered" recording recipient name and delivery timestamp

---

### User Story 2 - Handle Partial Deliveries (Priority: P1)

Orders that cannot be fulfilled in a single shipment require multiple delivery notes, with the system tracking cumulative delivered quantities to ensure nothing is over-delivered.

**Why this priority**: Critical for manufacturing scenarios where production batches may not align with order quantities. Prevents order fulfillment errors and customer disputes.

**Independent Test**: Can be fully tested by creating two delivery notes for the same order with partial quantities, verifying cumulative tracking prevents over-delivery, and confirming order completion when all quantities delivered.

**Acceptance Scenarios**:

1. **Given** an order for 100 units with only 60 units manufactured, **When** warehouse creates first delivery note for 60 units, **Then** order shows "PartiallyDelivered" status with 40 units remaining
2. **Given** an order with previous partial delivery of 60 units, **When** warehouse creates second delivery note attempting to deliver 50 units (exceeding remaining 40), **Then** system prevents creation and shows validation error
3. **Given** an order with multiple partial deliveries totaling ordered quantity, **When** final delivery note is marked "Delivered", **Then** order status updates to "Delivered" and all delivery notes are linked to the order

---

### User Story 3 - Generate Formal Delivery Documentation (Priority: P1)

Customers and logistics providers need formal bilingual (Thai/English) delivery notes as official shipping documents required for Thai business operations.

**Why this priority**: Legal/compliance requirement for Thai business operations. Without this, deliveries lack formal documentation required by customers and regulatory bodies.

**Independent Test**: Can be fully tested by generating a PDF for a delivery note and verifying it contains all required sections (bilingual header, customer info, items table, signature lines) in proper Thai business document format.

**Acceptance Scenarios**:

1. **Given** a delivery note exists with complete information, **When** user requests PDF generation, **Then** system produces Thai-language PDF with delivery note number, customer details, items table, and signature sections
2. **Given** a generated PDF, **When** user views the document, **Then** all Thai text renders correctly using Kanit font and bilingual headers appear as "ใบส่งของ / Delivery Note"
3. **Given** a PDF generation request, **When** system completes generation, **Then** PDF URL is returned and document is accessible for download, printing, or email

---

### User Story 4 - Track Delivery History (Priority: P2)

Customer service and management need to search and view historical delivery information to answer customer inquiries, reconcile shipments, and audit delivery performance.

**Why this priority**: Important for customer support and business operations, but system can function without search initially. Enables better customer service and operational insights.

**Independent Test**: Can be fully tested by creating multiple delivery notes with different orders, customers, and dates, then searching by various criteria (order ID, customer, date range, status) and verifying correct results returned.

**Acceptance Scenarios**:

1. **Given** multiple delivery notes exist in the system, **When** user searches by order ID, **Then** all delivery notes for that order are displayed with summary information
2. **Given** delivery notes spanning multiple months, **When** user filters by date range, **Then** only delivery notes within specified dates are returned
3. **Given** a delivery note in search results, **When** user views details, **Then** complete information including items, status history, and attached files are displayed

---

### User Story 5 - Attach Delivery Evidence (Priority: P2)

Warehouse and logistics staff attach photos, signatures, and supporting documents to delivery notes as proof of delivery and for dispute resolution.

**Why this priority**: Enhances delivery accountability and provides evidence for disputes, but core delivery tracking works without attachments. Valuable for reducing customer disputes.

**Independent Test**: Can be fully tested by uploading various file types (signatures, photos, packing lists) to a delivery note and verifying files are stored, retrievable, and displayed with correct metadata.

**Acceptance Scenarios**:

1. **Given** a delivery note exists, **When** staff uploads a signature image file, **Then** file is stored with delivery note and marked as "Signature" type
2. **Given** a delivery note with attached files, **When** user views the delivery note, **Then** all attachments are listed with file names, types, and upload timestamps
3. **Given** a delivery confirmation photo, **When** staff uploads during status update to "Delivered", **Then** photo is attached to delivery note as proof of delivery

---

### User Story 6 - Notify Customers of Shipments (Priority: P3)

Customers automatically receive notifications when their orders ship, including tracking information and estimated delivery times.

**Why this priority**: Improves customer experience but delivery operations can function without automated notifications. Can be implemented after core tracking is working.

**Independent Test**: Can be fully tested by creating and updating delivery note status, then verifying notification events are published and customers receive appropriate messages at each status change.

**Acceptance Scenarios**:

1. **Given** a delivery note is created, **When** status changes to "InTransit", **Then** customer receives shipment notification with tracking number and carrier information
2. **Given** a delivery in transit, **When** status changes to "Delivered", **Then** customer receives delivery confirmation with actual delivery time and recipient name
3. **Given** delivery events are published, **When** notification service receives events, **Then** notifications contain all required information (delivery note ID, order reference, customer details)

---

### Edge Cases

- What happens when a delivery note is created but order is cancelled before shipping? (System should allow marking delivery note as "Cancelled")
- How does system handle delivery note creation when no manufactured quantities are recorded? (System should warn but allow creation with manual quantity entry)
- What happens when carrier tracking number is invalid or changes? (System should allow updating tracking information on existing delivery notes)
- How does system handle delivery notes when customer address changes after creation? (System stores address snapshot at creation time to preserve historical record)
- What happens when partial delivery is attempted but remaining items are never produced? (System should allow marking order as "PartiallyDelivered - Complete" with notes explaining shortfall)
- How does system handle signature file upload failures during delivery confirmation? (Signature is optional; system should allow delivery confirmation without signature if upload fails)
- What happens when multiple delivery notes for same order are updated simultaneously? (System should use optimistic concurrency control to prevent conflicting updates)
- How does system handle PDF generation service being unavailable? (Delivery note operations succeed; PDF generation queued for automatic retry; user notified of pending PDF)
- What happens when NotificationService fails during delivery status update? (Status update succeeds; notification queued for retry; delivery note shows notification status as "pending")
- How does system handle OrderService being unavailable during delivery completion? (Delivery note status updates successfully; order status update queued for retry with eventual consistency)
- What happens when user tries to access a delivery note during archival migration? (System gracefully handles concurrent access during archival; user may experience slightly slower response but operation succeeds)
- How does system handle archival process failures? (Archival operations are idempotent and automatically retried; delivery notes remain accessible in active storage until archival confirmed successful)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST generate unique delivery note identifiers in format "DN-YYYY-XXXXXX" where YYYY is year and XXXXXX is sequential number
- **FR-002**: System MUST allow creating delivery notes linked to existing orders with item quantities to be delivered
- **FR-003**: System MUST validate that delivery quantities do not exceed manufactured quantities for each item
- **FR-004**: System MUST track cumulative delivered quantities across multiple delivery notes for same order to prevent over-delivery
- **FR-005**: System MUST support delivery note status workflow: Pending → InTransit → Delivered/PartiallyDelivered, with Cancelled as terminal state
- **FR-006**: System MUST require recipient name and actual delivery timestamp when marking delivery as "Delivered"
- **FR-007**: System MUST store shipping address snapshot at delivery note creation time for historical audit trail
- **FR-008**: System MUST allow attaching multiple files (signatures, photos, documents) to delivery notes with categorization by file type
- **FR-009**: System MUST generate bilingual Thai/English PDF documents containing delivery note details, items table, and signature sections
- **FR-010**: System MUST provide search and filtering of delivery notes by order ID, customer, date range, status, and tracking number
- **FR-011**: System MUST record complete audit trail for delivery notes including creation, status changes, and updates with timestamps and user information
- **FR-012**: System MUST support soft deletion of delivery notes (only "Pending" status can be deleted)
- **FR-013**: System MUST allow updating delivery details (carrier, tracking number, contact information, delivery instructions) after creation
- **FR-014**: System MUST publish events when delivery notes are created, status changes, or deliveries complete to integrate with other services
- **FR-015**: System MUST store item-level details including ordered quantity, manufactured quantity, and delivered quantity for reconciliation
- **FR-016**: System MUST prevent status transitions from terminal states ("Delivered", "Cancelled") to any other status
- **FR-017**: System MUST support optional purchase order references for supplier deliveries in addition to customer orders
- **FR-018**: System MUST validate status transitions allow only valid state changes (e.g., cannot skip from "Pending" directly to "Delivered")
- **FR-019**: System MUST enforce customer-scoped access control ensuring users can only access delivery notes for customers they are assigned to
- **FR-020**: System MUST implement role-based permissions (Delivery.Read, Delivery.Create, Delivery.Update, Delivery.UpdateStatus, Delivery.Delete, Delivery.UpdateFiles, Delivery.GeneratePdf) controlling what actions users can perform
- **FR-021**: System MUST authenticate and authorize all delivery note operations through integration with IAMService
- **FR-022**: System MUST allow core delivery note operations (create, update status, view) to succeed even when non-critical integrations (PDF generation, notifications) fail
- **FR-023**: System MUST automatically retry failed integration operations (order updates, event publishing, PDF generation) with exponential backoff
- **FR-024**: System MUST notify users of partial failures when delivery note operations succeed but dependent service integrations fail (e.g., "Delivery note created but customer notification pending")
- **FR-025**: System MUST maintain delivery note data integrity through database transactions ensuring no data loss even during service failures
- **FR-026**: System MUST emit structured logs for all delivery note operations including operation type, user, timestamp, delivery note ID, and outcome
- **FR-027**: System MUST expose metrics for monitoring including request counts, error rates, operation latencies, and active delivery note counts by status
- **FR-028**: System MUST implement distributed tracing across service boundaries to correlate operations spanning DeliveryService, OrderService, PdfService, and NotificationService
- **FR-029**: System MUST provide health check endpoints indicating service availability and dependency health status
- **FR-030**: System MUST retain delivery notes for 7 years from creation date to comply with business and regulatory requirements
- **FR-031**: System MUST automatically archive delivery notes older than 2 years from active storage to archival storage while maintaining accessibility
- **FR-032**: System MUST allow retrieval of archived delivery notes with acceptable performance degradation (up to 5 seconds retrieval time vs 2 seconds for active records)
- **FR-033**: System MUST automatically delete delivery notes and all associated files after 7 year retention period with audit logging of deletion events
- **FR-034**: System MUST implement URL path-based API versioning (e.g., /delivery/v1/, /delivery/v2/) allowing multiple API versions to coexist
- **FR-035**: System MUST support at least 2 concurrent API versions during transition periods to enable gradual client migration
- **FR-036**: System MUST document API version deprecation timeline providing minimum 6 months notice before removing deprecated versions

### Key Entities

- **Delivery Note**: Primary document representing a shipment of goods from factory to customer, containing delivery metadata (ID, dates, status, carrier info), shipping address snapshot, contact information, and references to order/purchase order
- **Delivery Note Item**: Individual line items within delivery note, tracking product details and three critical quantities: ordered (what customer requested), manufactured (what was actually produced), delivered (what is in this specific shipment)
- **Delivery Note File**: Attached files providing evidence and supporting documentation for deliveries (signatures, photos, packing lists, invoices), categorized by file type
- **Address**: Shipping destination information captured at delivery note creation time, including company name, contact details, full address fields (line 1, line 2, city, province, postal code, country, phone, email)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Warehouse staff can create a delivery note from an order in under 3 minutes including entering all required shipping information
- **SC-002**: System accurately tracks cumulative delivered quantities across partial deliveries with zero over-delivery incidents
- **SC-003**: PDF generation completes in under 5 seconds and produces properly formatted Thai business documents with correct font rendering
- **SC-004**: 95% of delivery status updates complete within 2 seconds including validation and event publishing
- **SC-005**: Search and filter operations return results in under 1 second for datasets containing up to 10,000 delivery notes
- **SC-006**: Customers receive shipment notifications within 5 minutes of delivery status changes
- **SC-007**: System maintains complete audit trail with 100% of delivery note changes recorded with timestamp and user attribution
- **SC-008**: File attachment uploads complete in under 10 seconds for files up to 5MB in size
- **SC-009**: Zero data loss incidents during delivery note creation or status updates through use of database transactions
- **SC-010**: Active delivery notes (less than 2 years old) can be retrieved and viewed within 2 seconds; archived delivery notes (2-7 years old) within 5 seconds
- **SC-011**: System enforces all validation rules (quantity limits, status transitions, required fields) with 100% accuracy preventing invalid data entry
- **SC-012**: Integration events are published successfully with 99.9% reliability to notify dependent services of delivery changes
- **SC-013**: All delivery note operations emit structured logs and metrics enabling 95% of operational issues to be diagnosed within 5 minutes using observability tools
- **SC-014**: Distributed traces successfully correlate operations across all integrated services with 100% trace completeness for delivery note workflows
