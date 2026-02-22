# Feature Specification: Barcode Scan Tracking

**Feature Branch**: `002-barcode-scan`
**Created**: 2026-02-21
**Status**: Draft
**Input**: User description: "Barcode scan tracking for delivery notes — allow employees to scan Flash Express shipping label barcodes to record tracking numbers automatically instead of manual entry"

## Clarifications

### Session 2026-02-21

- Q: If the downstream event publish fails after the database save, should the scan operation succeed or roll back? → A: Best-effort publish — the scan succeeds if the database save succeeds; the message broker handles retry/delivery of the event independently.
- Q: What value is stored as the employee's identity in the UpdatedBy field and status change event? → A: Internal user ID — downstream services resolve the full employee identity from the employee service using this ID.
- Q: Should the barcode value be trimmed of whitespace before being stored, or stored as-is? → A: Trim before storing — the saved tracking number never contains leading or trailing whitespace.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Scan Barcode to Record Tracking Number (Priority: P1)

An employee has packed an order and affixed the Flash Express shipping label to the package. They open the delivery note in the intranet, click "Scan Tracking Barcode", and scan the barcode on the label with a handheld scanner. The tracking number is captured instantly without any typing, the carrier name is recorded as Flash Express, and the delivery note status changes to In Transit.

**Why this priority**: This is the primary business flow — the entire feature exists to eliminate manual tracking number entry and trigger downstream fulfilment processes. Without this working, the feature has no value.

**Independent Test**: Can be fully tested by opening a delivery note in Pending status, submitting a barcode scan, and verifying that the tracking number, carrier, and status are updated correctly — delivering the core value of zero-typing tracking number capture.

**Acceptance Scenarios**:

1. **Given** a delivery note exists in Pending status, **When** an employee submits a valid barcode value, **Then** the delivery note's tracking number is set to the scanned value, carrier is set to Flash Express, status changes to In Transit, and the system records who performed the scan and when.
2. **Given** a delivery note exists in Pending status, **When** an employee submits a valid barcode value, **Then** downstream systems (notification and order management) are informed of the status change so they can send the customer a tracking email and update the order status.
3. **Given** a delivery note exists in Pending status, **When** the scan succeeds, **Then** the system returns a confirmation with the delivery note ID, tracking number, carrier name, and new status.

---

### User Story 2 - Prevent Double-Scanning Already-Dispatched Notes (Priority: P2)

An employee accidentally attempts to scan a barcode for a delivery note that has already been dispatched, delivered, or cancelled. The system must protect against this and return a clear, actionable error so the employee knows no changes were made.

**Why this priority**: Without this guard, a second scan could overwrite the tracking number of a note already out for delivery, causing data integrity issues and confusing customers and the order system.

**Independent Test**: Can be fully tested by submitting a barcode scan against a delivery note already in In Transit, Delivered, Partially Delivered, or Cancelled status and verifying a conflict error is returned.

**Acceptance Scenarios**:

1. **Given** a delivery note is already In Transit, **When** an employee submits a barcode scan, **Then** the system returns a conflict error with the message "This delivery note has already been dispatched." and makes no changes to the record.
2. **Given** a delivery note is Delivered, Partially Delivered, or Cancelled, **When** an employee submits a barcode scan, **Then** the same conflict error is returned and the record is unchanged.

---

### User Story 3 - Reject Invalid Barcode Input (Priority: P3)

An employee submits an empty or excessively long barcode value (for example, the scanner misfires and sends a blank string). The system must reject this before attempting any lookup and return a clear validation error.

**Why this priority**: Prevents garbage data from entering the system and gives the employee immediate feedback to re-scan.

**Independent Test**: Can be fully tested by submitting an empty barcode or a barcode exceeding 100 characters and verifying a validation error is returned without any state change.

**Acceptance Scenarios**:

1. **Given** an employee submits an empty or whitespace-only barcode value, **Then** the system returns a validation error without modifying any records.
2. **Given** an employee submits a barcode value longer than 100 characters, **Then** the system returns a validation error without modifying any records.

---

### User Story 4 - Handle Non-Existent Delivery Note (Priority: P3)

An employee scans a barcode while viewing a delivery note reference that no longer exists or was mistyped. The system returns a clear not-found error.

**Why this priority**: Defensive handling — important for correctness but low risk in practice since employees navigate from existing records.

**Independent Test**: Can be tested by submitting a barcode scan for a delivery note reference that does not exist and verifying a not-found error is returned.

