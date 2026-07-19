# MALIEV DeliveryService — Agent Coding Guide

> **This repo** is `Maliev.DeliveryService` — the central authority for outbound logistics documentation. It manages delivery notes, sequential ID generation (`DN-YYYY-XXXXXX`), partial shipment validation, and digital evidence storage via Google Cloud Storage.

---

## Build, Test & Lint Commands

All commands run from `B:\maliev\Maliev.DeliveryService`.

```powershell
# Build (treats warnings as errors — all must be fixed)
dotnet build Maliev.DeliveryService.slnx

# Run all tests
dotnet test Maliev.DeliveryService.slnx --verbosity normal

# Run a single test method
dotnet test --filter "FullyQualifiedName~DeliveryNotesControllerTests.CreateDeliveryNote_ValidRequest_ReturnsCreated"

# Run all tests in a class
dotnet test --filter "FullyQualifiedName~DeliveryNotesControllerTests"

# Run with code coverage
dotnet test Maliev.DeliveryService.slnx --collect:"XPlat Code Coverage"

# Format check
dotnet format Maliev.DeliveryService.slnx

# EF Core migrations (Infrastructure project only)
dotnet ef migrations add <Name> --project Maliev.DeliveryService.Infrastructure --startup-project Maliev.DeliveryService.Infrastructure
```

---

## Code Style & Conventions

### Workspace Structure

```
Maliev.DeliveryService/
├── Maliev.DeliveryService.Api/               # Controllers, Adapters, Authorization, Extensions
├── Maliev.DeliveryService.Application/        # DTOs, Services, Abstractions
├── Maliev.DeliveryService.Domain/             # Entities, value objects
├── Maliev.DeliveryService.Infrastructure/     # EF Core DbContext, Consumers, HttpClients, Storage, Persistence
├── Maliev.DeliveryService.Tests/              # Unit + Integration tests (xUnit)
│   ├── Integration/                           # Controller + DB integration tests
│   ├── Testing/                               # BaseIntegrationTestFactory, PostgreSqlTestFixture
│   ├── Unit/                                  # Unit tests
│   └── Fakes/                                 # Test fakes
├── Directory.Build.props                      # Central package versioning
└── Maliev.DeliveryService.slnx               # Solution file
```

### C# Naming & Formatting

- **Namespaces**: File-scoped (`namespace Maliev.DeliveryService.Domain.Entities;`)
- **Classes/Methods/Properties**: `PascalCase`
- **Private fields**: `_camelCase` (underscore prefix)
- **Parameters/locals**: `camelCase`
- **Async methods**: Suffix with `Async` (e.g., `CreateAsync`, `UpdateStatusAsync`)
- **Interfaces**: Prefix with `I` (e.g., `IDeliveryNoteService`)
- **Permissions**: GCP-style `{domain}.{plural-resource}.{action}` as `public const string` in a `Permissions` static class
  - Valid: `delivery.deliverynotes.create`, `delivery.deliverynotefiles.read`
  - Invalid: `delivery.deliverynote.create` (singular), `delivery.create` (missing resource)
- **XML docs**: Required on ALL public methods and properties
- **Nullable**: Enabled (`<Nullable>enable</Nullable>`). Use `?` explicitly
- **Imports**: System first, then third-party, then local. Alphabetize within groups. Remove unused `using`
- **Braces**: Allman style (new line) for methods and control structures. Expression-bodied for properties/accessors
- **Indentation**: 4 spaces, LF line endings, UTF-8, trim trailing whitespace

### C# Patterns

- **DI**: Constructor injection with `private readonly` fields
- **Controllers**: `[ApiController]`, `[ApiVersion("1")]`, `[Route("delivery/v{version:apiVersion}/delivery-notes")]`
- **Logging**: `ILogger<T>` with structured placeholders (never interpolate): `_logger.LogInformation("Processing {DeliveryNoteId}", id)`
- **Error handling**: Global exception middleware. Return `ProblemDetails` / `ErrorResponse` DTOs. Never expose stack traces
- **JSON**: camelCase (default ASP.NET Core conventions)
- **Manual mapping**: Static extension methods (`ToDto()`, `ToEntity()`). AutoMapper is banned
- **Validation**: `System.ComponentModel.DataAnnotations` on DTOs. FluentValidation is banned

---

## Banned Libraries (Build Will Fail)

| Banned | Use Instead |
|--------|-------------|
| AutoMapper | Manual mapping extensions |
| FluentValidation | DataAnnotations or manual validation |
| FluentAssertions | Standard xUnit `Assert.*` |
| Swashbuckle/Swagger | Scalar (at `/delivery/scalar`) |
| InMemoryDatabase (EF Core) | Testcontainers with real PostgreSQL |

---

## Testing Rules

- **Framework**: xUnit with standard `Assert` (`Assert.Equal`, `Assert.NotNull`, etc.)
- **Naming**: `MethodName_StateUnderTest_ExpectedBehavior` or `HTTP_METHOD_Path_Scenario_ExpectedStatus`
- **Coverage**: Minimum 80% per service
- **Integration tests**: `BaseIntegrationTestFactory<TProgram, TDbContext>` with Testcontainers (PostgreSQL 18, Redis, RabbitMQ). Never InMemoryDatabase
- **System tests** (Tier 3): `AspireTestFixture` with `[Collection("AspireDomainTests")]` — shared AppHost, never one per class
- **Eventual consistency**: Use `TestHelpers.WaitForAsync`. Never `Task.Delay`
- **MassTransit consumers**: Must have consumer tests using `AddMassTransitTestHarness()`
- **Test parallelization**: Disabled via `[assembly: CollectionBehavior(DisableTestParallelization = true)]`

