# Quickstart: Barcode Scan Tracking

**Branch**: `002-barcode-scan`

## Build & Run

```bash
# From repo root
dotnet build Maliev.DeliveryService.slnx

# Run the API (requires running RabbitMQ + PostgreSQL via Aspire AppHost)
dotnet run --project Maliev.DeliveryService.Api
```

## Run Tests

```bash
# All tests
dotnet test Maliev.DeliveryService.slnx

# Unit tests only
dotnet test Maliev.DeliveryService.Tests --filter "FullyQualifiedName~Unit"
```

## Test the Endpoint Manually

```bash
# Happy path — scan a barcode for a Pending delivery note
curl -X POST "https://localhost:{port}/delivery/v1/delivery-notes/DN-2026-000001/barcode-scan" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{"barcodeValue": "TH123456789XY"}'

# Expected: 200 OK
# {
#   "deliveryNoteId": "DN-2026-000001",
#   "trackingNumber": "TH123456789XY",
#   "carrierName": "Flash Express",
#   "status": "InTransit"
# }

# Already dispatched
# Expected: 409 Conflict with { "error": "This delivery note has already been dispatched." }

# Not found
# Expected: 404 Not Found

# Empty barcode
curl -X POST ... -d '{"barcodeValue": ""}'
# Expected: 400 Bad Request (model validation)
```

## Files Changed / Created

| File | Action |
|------|--------|
| `Maliev.DeliveryService.Api/DTOs/BarcodeScanRequest.cs` | Create |
| `Maliev.DeliveryService.Api/DTOs/BarcodeScanResponse.cs` | Create |
| `Maliev.DeliveryService.Api/Services/IDeliveryNoteService.cs` | Add `ScanBarcodeAsync` |
| `Maliev.DeliveryService.Api/Services/DeliveryNoteService.cs` | Implement `ScanBarcodeAsync` |
| `Maliev.DeliveryService.Api/Controllers/DeliveryNotesController.cs` | Add `ScanBarcode` action |
| `Maliev.DeliveryService.Tests/Unit/Services/DeliveryNoteServiceTests.cs` | Add 4 test cases |
