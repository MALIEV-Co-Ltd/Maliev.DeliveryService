# Maliev Delivery Service

[![Build Status](https://img.shields.io/badge/Build-Passing-success)](https://github.com/MALIEV-Co-Ltd/Maliev.DeliveryService)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Database](https://img.shields.io/badge/Database-PostgreSQL%2018-blue)](https://www.postgresql.org/)

High-performance microservice for managing delivery notes, partial shipments, and delivery evidence documentation for the Maliev manufacturing ecosystem.

**Role in MALIEV Architecture**: The central authority for outbound logistics documentation. It manages the lifecycle of Delivery Notes (DN), handles sequential ID generation, and integrates with Google Cloud Storage for digital evidence (signatures/photos), ensuring a seamless transition from manufacturing completion to customer delivery.

---

## 🏗️ Architecture & Tech Stack

- **Framework**: ASP.NET Core 10.0 (C# 13)
- **Database**: PostgreSQL 18 with Entity Framework Core 10.x
- **Messaging**: RabbitMQ via MassTransit
- **Storage**: Google Cloud Storage (Bucket-based signature and evidence storage)
- **API Documentation**: OpenAPI 3.1 + Scalar UI
- **Observability**: OpenTelemetry (Metrics, Traces, Logging)

---

## ⚖️ Constitution Rules

This service strictly adheres to the platform development mandates:

### Banned Libraries
To maintain high performance and low complexity, the following are **NOT** used:
- ❌ **AutoMapper**: Explicit manual mapping only.
- ❌ **FluentValidation**: Standard Data Annotations (`[Required]`, `[Range]`) only.
- ❌ **FluentAssertions**: Standard xUnit `Assert` methods only.
- ❌ **In-memory Test DB**: All integration tests use **Testcontainers** with real PostgreSQL 18.

### Mandatory Practices
- ✅ **TreatWarningsAsErrors**: Enabled in all `.csproj` files.
- ✅ **XML Documentation**: Required on all public methods and properties.
- ✅ **No Secrets in Code**: All sensitive configuration injected via environment variables.
- ✅ **No Test Config in Program.cs**: Test configuration in test fixtures only.
- ✅ **IAM Integration**: Self-registers permissions with the IAM Service using GCP-style naming: `{service}.{resource}.{action}`.

---

## ✨ Key Features

- **Delivery Note Management**: Full lifecycle management of delivery documentation from draft to completed.
- **Sequential ID Generation**: Robust format `DN-YYYY-XXXXXX` with high-concurrency handling.
- **Partial Delivery Validation**: Logic to prevent shipment quantities from exceeding order quantities across multiple notes.
- **Event-Driven Workflows**: Consumes `OrderCompletedEvent` to automate draft creation and publishes `DeliveryNoteCreatedEvent` for PDF generation.
- **Evidence Documentation**: Digital signature and photo evidence handling with secure storage.

---

## 🚀 Quick Start

### Prerequisites
- .NET 10.0 SDK
- Docker Desktop (for infrastructure)
- PostgreSQL 18 (Alpine)

### Local Development Setup

1. **Clone the repository**
```bash
git clone https://github.com/MALIEV-Co-Ltd/Maliev.DeliveryService.git
cd Maliev.DeliveryService
```

2. **Spin up Infrastructure**
```bash
docker run --name delivery-db -e POSTGRES_PASSWORD=YOUR_PASSWORD -p 5432:5432 -d postgres:18-alpine
docker run --name delivery-rabbitmq -p 5672:5672 -p 15672:15672 -d rabbitmq:3-management-alpine
```

3. **Configure Environment**
```powershell
# Windows PowerShell
$env:ConnectionStrings__DeliveryDbContext="YOUR_POSTGRES_CONNECTION_STRING"
$env:ConnectionStrings__rabbitmq="YOUR_RABBITMQ_CONNECTION_STRING"
$env:ConnectionStrings__redis="YOUR_REDIS_CONNECTION_STRING"
$env:GoogleCloudStorage__BucketName="maliev-delivery-evidence-dev"
$env:GoogleCloudStorage__ProjectId="maliev-platform"
```

4. **Apply Migrations & Run**
```bash
dotnet ef database update --project Maliev.DeliveryService.Data
dotnet run --project Maliev.DeliveryService.Api
```

The service will be available at `http://localhost:5000/delivery`. Access the interactive documentation at `http://localhost:5000/delivery/scalar`.

---

## 📡 API Endpoints

All endpoints are prefixed with `/delivery/v1/`.

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/notes` | Create a new delivery note |
| GET | `/notes/{id}` | Get delivery note details |
| PUT | `/notes/{id}/complete` | Finalize a delivery note and trigger events |
| POST | `/notes/{id}/evidence` | Upload signature or photo evidence |

---

## 🏥 Health & Monitoring

Standardized health probes for Kubernetes orchestration:
- **Liveness**: `GET /delivery/liveness`
- **Readiness**: `GET /delivery/readiness` (Checks DB, RabbitMQ, and Storage connectivity)
- **Metrics**: `GET /delivery/metrics` (Prometheus format)

---

## 🧪 Testing

We prioritize reliable tests over mock-heavy unit tests.

```bash
# Run all tests using Testcontainers
dotnet test --verbosity normal
```

- **Integration Tests**: Use real PostgreSQL 18 containers.
- **Consumer Tests**: Verify MassTransit logic using in-memory harness.

---

## 📦 Deployment

Infrastructure management is handled via GitOps patterns.

- **Docker Image**: `REGION-docker.pkg.dev/PROJECT_ID/REPOSITORY/maliev-delivery-service:{sha}`
- **Environments**: Development, Staging, Production

---

## 📄 License

Proprietary - © 2026 MALIEV Co., Ltd. All rights reserved.