---

## Mandatory Rules

- **`TreatWarningsAsErrors = true`**: Zero warnings allowed. No suppression
- **`[RequirePermission("delivery.deliverynotes.action")]`**: On all endpoints, not plain `[Authorize]`
- **API versioning**: All routes versioned (`v1/`)
- **Service prefix**: Routes prefixed with `/delivery` (e.g., `/delivery/v1/delivery-notes`)
- **Scalar docs**: Configured at `/delivery/scalar`
- **Secrets**: Never hardcoded. Use GCP Secret Manager or environment variables
- **Async/await**: All the way down. Pass `CancellationToken`
- **EF Core Design package**: Only in Infrastructure project, never in Api
- **PostgreSQL xmin**: Shadow property only — `entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion()`. Never add entity property
- **Temporary files**: Generate in `/temp` folder, clean up afterwards

---

## Security & Authorization Boundaries

- **Cross-boundary DTO rule**: Before changing controllers, service clients, DTOs, events, or BFF payloads, verify request/response DTOs, JSON property names, messaging schemas, and tests that assert the actual wire shape.
- **Endpoint permission rule**: Keep `[RequirePermission]` on every endpoint; do not replace it with plain `[Authorize]`.
- **Delivery note resource scope**: Search and object routes must fail closed. A caller needs unrestricted `delivery.deliverynotes.read` on `delivery-notes/*` or customer-scoped `delivery.customer.read` on `customers/{customerId}` before a delivery note, file list, generated PDF request, update, status transition, evidence upload, or delete can proceed.
- **IAM test coverage**: Unit tests must cover both unrestricted and customer-scoped denied paths when authorization code changes.

---

## Git Rules

- Each `Maliev.*` folder is an independent git repo. `cd` into it before git commands
- **Commit early and often** after every meaningful unit of work. Do not accumulate changes
- **Never use `git checkout` to restore files** — commit first, then `git revert` or `git reset --soft`
- Feature branches merged to `develop` via PR. Do not push without being asked

---

## Service-Specific Details

### Architecture

- **Framework**: ASP.NET Core 10.0 (C# 13)
- **Database**: PostgreSQL 18 with Entity Framework Core 10.x
- **Messaging**: RabbitMQ via MassTransit
- **Storage**: Google Cloud Storage (bucket-based signature and evidence storage)
- **API Documentation**: OpenAPI 3.1 + Scalar UI
- **Observability**: OpenTelemetry (Metrics, Traces, Logging)

### Environment Variables

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DeliveryDbContext` | PostgreSQL connection string |
| `ConnectionStrings__rabbitmq` | RabbitMQ connection string |
| `ConnectionStrings__redis` | Redis connection string |
| `GoogleCloudStorage__BucketName` | GCS bucket for delivery evidence files |
| `GoogleCloudStorage__ProjectId` | GCP project ID |
| `OrderService__BaseUrl` | OrderService HTTP client base URL |

### Permissions (DeliveryPermissions)

| Permission | Endpoint |
|-----------|----------|
| `delivery.deliverynotes.create` | POST `/delivery/v1/delivery-notes` |
| `delivery.deliverynotes.read` | GET `/delivery/v1/delivery-notes`, GET `/delivery/v1/delivery-notes/{id}` |
| `delivery.deliverynotes.update` | PUT `/delivery/v1/delivery-notes/{id}`, PATCH `/delivery/v1/delivery-notes/{id}/status` |
| `delivery.deliverynotes.delete` | DELETE `/delivery/v1/delivery-notes/{id}` |
| `delivery.deliverynotes.generate` | POST `/delivery/v1/delivery-notes/{id}/generate-pdf` |
| `delivery.deliverynotefiles.create` | POST `/delivery/v1/delivery-notes/{id}/files` |
| `delivery.deliverynotefiles.read` | GET `/delivery/v1/delivery-notes/{id}/files` |

### Sequential ID Generation

Delivery note IDs use the format `DN-YYYY-XXXXXX` (e.g., `DN-2026-000001`). Generated by `DeliveryNoteIdGenerator` with concurrency retry logic.

### Status Transitions

```
Pending → InTransit → Delivered
                    → PartiallyDelivered → Delivered
Any (except Delivered/Cancelled) → Cancelled
```

Only `Pending` notes can be soft-deleted. Transitioning to `Delivered` requires `ActualDeliveryTime` and `ReceivedByName`.

### Database Conventions

- **Snake_case naming**: Applied globally via `SnakeCaseNamingHelper.ApplySnakeCaseNaming(modelBuilder)`
- **UTC DateTime**: Global value converter prevents `Npgsql.UnspecifiedKind` exceptions
- **Soft delete**: `IsDeleted` query filter on `DeliveryNote` and `DeliveryNoteFile` entities
- **Concurrency**: Optimistic concurrency via `RowVersion` / `xmin` shadow property

### MassTransit Events

| Event | Direction | Description |
|-------|-----------|-------------|
| `OrderCompletedEvent` | Consumed | Auto-creates draft delivery note from order |
| `DeliveryNoteCreatedEvent` | Published | New delivery note created |
| `DeliveryStatusChangedEvent` | Published | Status transition completed |
| `DeliveryCompletedEvent` | Published | Delivery fully completed |
| `DeliveryNotePdfRequestedEvent` | Published | PDF generation requested |