**Acceptance Scenarios**:

1. **Given** the delivery note referenced in the request does not exist, **When** an employee submits a barcode scan, **Then** the system returns a not-found error and makes no changes.

---

### Edge Cases

- What happens when the scanner sends a barcode with leading/trailing whitespace? The system trims the value first. If the result is empty, a validation error is returned. If non-empty, the trimmed value is stored as the tracking number — never the raw padded string.
- What happens if two employees scan the same delivery note simultaneously? The second scan receives a conflict error because the first will have already transitioned the status to In Transit.
- What happens if the downstream event notification fails after the record is saved? The scan is still considered successful — event publishing is best-effort and the message broker handles retry. The delivery note record reflects the correct state regardless of event delivery status.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST expose a barcode scan action for a specific delivery note, accessible only to authenticated employees.
- **FR-002**: The system MUST accept a barcode value submitted by the employee and record it as the tracking number on the delivery note.
- **FR-003**: The system MUST automatically record the carrier name as "Flash Express" on a successful scan — no employee input required for carrier.
- **FR-004**: The system MUST transition the delivery note status to "In Transit" upon a successful barcode scan.
- **FR-005**: The system MUST record the internal user ID of the employee who performed the scan and the exact time of the scan. Downstream services resolve the full employee identity from the employee service using this ID.
- **FR-006**: The system MUST publish a status change event to the message broker on a successful scan (best-effort). The event includes the delivery note reference, order reference, previous status, new status, timestamp, and the internal user ID of who made the change (consumers resolve full identity from the employee service). If the event publish fails, the scan operation is still considered successful — the broker is responsible for retry and delivery.
- **FR-007**: The system MUST trim leading and trailing whitespace from the barcode value before validation and storage. After trimming, if the value is empty or exceeds 100 characters, the system MUST return a validation error without modifying any records.
- **FR-008**: The system MUST reject barcode scan requests for delivery notes already in In Transit, Delivered, Partially Delivered, or Cancelled status, returning a conflict error with the message "This delivery note has already been dispatched."
- **FR-009**: The system MUST return a not-found error when the referenced delivery note does not exist.
- **FR-010**: Upon success, the system MUST return the delivery note reference, tracking number, carrier name, and current status to the caller.

### Key Entities

- **Delivery Note**: Represents a shipment record. Contains a tracking number (the scanned barcode value), carrier name, status (transitions from Pending to In Transit on scan), and audit fields recording the internal user ID of who last updated it and when.
- **Barcode Scan Request**: The input from the employee's scanner — a single barcode value string, maximum 100 characters, must not be empty.
- **Status Change Event**: A notification sent to downstream systems recording what changed (previous and new status), when, who made the change, and the associated order reference.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Employees can record a tracking number via barcode scan in under 10 seconds, eliminating the manual typing step and reducing transcription errors to zero.
- **SC-002**: 100% of successful barcode scans correctly associate the tracking number with the delivery note and transition its status to In Transit.
- **SC-003**: 0% of already-dispatched, partially delivered, delivered, or cancelled delivery notes can have their tracking number overwritten via a barcode scan — all such attempts are blocked with a clear error.
- **SC-004**: Downstream systems (customer notification, order management) receive a status change event for every successful scan, enabling timely customer communications without manual intervention.
- **SC-005**: Invalid inputs (empty, too long) are rejected immediately with clear feedback, enabling employees to re-scan without confusion.

## Assumptions

- Only Flash Express barcodes are scanned via this endpoint. The carrier name "Flash Express" is hardcoded. If other carriers are added in future, this assumption must be revisited.
- The barcode value encoded on the Flash Express label is exactly the tracking number — no decoding or transformation is required.
- The 100-character maximum for barcode values comfortably covers all Flash Express tracking number formats currently in use.
- Employees access this feature via Maliev.Intranet and are already authenticated. The endpoint enforces the same authorisation as existing delivery note endpoints.
- Downstream consumers (notification service, order service) are deployed and consuming status change events. Verification of their readiness is a deployment pre-condition, not part of this feature.

## Out of Scope

- Supporting barcode scans for carriers other than Flash Express.
- Implementing or modifying downstream consumers of the status change event (notification service, order service).
- Manual correction of a tracking number after it has been scanned (requires a separate feature).
- Barcode format validation beyond length and non-empty checks — the system accepts any non-empty string up to 100 characters.
