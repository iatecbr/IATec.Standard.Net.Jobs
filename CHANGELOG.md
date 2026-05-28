# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.1.0] — 2026-05-26

### ADDED

- `README.md` — full template documentation: architecture, configuration, Hangfire job patterns, batch monitoring, Redis, MassTransit/SQS, integration tests, Docker, CI/CD, renaming guide, and Template Extension Points table.
- `CHANGELOG.md` — full release history.

### UPDATED

- `App.csproj` version bumped to `2.1.0`.

---

## [2.0.0] — 2026-05-26

### ADDED

- `Serilog.AspNetCore` `10.0.0` and `Serilog` `4.3.1` for structured logging.
- Hangfire Dashboard Basic Authentication via `HangfireDashboardAuthFilter` and `DashboardOption` (`Dashboard.Username`, `Dashboard.Password`).
- `AspNetCore.HealthChecks.Redis` `9.0.0` and `AspNetCore.HealthChecks.Hangfire` `9.0.0` added to `App.csproj`.

### UPDATED

- **BREAKING:** Migrated from Pattern 1 (BaseJob wrapper) to **Pattern 2 (Command = Job)** — commands are MediatR `IRequest<Result>` records enqueued directly via `IBackgroundJobClient.EnqueueCommand(command)` and dispatched via `ISender.Send()` inside the Hangfire worker. `BaseCommand<T>` removed.
- **BREAKING:** `ProcessAssetCommand` and `ProcessAssetBatchCommand` converted to positional `sealed record` types with `[AutomaticRetry]`, `[Queue]`, and `[CommandDisplayName]` attributes.
- **BREAKING:** Domain models (`BatchInfo`, `BatchKey`, `BatchMonitorResult`, `BatchProgressInfo`) moved from Domain entities to `Domain.Helpers` namespace.
- **BREAKING:** `AwsOption` and `SqsOption` moved from `Domain` to `MessageQueue.Options`. `RedisOption` moved to `Persistence.Options`.
- **BREAKING:** `ProcessAssetEventConsumer` moved from `Application` to `MessageQueue` layer.
- `CommandAttributeJobFilter` (Order = -1) propagates `[AutomaticRetry]`, `[Queue]`, and `[CommandDisplayName]` from command types to Hangfire job filters. Default global `AutomaticRetryAttribute` (Attempts=10) removed.
- `UseConsole()` idempotency guard added — static lock prevents double-initialization in integration test environments with multiple `WebApplicationFactory` instances.
- `RedisOption` updated — parses database index from connection string suffix (`/N`); `HostConnectionString` strips the suffix for `ConnectionMultiplexer`.
- `MonitorBatchCommand` — all properties are positional constructor parameters for correct Newtonsoft.Json round-trip serialization.
- `IATec.Shared.Application` `1.1.0` → `2.0.0`.
- `IATec.Shared.Domain` `1.2.0` → `2.0.1`.
- `IATec.Shared.Behaviors` `1.2.0` → `1.3.0`.
- `IATec.Shared.HttpClient` `2.1.0` → `3.0.0`.
- `IATec.Shared.Api` `1.1.0` → `1.2.0`.
- `Microsoft.AspNetCore.OpenApi` `10.0.1` → `10.0.8`.
- `Microsoft.Extensions.*` packages bumped to `10.0.8`.

### FIXED

- `LogDispatcher.cs`: `Content = content?.ToString()!` replaced with `Content = content?.ToString() ?? string.Empty`.
- Monitor timeout — `MonitorBatchCommand` now respects a configurable timeout to prevent infinite polling.

### REMOVED

- `BaseJob<T>` wrapper class (Pattern 1) — replaced by Pattern 2.

---

## [1.4.0] — 2026-04-06

### ADDED

- Code style consistency pass across solution.
- `Integration.Tests` project with real Testcontainers (Redis + LocalStack SQS):
  - `InfraIntegrationTestFixture` — starts Redis and LocalStack containers.
  - `ProcessAssetJobTest`, `ProcessAssetBatchJobTest` — verifies job enqueue and batch execution.
  - `ProcessAssetEventTest` — verifies end-to-end SQS event → Hangfire job flow.
  - `ConsumedMessageStore` and `TestProcessAssetEventConsumer` — test helpers.
- Unit tests restructured: `Domain.Tests` and `Application.Tests` re-organized following `Accounts.Api` pattern.

### FIXED

- Dead code removed from `App` layer.
- Validation rules extracted to `AssetValidationRules.cs`.

---

## [1.3.0] — 2026-04-01

### ADDED

- Hangfire Pro Batch support via `IBatchJobService`:
  - `BatchJobService` — creates Pro batches, enqueues jobs, auto-enqueues `MonitorBatchCommand`.
  - `MonitorBatchCommand` / `MonitorBatchCommandHandler` — polls Redis progress, updates Hangfire Console progress bar.
  - `BatchJobServiceExtensions.EnqueueBatch(...)` — helper for batch creation.
- `IJobHelper` / `JobHelper` — Redis-based batch progress tracking (increment, get, reset).
- `PerformContextAccessor` / `IPerformContextAccessor` — provides `PerformContext` (Hangfire Console) to handlers.
- `CommandDisplayNameAttribute` — custom attribute for Hangfire Dashboard display names.
- API versioning via URL segment with v1 (deprecated) and v2 (current) on `JobsController`:
  - `POST api/v2/jobs/assets/process` — single job
  - `POST api/v2/jobs/assets/process-batch` — batch job
  - `GET api/v2/jobs/batch/{batchId}/monitor` — batch status
  - `POST api/v2/jobs/batch/{batchId}/cancel` — batch cancellation
  - `GET api/v2/jobs/batch/{batchKey}/progress` — Redis progress
- `DashboardOption` and `HangfireDashboardAuthFilter` — Basic Auth for Hangfire Dashboard.
- `Program.cs`, `appsettings.json` with Redis and AWS/SQS configuration.
- `JobsController` with full v1/v2 Hangfire showcase.
- Hangfire Pro configuration: `UseRedisStorage`, `UseBatches`, `UseThrottling`, `UseConsole`.
- `HangfireQueuesExtension` — configures `BackgroundJobServer` queues.

---

## [1.2.0] — 2026-03-26

### ADDED

- Initial Jobs template: `App.csproj` with Hangfire, Redis, Scalar, versioning, and health checks.
- `HangFire.Jobs` project with `BaseCommand`, `BackgroundJobClientExtensions`, `CommandAttributeJobFilter`, `JobRetryPolicyConstant`.
- `MessageQueue` layer with MassTransit/SQS: `ProcessAssetEventConsumer`, `AwsOption`, `SqsOption`.
- `Persistence` layer with Redis: `RedisExtension`, `RedisOption`.
- `Domain` contracts: `IIntegrationEvent`, `IJobHelper`, `IBatchJobService`, batch helpers.
- `Application` layer: `ProcessAssetCommand`, `ProcessAssetBatchCommand`, `ProcessAssetEvent`, handlers, validator.
- `AntiCorruption` layer: `LogService`, `LoggingConfig`.

---

## [1.1.0] — 2026-03-06

### ADDED

- Initial implementation of the jobs pattern with unit tests for `ProcessAssetJob`, `ProcessAssetBatchJob`, and `BatchMonitorJob`.

---

## [1.0.0] — 2024-05-28

### ADDED

- Repository bootstrap with `LICENSE`, `.gitignore`, and initial project skeleton.
