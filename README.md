# {API_NAME}

> Robust .NET background jobs template at IATec with Hangfire Pro, Redis, and MassTransit/SQS, promoting standard practices for reliable async job processing.

**Current Version:** `2.1.0`

---

## 📋 Table of Contents

- [About the Project](#about-the-project)
- [Technologies and Stack](#technologies-and-stack)
- [Project Structure](#project-structure)
- [Architecture Layers](#architecture-layers)
- [Prerequisites](#prerequisites)
- [How to Run](#how-to-run)
- [Configuration](#configuration)
- [API Documentation (Scalar)](#api-documentation-scalar)
- [Health Checks](#health-checks)
- [API Versioning](#api-versioning)
- [CORS](#cors)
- [Authentication](#authentication)
- [Jobs Layer (Hangfire)](#jobs-layer-hangfire)
- [Feature: Assets](#feature-assets)
- [Logging Dispatcher](#logging-dispatcher)
- [External Services (AntiCorruption)](#external-services-anticorruption)
- [Persistence Layer (Redis)](#persistence-layer-redis)
- [Message Queue Layer (MassTransit/SQS)](#message-queue-layer-masstransitsqs)
- [Domain Layer](#domain-layer)
- [CrossCutting Layer](#crosscutting-layer)
- [Test Projects](#test-projects)
- [Docker](#docker)
- [CI/CD](#cicd)
- [Renaming the API](#renaming-the-api)
- [Template Extension Points](#template-extension-points)
- [Contributing](#contributing)
- [Changelog](#changelog)

---

## About the Project

This repository is a **base template / scaffolding project** for creating new .NET background job services following IATec standards. It is built on top of `IATec.Standard.Net.Api` with a fully implemented Hangfire Pro job layer, Redis persistence, MassTransit/SQS message queue, and real integration tests using Testcontainers.

### What comes pre-configured

- Clean Architecture / Vertical Slices with **MediatR CQRS**.
- **Hangfire Pro** — job processing with Redis storage, Pro Batches, Throttling, and Console log.
- **Pattern 2 (Command = Job)** — commands are MediatR requests enqueued directly via `IBackgroundJobClient.EnqueueCommand(command)`, dispatched to handlers via `ISender.Send()` inside the worker.
- **Batch monitoring** — `ProcessAssetBatchCommand` creates a Hangfire Pro batch; `MonitorBatchCommand` polls progress and updates the console progress bar.
- **Redis** persistence via `StackExchange.Redis` — used for job storage (Hangfire Pro.Redis) and batch progress tracking.
- **Hangfire Dashboard** with Basic Authentication (`Dashboard` section in `appsettings.json`).
- **API Versioning** via URL segment (`v{version:apiVersion}`) — `v1` deprecated, `v2` current.
- **Scalar** interactive API documentation.
- **Health Checks** — version endpoint + Redis + Hangfire.
- **CORS** policy (`AllowAnyOrigin`, `AllowAnyMethod`, `AllowAnyHeader`).
- **FluentValidation** pipeline behavior (validators run before handlers inside the Hangfire worker).
- **Exception pipeline behavior** via `IATec.Shared.Behaviors`.
- **Logging dispatcher** sending structured logs to IATec Log Service.
- **MassTransit** with `MassTransit.AmazonSQS` — consumer for `ProcessAssetEvent` registered in `MessageQueue` layer.
- **Integration tests** with real Testcontainers (Redis + LocalStack SQS) — no mocks.
- **Serilog** for structured logging.
- Integration with internal `IATec.Shared.*` packages.

### What is NOT implemented (placeholder scaffolding)

- `ProcessAssetCommandHandler` throws `NotImplementedException` — replace with actual business logic.
- `docker/Dockerfile` is **empty (0 bytes)**.
- No CI/CD pipelines (no `.github/workflows`).

> **Note:** Whenever creating a new job service from this template, read the [Renaming the API](#renaming-the-api) section to adjust names and references.

---

## Technologies and Stack

| Technology | Version |
|------------|---------|
| .NET | 10.0 |
| ASP.NET Core | 10.0.8 |
| Scalar.AspNetCore | 2.14.14 |
| Microsoft.AspNetCore.OpenApi | 10.0.8 |
| API Versioning (Asp.Versioning.Mvc) | 10.0.0 |
| Asp.Versioning.Mvc.ApiExplorer | 10.0.0 |
| AspNetCore.HealthChecks.UI.Client | 9.0.0 |
| AspNetCore.HealthChecks.Redis | 9.0.0 |
| AspNetCore.HealthChecks.Hangfire | 9.0.0 |
| Hangfire.AspNetCore | 1.8.23 |
| Hangfire.Pro | 3.0.5 |
| Hangfire.Pro.Redis | 3.3.1 |
| Hangfire.Console | 1.4.3 |
| Hangfire.Throttling | 1.4.3 |
| MassTransit | 8.5.9 |
| MassTransit.AmazonSQS | 8.5.9 |
| StackExchange.Redis | 2.13.17 |
| Serilog.AspNetCore | 10.0.0 |
| Serilog | 4.3.1 |
| MediatR | 14.1.0 |
| FluentValidation | 12.1.1 |
| FluentResults | 4.0.0 |
| Newtonsoft.Json | 13.0.4 |
| Microsoft.Extensions.Configuration | 10.0.8 |
| Microsoft.Extensions.Configuration.Binder | 10.0.8 |
| Microsoft.Extensions.Http | 10.0.8 |
| Microsoft.Extensions.Options | 10.0.8 |
| IATec.Shared.Api | 1.2.0 |
| IATec.Shared.Application | 2.0.0 |
| IATec.Shared.Domain | 2.0.1 |
| IATec.Shared.HttpClient | 3.0.0 |
| IATec.Shared.Behaviors | 1.3.0 |

> **CSPROJ Settings:** `Nullable=enable`, `ImplicitUsings=enable`, `InvariantGlobalization=false`, `GenerateDocumentationFile=True`.

---

## Project Structure

```
IATec.Standard.Net.Jobs/
├── src/
│   ├── App/
│   │   ├── Configurations/
│   │   │   ├── AppDependencyInjectionConfig.cs
│   │   │   ├── Filters/
│   │   │   │   └── HangfireDashboardAuthFilter.cs
│   │   │   ├── Options/
│   │   │   │   └── DashboardOption.cs
│   │   │   └── Extensions/
│   │   │       ├── CorsPolicyExtension.cs
│   │   │       ├── HangfireExtension.cs
│   │   │       ├── HangfireQueuesExtension.cs
│   │   │       ├── HealthCheckExtension.cs
│   │   │       ├── OptionsExtension.cs
│   │   │       ├── ScalarExtension.cs
│   │   │       └── VersioningExtension.cs
│   │   ├── Controllers/
│   │   │   └── JobsController.cs
│   │   ├── Properties/
│   │   │   └── launchSettings.json
│   │   ├── appsettings.json
│   │   └── Program.cs
│   │
│   ├── Application/
│   │   ├── Configurations/
│   │   │   ├── ApplicationDependencyInjectionConfig.cs
│   │   │   ├── Extensions/
│   │   │   │   ├── MediatorConfig.cs
│   │   │   │   └── ValidatorConfig.cs
│   │   │   └── Factories/
│   │   │       └── ValidatorFactory.cs
│   │   ├── Dispatchers/
│   │   │   └── Logging/
│   │   │       └── LogDispatcher.cs
│   │   └── Features/
│   │       └── Assets/
│   │           ├── Commands/
│   │           │   ├── ProcessAssetCommand.cs         ← single job command
│   │           │   ├── ProcessAssetCommandHandler.cs  ← throws NotImplementedException
│   │           │   ├── ProcessAssetBatchCommand.cs    ← batch job command
│   │           │   └── ProcessAssetBatchCommandHandler.cs
│   │           ├── Events/
│   │           │   └── ProcessAssetEvent.cs           ← MassTransit event
│   │           ├── Rules/
│   │           │   └── AssetValidationRules.cs
│   │           └── ProcessAssetCommandValidator.cs
│   │
│   ├── CrossCutting/
│   │   └── (empty — no .cs files)
│   │
│   ├── Domain/
│   │   ├── Contracts/
│   │   │   ├── Bus/
│   │   │   │   └── IIntegrationEvent.cs
│   │   │   ├── Helpers/
│   │   │   │   └── IJobHelper.cs
│   │   │   └── Services/
│   │   │       └── IBatchJobService.cs
│   │   └── Helpers/
│   │       ├── BatchInfo.cs
│   │       ├── BatchKey.cs
│   │       ├── BatchMonitorResult.cs
│   │       └── BatchProgressInfo.cs
│   │
│   ├── HangFire.Jobs/
│   │   ├── Base/
│   │   │   └── BaseCommand.cs
│   │   ├── Commands/
│   │   │   ├── MonitorBatchCommand.cs
│   │   │   └── MonitorBatchCommandHandler.cs
│   │   ├── Configurations/
│   │   │   └── HangFireJobsDependencyInjectionConfig.cs
│   │   ├── Constants/
│   │   │   └── JobRetryPolicyConstant.cs
│   │   ├── Contracts/
│   │   │   └── IPerformContextAccessor.cs
│   │   ├── Extensions/
│   │   │   ├── BackgroundJobClientExtensions.cs
│   │   │   └── BatchJobServiceExtensions.cs
│   │   ├── Filters/
│   │   │   ├── CommandAttributeJobFilter.cs
│   │   │   └── CommandDisplayNameAttribute.cs
│   │   ├── Helpers/
│   │   │   ├── JobHelper.cs
│   │   │   └── PerformContextAccessor.cs
│   │   └── Services/
│   │       └── BatchJobService.cs
│   │
│   ├── Persistence/
│   │   ├── Configurations/
│   │   │   ├── PersistenceDependencyInjectionConfig.cs
│   │   │   └── Extensions/
│   │   │       └── RedisExtension.cs
│   │   └── Options/
│   │       └── RedisOption.cs
│   │
│   ├── MessageQueue/
│   │   ├── Configurations/
│   │   │   ├── MessageQueueDependencyInjectionConfig.cs
│   │   │   └── Extensions/
│   │   │       └── MassTransitConfig.cs
│   │   ├── Consumers/
│   │   │   └── ProcessAssetEventConsumer.cs
│   │   └── Options/
│   │       ├── AwsOption.cs
│   │       └── SqsOption.cs
│   │
│   ├── AntiCorruption/
│   │   ├── Configurations/
│   │   │   ├── AntiCorruptionDependencyInjectionConfig.cs
│   │   │   └── Extensions/
│   │   │       └── LoggingConfig.cs
│   │   └── Services/
│   │       └── Iatec/
│   │           └── LogService.cs
│   │
│   ├── Domain.Tests/
│   │   └── Domain.Tests.csproj (empty)
│   │
│   ├── Application.Tests/
│   │   └── Application.Tests.csproj (empty)
│   │
│   └── Integration.Tests/
│       ├── Configurations/
│       │   └── InfraIntegrationTestFixture.cs  ← Testcontainers Redis + LocalStack
│       ├── Helpers/
│       │   ├── ConsumedMessageStore.cs
│       │   └── TestProcessAssetEventConsumer.cs
│       └── Tests/
│           ├── Events/
│           │   └── ProcessAssetEventTest.cs
│           ├── Jobs/
│           │   ├── ProcessAssetJobTest.cs
│           │   └── ProcessAssetBatchJobTest.cs
│           └── JobIntegrationFixture.cs
│
├── docker/
│   └── Dockerfile (empty — 0 bytes)
│
├── secrets/
│   └── secrets.yml (Kubernetes Secret template)
│
├── IATec.Standard.Net.Jobs.sln
├── README.md
└── CHANGELOG.md
```

---

## Architecture Layers

### 1. App (Presentation Layer)

Entrypoint ASP.NET Core Web API. Configures Hangfire, queues, dashboard, and all DI.

Note: the entrypoint project is named `App` (not `Api`) in this template.

### 2. Application Layer

Contains use cases, MediatR handlers, validators, dispatchers, and factories.

**Mediator Pipeline Behaviors (from `IATec.Shared.Behaviors`):**
1. `ValidatorPipelineBehavior<,>` — runs FluentValidation validators before the handler **inside the Hangfire worker** (not during the HTTP request).
2. `ExceptionPipelineBehavior<,>` — global exception handling wrapper.

### 3. Domain Layer

Contains contracts and helpers for the job system:

| Type | Description |
|------|-------------|
| `IIntegrationEvent` | Marker interface for MassTransit events |
| `IJobHelper` | Contract for Redis-based batch progress tracking |
| `IBatchJobService` | Contract for batch creation, monitoring, and cancellation |
| `BatchInfo` | Metadata about a batch (name, key, total jobs) |
| `BatchKey` | Typed key for identifying a batch in Redis |
| `BatchMonitorResult` | Result returned by `GET batch/{batchId}/monitor` |
| `BatchProgressInfo` | Fine-grained progress data from Redis |

### 4. HangFire.Jobs Layer

Core Hangfire infrastructure layer. Contains:

| Component | Purpose |
|-----------|---------|
| `BackgroundJobClientExtensions.EnqueueCommand(command)` | Enqueues any MediatR `IRequest` as a Hangfire job via `ISender.Send()`, reading `[Queue]` from the command type |
| `CommandAttributeJobFilter` | Propagates `[AutomaticRetry]`, `[Queue]`, and `[CommandDisplayName]` from command types to Hangfire job filters |
| `CommandDisplayNameAttribute` | Custom attribute for human-readable job names in the Dashboard |
| `BatchJobService` | Creates Hangfire Pro batches, enqueues `MonitorBatchCommand`, cancels batches |
| `MonitorBatchCommand` / `MonitorBatchCommandHandler` | Polls Redis progress and updates Hangfire Console progress bar until batch completes |
| `JobHelper` | Redis-based batch progress tracking (increment, get, reset) |
| `PerformContextAccessor` | Provides access to `PerformContext` (Hangfire Console) inside handlers |
| `JobRetryPolicyConstant` | Constants for queue names and retry counts |

### 5. Persistence Layer (Redis)

`RedisExtension.cs` registers `IConnectionMultiplexer` (singleton) via `StackExchange.Redis`.

Redis serves two purposes in this template:
1. **Hangfire storage** — job queue, state machine, batch metadata (via `Hangfire.Pro.Redis`).
2. **Batch progress tracking** — `IJobHelper` uses Redis hashes to track per-job completion.

`RedisOption` binds from `ConnectionStrings.RedisConnection` and parses the database index from the connection string suffix (e.g., `localhost:6379/1` → database `1`).

### 6. MessageQueue Layer (MassTransit/SQS)

`MassTransitConfig.cs` registers MassTransit with `AmazonSQS` transport:

- **`ProcessAssetEventConsumer`** — receives `ProcessAssetEvent` from SQS and enqueues a `ProcessAssetCommand` to Hangfire.
- AWS credentials and SQS options are bound from the `AWS` section in `appsettings.json`.
- `AWS.ServiceUrl` can be set to a LocalStack endpoint for local development.

### 7. AntiCorruption Layer

Adapters for external services. Currently implements:
- **IATec Log Service** (`LogService.cs`) — typed `HttpClient` sending `LogDto` via `POST v1/log`.

### 8. CrossCutting Layer

**Currently empty.** Contains only `CrossCutting.csproj` with references to `IATec.Shared.Behaviors` (`1.3.0`) and `MediatR` (`14.1.0`). **Zero `.cs` source files.**

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Redis instance running locally (or a remote instance)
- (Optional) LocalStack for SQS local development
- Hangfire Pro license (or Hangfire Pro trial) — `Hangfire.Pro` and `Hangfire.Pro.Redis` are commercial packages
- (Optional) Hangfire Ace license — required if using `Hangfire.Throttling` for job throttling/rate limiting
- Editor of your choice (VS, VS Code, Rider)

---

## How to Run

### 1. Clone the repository

```bash
git clone <repository-url>
cd {API_NAME}
```

### 2. Start Redis

```bash
docker run --rm -p 6379:6379 redis:latest
```

### 3. (Optional) Start LocalStack for SQS

```bash
docker run --rm -it -p 4566:4566 localstack/localstack
```

### 4. Restore packages

```bash
dotnet restore
```

### 5. Run the App

```bash
dotnet run --project src/App/App.csproj
```

**Default profile (from `launchSettings.json`):**
- App URL: `http://localhost:5015`
- Hangfire Dashboard: `http://localhost:5015/dashboard`
- Scalar UI: `http://localhost:5015/documentation`
- Environment: `Local`

Dashboard credentials (default): `admin` / `admin` (configured in `appsettings.json` under `Dashboard`).

---

## Configuration

Settings are located in `src/App/appsettings.json`.

### What to configure when starting a new job service

| Section | Description | Example |
|---------|-------------|---------|
| `TimeZone` | Application time zone | `"America/Sao_Paulo"` |
| `Container.Name` | Deployment container metadata name | `"MyJobs-Production"` |
| `Dashboard.Username` | Hangfire Dashboard login | `"myadmin"` |
| `Dashboard.Password` | Hangfire Dashboard password | Change before deploying |
| `AWS.ServiceUrl` | LocalStack endpoint (leave empty for real AWS) | `"http://localhost:4566"` |
| `AWS.Sqs.Region` | AWS region for SQS | `"sa-east-1"` |
| `AWS.Sqs.AccessKey` | AWS access key | `"AKIA..."` |
| `AWS.Sqs.SecretKey` | AWS secret key | `"..."` |
| `AWS.Sqs.Scope` | Queue name prefix for environment isolation | `"dev"`, `"staging"` |
| `ConnectionStrings.RedisConnection` | Redis connection string with database index | `"redis.internal:6379/2"` |

### Typed Options registered in DI

In `OptionsExtension.cs`:

| Option Class | Configuration Key | Purpose |
|--------------|-------------------|---------|
| `LogServiceOption` | `LogServiceOption` | URL for IATec Log Service |
| `ContainerOption` | `ContainerOption` | Container metadata for logging dispatcher |
| `AwsOption` | `AWS` | Root AWS config: `ServiceUrl`, `Sqs` |
| `RedisOption` | `ConnectionStrings` | Redis connection string and database index |
| `DashboardOption` | `Dashboard` | Hangfire Dashboard credentials |

---

## API Documentation (Scalar)

The project uses **Scalar** (not Swagger) for interactive API documentation.

- **OpenAPI JSON:** `/openapi/v1.json`, `/openapi/v2.json`
- **Scalar UI:** `/documentation`
- **Availability:** Only in `Local` or `Development` environments.
- **Theme:** Mars, dark mode, C# HttpClient as default client.

---

## Health Checks

Endpoint: `GET /_healthcheck/status`

- **VersionHealthCheck** — assembly version string.
- **RedisHealthCheck** — verifies Redis connectivity (`AspNetCore.HealthChecks.Redis`).
- **HangfireHealthCheck** — verifies Hangfire server is running (`AspNetCore.HealthChecks.Hangfire`).
- Response is formatted via `HealthChecks.UI.Client`.

---

## API Versioning

Configured in `VersioningExtension.cs` using **URL segment** strategy (`v{version:apiVersion}`):

- **Default version:** `1.0`
- **v1:** Deprecated — endpoints still functional but marked deprecated.
- **v2:** Current stable — preferred for new integrations.

---

## CORS

Configured in `CorsPolicyExtension.cs` with `AllowAnyHeader`, `AllowAnyMethod`, `AllowAnyOrigin`.

**Exposed headers:** `X-Custom-Header`, `Location`, `Content-Disposition`, `Content-Length`.

> ⚠️ **Warning:** `AllowAnyOrigin()` is extremely permissive. Review and restrict before deploying to production.

---

## Authentication

**Currently NOT configured** for API endpoints.

The Hangfire Dashboard uses Basic Authentication (`HangfireDashboardAuthFilter`) with credentials from the `Dashboard` section in `appsettings.json`.

---

## Jobs Layer (Hangfire)

### Pattern 2: Command = Job

Commands are MediatR `IRequest<Result>` records that are enqueued directly as Hangfire jobs and dispatched to handlers via `ISender.Send()` inside the worker. No separate job wrapper class is needed.

```csharp
// Enqueue a job:
var jobId = backgroundJobClient.EnqueueCommand(new ProcessAssetCommand(assetId));

// The handler runs inside the Hangfire worker:
public class ProcessAssetCommandHandler(ILogger<ProcessAssetCommandHandler> logger) : IRequestHandler<ProcessAssetCommand, Result>
{
    public Task<Result> Handle(ProcessAssetCommand request, CancellationToken ct)
    {
        // Your business logic here
        throw new NotImplementedException();
    }
}
```

### Queue and Retry Configuration

Decorate command records with Hangfire attributes:

```csharp
[AutomaticRetry(Attempts = 3)]
[Queue("critical")]
[CommandDisplayName("Process Asset: {0}")]
public sealed record ProcessAssetCommand(Guid AssetId) : IRequest<Result>
{
    public override string ToString() => AssetId.ToString();
}
```

`CommandAttributeJobFilter` (Order = -1) propagates these attributes automatically to the Hangfire job.

### Batches

`ProcessAssetBatchCommand` creates a Hangfire Pro batch via `IBatchJobService`. A `MonitorBatchCommand` is automatically enqueued to track progress in the Hangfire Console.

| Endpoint | v1 | v2 | Description |
|----------|----|----|-------------|
| `POST api/v{n}/jobs/assets/process` | ✓ (deprecated) | ✓ | Enqueue a single `ProcessAssetCommand` |
| `POST api/v{n}/jobs/assets/process-batch` | ✓ (deprecated) | ✓ | Enqueue a `ProcessAssetBatchCommand` (creates batch) |
| `GET api/v{n}/jobs/batch/{batchId}/monitor` | ✓ | ✓ | Get batch status and timing |
| `POST api/v{n}/jobs/batch/{batchId}/cancel` | ✓ | ✓ | Cancel a pending batch |
| `GET api/v{n}/jobs/batch/{batchKey}/progress` | ✓ | ✓ | Get Redis-based fine-grained progress |

> v1 `process` and `process-batch` accept `ProcessAssetCommand[]` (array); v2 accepts `IReadOnlyCollection<ProcessAssetCommand>`.

### Hangfire Dashboard

Available at `/dashboard`. Protected by Basic Authentication. Displays all jobs, batches, queues, and recurring jobs with custom display names from `[CommandDisplayName]`.

---

## Feature: Assets

Sample feature demonstrating single job and batch processing patterns.

| File | Type | Details |
|------|------|---------|
| `ProcessAssetCommand.cs` | `sealed record` | `IRequest<Result>`. Has `[AutomaticRetry]`, `[Queue]`, `[CommandDisplayName]` attributes. |
| `ProcessAssetCommandHandler.cs` | Handler | `throw new NotImplementedException()` — replace with real logic |
| `ProcessAssetBatchCommand.cs` | `sealed record` | `IRequest<Result>`. Wraps `IReadOnlyCollection<ProcessAssetCommand>`. |
| `ProcessAssetBatchCommandHandler.cs` | Handler | Creates Hangfire Pro batch, enqueues individual jobs, starts monitoring |
| `ProcessAssetEvent.cs` | MassTransit event | `IIntegrationEvent` — published to SQS, consumed by `ProcessAssetEventConsumer` |
| `AssetValidationRules.cs` | Validation rules | Shared rules used by `ProcessAssetCommandValidator` |
| `ProcessAssetCommandValidator.cs` | `AbstractValidator<ProcessAssetCommand>` | Runs inside the Hangfire worker via MediatR pipeline |

---

## Logging Dispatcher

### `LogDispatcher.cs`

Implements `ILogDispatcher` (from `IATec.Shared.Domain.Contracts.Dispatcher`).

**Constructs a `LogDto` with:**
- `ContainerKey` — from `ContainerOption.Key`
- `Source`, `Owner`, `Action` — caller-provided
- `Content` — `object?.ToString() ?? string.Empty`
- `Date` — `DateTime.UtcNow`
- `Id` — `string.Empty`
- `UserId` — `string.Empty`

Then delegates to `ILogService.SendAsync()`.

### `ILogService` implementation

**`LogService.cs`** (AntiCorruption layer):
- Typed `HttpClient` with base address from `LogServiceOption.Url`.
- Sends `POST v1/log` as JSON.
- **Does NOT throw on failure** — errors are caught and logged silently.

---

## External Services (AntiCorruption)

### IATec Log Service

| Config | File |
|--------|------|
| DI Registration | `LoggingConfig.cs` |
| Implementation | `LogService.cs` |
| Contract | `ILogService` (from `IATec.Shared.Domain`) |

---

## Persistence Layer (Redis)

`RedisExtension.cs` registers `IConnectionMultiplexer` (singleton).

- Connection string format: `host:port/database` (e.g., `localhost:6379/1`).
- `RedisOption.HostConnectionString` strips the `/database` suffix for `ConnectionMultiplexer`.
- `RedisOption.Database` parses the integer database index.

Redis is used by both Hangfire Pro (job storage) and `IJobHelper` (batch progress tracking).

---

## Message Queue Layer (MassTransit/SQS)

`MassTransitConfig.cs` configures MassTransit with Amazon SQS transport:

- **`ProcessAssetEventConsumer`** — receives `ProcessAssetEvent` from SQS queue and enqueues `ProcessAssetCommand` to Hangfire.
- Queue names are scoped using `AWS.Sqs.Scope` for environment isolation.
- Set `AWS.ServiceUrl` to a LocalStack endpoint for local development.

---

## Domain Layer

Contains contracts and helpers for job orchestration:

| Type | Description |
|------|-------------|
| `IIntegrationEvent` | Marker interface for MassTransit events |
| `IJobHelper` | Redis-based batch progress contract |
| `IBatchJobService` | Batch lifecycle contract (create, monitor, cancel) |
| `BatchInfo` | Batch name, key, and total job count |
| `BatchKey` | Typed Redis key for a batch (`FromJobId(jobId)`) |
| `BatchMonitorResult` | Status, timing, and progress for a batch |
| `BatchProgressInfo` | Fine-grained per-key progress from Redis |

---

## CrossCutting Layer

**Status: Completely empty.**

Contains only `CrossCutting.csproj` with references to `IATec.Shared.Behaviors` (`1.3.0`) and `MediatR` (`14.1.0`). **Zero `.cs` source files.**

---

## Test Projects

| Project | Type | Framework | Status |
|---------|------|-----------|--------|
| `Domain.Tests` | Unit | None configured | Empty |
| `Application.Tests` | Unit | None configured | Empty |
| `Integration.Tests` | Integration | xUnit + Testcontainers | Real tests included |

### Integration Tests

`InfraIntegrationTestFixture` starts real containers:
- **Redis** via Testcontainers — used for Hangfire storage and progress tracking.
- **LocalStack** via Testcontainers — emulates SQS for event consumer tests.

Includes tests for:
- `ProcessAssetJobTest` — verifies single job enqueue and execution.
- `ProcessAssetBatchJobTest` — verifies batch creation, monitoring, and completion.
- `ProcessAssetEventTest` — verifies end-to-end SQS event → Hangfire job flow.

---

## Docker

### `docker/Dockerfile`
**Empty (0 bytes).**

### Basic Dockerfile example

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/App/App.csproj"
RUN dotnet build "src/App/App.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/App/App.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "App.dll"]
```

---

## CI/CD

**Status: No CI/CD configured.**

- No `.github/` folder.
- No GitHub Actions workflows.
- No Azure DevOps pipelines.
- No Jenkins/GitLab CI files.

The only file in `secrets/` is `secrets.yml`, a **Kubernetes Secret manifest template** with placeholders (`#{deployment_name}#`, `#{namespace}#`).

---

## Renaming the API

> Follow these steps when using this project as a base for a new job service.

### 1. Solution file

```bash
mv IATec.Standard.Net.Jobs.sln {API_NAME}.sln
```

### 2. Assembly and namespaces

Update `App.csproj` if desired:
```xml
<AssemblyName>{API_NAME}.App</AssemblyName>
<RootNamespace>{API_NAME}.App</RootNamespace>
```

Run a **Replace All** in `src/` to adjust namespaces:

| From | To (example) |
|------|--------------|
| `namespace App;` | `namespace MyProject.App;` |
| `namespace Application;` | `namespace MyProject.Application;` |
| `namespace HangFire.Jobs;` | `namespace MyProject.HangFire.Jobs;` |
| `namespace Domain;` | `namespace MyProject.Domain;` |

Or keep simplified namespaces (`App`, `Application`, `Domain`, etc.).

### 3. Scalar / OpenAPI title

In `src/App/Configurations/Extensions/ScalarExtension.cs`, replace `{API_NAME}` with the actual project name.

### 4. Dashboard credentials

In `appsettings.json`, update `Dashboard.Username` and `Dashboard.Password` before deploying.

### 5. README and CHANGELOG

Replace all `{API_NAME}` placeholders with the actual project name.

### 6. Version

Update `<Version>` in `App.csproj` to `1.0.0` for the new project.

---

## Template Extension Points

This project is an **intentional scaffold/template**. The following items are **structural placeholders** — not bugs or missing features.

| # | Extension Point | Location | Purpose |
|---|----------------|----------|---------|
| 1 | `NotImplementedException` in `ProcessAssetCommandHandler` | `Application/Features/Assets/` | Replace with actual job business logic |
| 2 | Empty `CrossCutting` layer | `src/CrossCutting/` | Add shared utilities or cross-cutting concerns |
| 3 | No CI/CD | Root | Add GitHub Actions / Azure DevOps when ready |
| 4 | Empty Dockerfile | `docker/` | Add build/publish steps for containerization |
| 5 | Permissive CORS | `CorsPolicyExtension.cs` | Restrict origins/methods when deploying to production |
| 6 | Default Dashboard credentials | `appsettings.json` | Change `admin/admin` before production |
| 7 | Empty `AWS.ServiceUrl` | `appsettings.json` | Set LocalStack URL for local dev, leave empty for real AWS |
| 8 | `{API_NAME}` placeholders | `ScalarExtension.cs`, README | Rename when cloning template |
| 9 | Unit test projects | `*.Tests/` | Add xUnit/NUnit/MSTest and write unit tests |
| 10 | MassTransit license | `MessageQueue.csproj` | Pinned to `8.5.9` (last pre-9.x release) — upgrade to 9.x+ when license is acquired |

---

## Contributing

Contributions are welcome! To contribute:

1. Fork the repository.
2. Create a branch: `git checkout -b feature/feature-name`.
3. Commit with clear messages following [Conventional Commits](https://www.conventionalcommits.org/).
4. Open a Pull Request for review.

---

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for full release history.
