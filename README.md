# Maliev.DeliveryService

A high-performance microservice for managing delivery notes, partial shipments, and delivery evidence documentation. Built with **.NET 10**, **Entity Framework Core 10**, and **MassTransit**.

## 🚀 Features

*   **Delivery Note Management**: Create, update, and track delivery notes.
*   **Sequential ID Generation**: Format `DN-YYYY-XXXXXX` with concurrency handling.
*   **Partial Deliveries**: Validation to ensure delivery quantities do not exceed order quantities.
*   **Event-Driven Architecture**: Consumes `OrderCompletedEvent` to auto-create drafts.
*   **PDF Generation**: Triggers Thai/English PDF generation via event bus.
*   **Evidence Handling**: Upload signatures and photos to Google Cloud Storage.

## 🛠 Tech Stack

*   **.NET 10** (C# 13)
*   **PostgreSQL 18** (Data persistence)
*   **Redis** (Distributed caching)
*   **RabbitMQ** (Message bus via MassTransit)
*   **Google Cloud Storage** (File storage)
*   **OpenAPI / Scalar** (API Documentation)
*   **Testcontainers** (Integration testing)

## 📦 Project Structure

*   `Maliev.DeliveryService.Api`: The Web API entry point.
*   `Maliev.DeliveryService.Data`: EF Core DbContext, Entities, and Migrations.
*   `Maliev.DeliveryService.Tests`: Unit and Integration tests.

## 🔧 Setup & Running

### Prerequisites

*   .NET 10 SDK
*   Docker Desktop (for databases and message broker)

### Running Locally

1.  **Start dependencies**: Ensure Docker is running.
2.  **Run the application**:
    ```bash
    dotnet run --project Maliev.DeliveryService.Api/Maliev.DeliveryService.Api.csproj
    ```
    The app will attempt to connect to infrastructure. For full orchestration, run via the Aspire AppHost (if available in the parent solution).

### Configuration

Copy `appsettings.json` to `appsettings.Development.json` and configure:

*   `ConnectionStrings:DeliveryDb` (PostgreSQL)
*   `Redis:ConnectionString`
*   `RabbitMQ:Host`, `Username`, `Password`
*   `GoogleCloud:BucketName` (Optional for local dev)

## 🧪 Testing

The solution includes a comprehensive test suite covering unit logic, consumers, and end-to-end integration flows.

### Running Unit Tests
Fast, in-memory tests for business logic and consumers.
```bash
dotnet test Maliev.DeliveryService.Tests --filter Category!=Integration
```

### Running Integration Tests
Uses **Testcontainers** to spin up real PostgreSQL instances. Requires Docker.
```bash
dotnet test Maliev.DeliveryService.Tests --filter Category=Integration
```

### Running Consumer Tests
Verifies MassTransit consumers using the in-memory test harness.
```bash
dotnet test Maliev.DeliveryService.Tests --filter "FullyQualifiedName~Consumers"
```

## 📚 API Documentation

When running locally in Development mode, API documentation is available at:

*   **Scalar UI**: `https://localhost:5001/scalar/v1`
*   **OpenAPI JSON**: `https://localhost:5001/openapi/v1.json`

## 🔒 Authentication

The API uses Bearer Token authentication. Ensure your requests include a valid JWT with `sub` (User ID) and `customer_id` claims.
