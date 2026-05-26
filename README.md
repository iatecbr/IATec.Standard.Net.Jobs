# 🚀 {API_NAME}

> Robust .NET API for job scheduling and background processing at IATec, promoting standard practices, efficiency, security, and scalability. Ideal for batch processing, recurring tasks, and high-performance job execution.

---

## 📋 Index

- [About the Project](#about-the-project)
- [Technologies and Stack](#technologies-and-stack)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [How to Run](#how-to-run)
- [Configuration](#configuration)
- [Health Checks](#health-checks)
- [API Documentation (Scalar)](#api-documentation-scalar)
- [Hangfire Dashboard](#hangfire-dashboard)
- [Hangfire Queues and Workers](#hangfire-queues-and-workers)
- [Job Retry Policies](#job-retry-policies)
- [Batch Jobs](#batch-jobs)
- [Message Queue](#message-queue)
- [Tests](#tests)
- [Renaming the API](#renaming-the-api)
- [Docker](#docker)
- [Contributing](#contributing)

---

## About the Project

This repository is a **base template** for creating new .NET job scheduling APIs following IATec standards. It comes pre-configured with:

- Decoupled layered architecture (Domain, Application, Persistence, AntiCorruption, MessageQueue, HangFire.Jobs, CrossCutting, App).
- API versioning for controllers that schedule/enqueue jobs.
- Automatic documentation via **Scalar/OpenAPI**.
- Fully configured **Hangfire** dashboard with Redis storage.
- **Hangfire Pro** features: Batches and Throttling.
- Command-based job enqueuing with attribute-based retry policies.
- **MassTransit** message bus with **Amazon SQS** transport.
- Health Checks with Redis, Hangfire, and API version status.
- CORS configuration.
- Integration with shared libraries (`IATec.Shared.*`).
- Validation and fluent results (`FluentValidation`, `FluentResults`).
- MediatR for inter-layer communication and job dispatch.
- Integration tests with testcontainers-ready infrastructure.

> **Note:** Whenever creating a new API from this template, read the [Renaming the API](#renaming-the-api) section to adjust names and references.

---

## Technologies and Stack

| Technology | Version |
|------------|---------|
| .NET | 10.0 |
| ASP.NET Core | 10.0.x |
| Hangfire.AspNetCore | 1.8.23 |
| Hangfire.Pro + Redis | 3.x |
| Hangfire.Console | 1.4.3 |
| Hangfire.Throttling | 1.4.3 |
| StackExchange.Redis | 2.13.1 |
| MassTransit | 9.1.1 |
| MassTransit.AmazonSQS | 9.1.1 |
| Scalar.AspNetCore | 2.14.14 |
| Microsoft.AspNetCore.OpenApi | 10.0.8 |
| API Versioning (Asp.Versioning.Mvc) | 10.0.0 |
| HealthChecks (Redis, Hangfire, UIClient) | 9.0.0 |
| MediatR | 14.1.0 |
| FluentValidation | 12.1.1 |
| FluentResults | 4.0.0 |
| Serilog | 4.3.1 |
| IATec.Shared.* | As per `csproj` files |

---

## Architecture

The project follows a layered organization inside the `src/` folder:

```
src/
├── App/                    → ASP.NET Core entrypoint (Controllers, Startup, Configs)
├── Application/            → Use cases, handlers, application logic, command-based jobs
├── CrossCutting/           → Shared behaviors, MediatR pipelines
├── Domain/                 → Entities, contracts, validations, pure business rules
├── Persistence/            → Data access, Redis connection helpers
├── AntiCorruption/         → Adapters for external services (IATec Log Service, HTTP Clients)
├── MessageQueue/           → MassTransit consumers/producers (Amazon SQS)
├── HangFire.Jobs/          → Base commands, job filters, batch services, helpers
├── Domain.Tests/           → Domain layer unit tests
├── Application.Tests/      → Application layer unit tests
└── Integration.Tests/      → Integration tests (xUnit + Testcontainers-ready)
```

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or compatible higher version)
- [Redis](https://redis.io/) (required for Hangfire storage)
- (Optional) Docker for building/publishing images
- (Optional) AWS credentials for SQS message queue
- Editor of your choice (VS, VS Code, Rider)

---

## How to Run

### 1. Clone the repository

```bash
git clone <repository-url>
cd {API_NAME}
```

### 2. Configure Redis

Ensure Redis is running locally or update `ConnectionStrings.RedisConnection` in `src/App/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "RedisConnection": "localhost:6379/1"
  }
}
```

### 3. Restore packages

```bash
dotnet restore
```

### 4. Run the API

```bash
dotnet run --project src/App/App.csproj
```

By default, the application will be available at:
- `http://localhost:5000`
- `https://localhost:5001` (if configured)

> Check the console output for the exact port when starting the project.

### 5. Access the Hangfire Dashboard

Open your browser at:

```
http://localhost:5000/dashboard
```

Default credentials (see `appsettings.json` under `Dashboard`):
- **Username:** `admin`
- **Password:** `admin`

---

## Configuration

Settings are located in `src/App/appsettings.json` (and its environment overrides, such as `appsettings.Development.json`).

**Current structure example:**

```json
{
  "TimeZone": "UTC",
  "Container": {
    "Name": "Vertical-ContextContainerType",
    "ContainerId": "ContainerId"
  },
  "Dashboard": {
    "Username": "admin",
    "Password": "admin"
  },
  "AWS": {
    "ServiceUrl": "",
    "Sqs": {
      "Region": "us-east-1",
      "AccessKey": "",
      "SecretKey": "",
      "Scope": "",
      "RetryCount": 3,
      "IntervalMilliSeconds": 1000
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "ConnectionStrings": {
    "RedisConnection": "localhost:6379/1"
  }
}
```

### What to configure when starting a new API

| Section | Description | Example |
|---------|-------------|---------|
| `TimeZone` | Application time zone | `"America/Sao_Paulo"` |
| `Container` | Deployment/container metadata | Adjust `Name` and `ContainerId` according to your environment |
| `Dashboard` | Hangfire Dashboard basic auth credentials | Change username/password before production |
| `AWS.Sqs` | Amazon SQS configuration for MassTransit | `Region`, `AccessKey`, `SecretKey`, `Scope` |
| `Logging` | ASP.NET Core log level | `"Debug"`, `"Information"`, `"Warning"` |
| `ConnectionStrings.RedisConnection` | Redis host + database for Hangfire | `"my-redis:6379/2"` |

> **Tip:** Add new configuration sections in `src/App/Configurations/Extensions/OptionsExtension.cs` for typed injection via `IOptions<T>`.

---

## Health Checks

The project exposes a health check endpoint returning the API version and infrastructure status:

```
GET /_healthcheck/status
```

Features:
- Returns `Healthy`/`Degraded`/`Unhealthy` status.
- Includes the API assembly version in the response body.
- Checks **Redis** connection (Hangfire storage).
- Checks **Hangfire** status (failed jobs threshold + available servers).
- Response in `HealthChecks.UI.Client` visual format.

---

## API Documentation (Scalar)

Interactive documentation powered by **Scalar** and native ASP.NET Core **OpenAPI** is available in **non-Production** environments:

- OpenAPI JSON: `/openapi/v1.json` and `/openapi/v2.json`
- Scalar UI: `/documentation`

### Configured features

- Automatically generated from native `Microsoft.AspNetCore.OpenApi`.
- **Kepler theme** with dark mode.
- Two documented versions: **v1** (deprecated endpoints) and **v2** (stable).
- Default HTTP client configured as **C# HttpClient**.
- Title: `{API_NAME}`.

---

## Hangfire Dashboard

The **Hangfire Dashboard** is exposed at the `/dashboard` route with basic authentication.

### Dashboard configuration

Configured in `src/App/Configurations/Extensions/HangfireExtension.cs`:
- **Dashboard Title:** `{API_NAME} Jobs Dashboard` (rename when creating a new project).
- **Authorization:** `HangfireDashboardAuthFilter` using credentials from `appsettings.json` (`Dashboard` section).
- **Display Name Resolution:** Custom `DisplayNameFunc` reads `[CommandDisplayName]` attributes on command classes to show friendly names in the Dashboard.

### Dashboard Security

> **Attention:** Change the `Dashboard` credentials in `appsettings.json` before deploying to production. The default is `admin`/`admin`.

---

## Hangfire Queues and Workers

The application pre-configures Hangfire job queues and worker count.

### Default queue configuration

Located in `src/App/Configurations/Extensions/HangfireQueuesExtension.cs`:

```csharp
services.AddHangfireServer(options =>
{
    options.Queues = ["default", "heavy"];
    options.WorkerCount = Environment.ProcessorCount * 2;
});
```

| Setting | Default | Description |
|---------|---------|-------------|
| `Queues` | `["default", "heavy"]` | Job queues. Add new queues for job segregation. |
| `WorkerCount` | `ProcessorCount * 2` | Number of concurrent workers per server. |

When creating commands, assign a queue with:

```csharp
[Queue("heavy")]
public class MyHeavyJobCommand : BaseCommand<MyHeavyJobCommand>
{
    // ...
}
```

---

## Job Retry Policies

Retry behavior is controlled via attributes on command classes.

### How it works

- The default Hangfire `AutomaticRetryAttribute` is **removed** globally to avoid overriding command-specific policies.
- The `CommandAttributeJobFilter` propagates `[AutomaticRetry]`, `[Queue]`, and `[CommandDisplayName]` from commands to jobs enqueued via `ISender.Send(command)`.

### Example command with retry policy

```csharp
[Queue("default")]
[CommandDisplayName("Process daily report for {0}")]
[AutomaticRetry(Attempts = 5, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
public class ProcessReportCommand : BaseCommand<ProcessReportCommand>
{
    public string ReportName { get; set; }

    public override string ToString() => ReportName;
}
```

### Available retry constants

Located in `src/HangFire.Jobs/Constants/JobRetryPolicyConstant.cs`:

| Constant | Value | Description |
|----------|-------|-------------|
| `DefaultRetryAttempts` | `3` | Default retry count |
| `DefaultRetryDelayInSeconds` | `60` | Delay between retries |

---

## Batch Jobs

This template includes **Hangfire.Pro** features for batch processing.

### BatchJobService

Located in `src/HangFire.Jobs/Services/BatchJobService.cs`:
- `StartBatchAsync` — starts a new batch and returns the batch ID.
- `AwaitBatchCompletionAsync` — waits for a batch to complete (or timeout).
- `MonitorBatchCommand` / `MonitorBatchCommandHandler` — scheduled job to monitor batch completion.

### Extension methods

Located in `src/HangFire.Jobs/Extensions/BatchJobServiceExtensions.cs`:
- `BatchCreateJobs(IEnumerable<string> jobIds, int batchSize)` — splits a list of job IDs into batches.

### Throttling

Hangfire.Throttling is enabled globally in `HangfireExtension.cs` to prevent resource exhaustion during high-throughput job execution.

---

## Message Queue

The project uses **MassTransit** with **Amazon SQS** transport for asynchronous messaging.

### Configuration

Located in `src/MessageQueue/Configurations/`:
- `MassTransitConfig.cs` — configures MassTransit with Amazon SQS.
- `MessageQueueDependencyInjectionConfig.cs` — dependency injection setup.

### Consumers

Example consumer located in `src/MessageQueue/Consumers/ProcessAssetEventConsumer.cs`.

---

## Tests

The template includes three test projects:

| Project | Layer Tested | Framework |
|---------|--------------|-----------|
| `Domain.Tests` | Domain | xUnit / MSTest (to be configured as needed) |
| `Application.Tests` | Application | xUnit / MSTest (to be configured as needed) |
| `Integration.Tests` | API + Infrastructure | xUnit / Testcontainers-ready |

To run all tests:

```bash
dotnet test
```

> **Note:** Integration tests may require Docker for testcontainers (Redis, etc.).

---

## Renaming the API

> Whenever using this project as a base for a new API, follow the steps below to adjust names and references. The text `{API_NAME}` used throughout this README acts as a placeholder for the **actual project name** you want to use.

### Step-by-step guide

#### 1. Clone the repository and enter the folder

```bash
git clone <repository-url>
cd {API_NAME}
```

#### 2. Rename the Solution file

```bash
mv IATec.Standard.Net.Jobs.sln {API_NAME}.sln
```

#### 3. Rename `AssemblyName` and `RootNamespace` in `.csproj` files

Open all `src/**/*.csproj` files and change:

```xml
<PropertyGroup>
    <AssemblyName>{API_NAME}</AssemblyName>
    <RootNamespace>{API_NAME}</RootNamespace>
</PropertyGroup>
```

> By default these fields are optional and inherit the file name. Explicitly defining them prevents assembly name mismatches after renaming.

#### 4. Adjust C# namespaces

Run a **Replace All** in the `src/` folder for each project layer. Example:

| From | To (example) |
|------|--------------|
| `namespace App;` | `namespace ProjectName.App;` |
| `namespace Application;` | `namespace ProjectName.Application;` |
| `namespace Domain;` | `namespace ProjectName.Domain;` |
| `namespace HangFire.Jobs;` | `namespace ProjectName.HangFire.Jobs;` |

Or keep simplified namespaces (`App`, `Domain`, `Application`, `HangFire.Jobs`, etc.) — this is a team preference.

#### 5. Update Scalar title

Open `src/App/Configurations/Extensions/ScalarExtension.cs` and change:

```csharp
document.Info.Title = "{API_NAME}";
```

And also:

```csharp
.WithTitle("{API_NAME}")
```

#### 6. Update Hangfire Dashboard title

Open `src/App/Configurations/Extensions/HangfireExtension.cs` and change:

```csharp
DashboardTitle = "{API_NAME} Jobs Dashboard",
```

#### 7. Update README

Replace **all** occurrences of `{API_NAME}` in this `README.md` with the actual project name.

You can use your editor's `Find & Replace` (usually `Ctrl+Shift+H`) with the following text:

- **Find:** `{API_NAME}`
- **Replace:** `MyNewProjectName`

#### 8. Review and commit

After all changes, run a full build to ensure everything compiles:

```bash
dotnet build
```

Then commit your changes:

```bash
git add .
git commit -m "refactor: rename template API to {API_NAME}"
```

---

### Quick Checklist

Use this checklist to ensure you didn't miss any step:

- [ ] Repository cloned and folder renamed to new API name
- [ ] `.sln` file renamed to `{API_NAME}.sln`
- [ ] `AssemblyName` updated in all `.csproj` files
- [ ] `RootNamespace` updated in all `.csproj` files
- [ ] Namespaces adjusted in source code (`src/`)
- [ ] Scalar `Title` updated in `ScalarExtension.cs`
- [ ] Hangfire Dashboard `DashboardTitle` updated in `HangfireExtension.cs`
- [ ] All `{API_NAME}` placeholders replaced in `README.md`
- [ ] `dotnet build` runs successfully with zero errors
- [ ] Changes committed to version control

---

## Docker

There is `docker/Dockerfile` prepared for building the application, but it is currently **empty**.

> **Attention:** The Dockerfile is currently empty. When creating a new API from this template, fill it according to your build pipeline. Basic example:

```dockerfile
# syntax=docker/dockerfile:1
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

## Contributing

Contributions are welcome! To contribute:

1. Fork the repository.
2. Create a branch for your feature or fix: `git checkout -b feature/feature-name`.
3. Commit your changes with clear messages.
4. Open a Pull Request for review.

---

> **Note:** This is a base template. Feel free to add/remove layers, packages, queues, and configurations according to your business domain needs.
