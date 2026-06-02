# AGENTS.md

## Build & Run

```bash
dotnet restore
dotnet build
dotnet run --project src/App/App.csproj
```

- App runs at `http://localhost:5015` (`ASPNETCORE_ENVIRONMENT=Local`).
- Hangfire Dashboard: `http://localhost:5015/dashboard` (Basic Auth: `admin`/`admin` by default).
- Scalar UI: `http://localhost:5015/documentation`.

## Tests

```bash
# All tests
dotnet test

# Integration tests only (requires Docker — starts Redis + LocalStack automatically via Testcontainers)
dotnet test src/Integration.Tests/Integration.Tests.csproj

# Domain/Application unit tests (currently empty projects)
dotnet test src/Domain.Tests/Domain.Tests.csproj
dotnet test src/Application.Tests/Application.Tests.csproj
```

Integration tests spin up **real containers** (Redis `redis:7-alpine`, LocalStack `localstack/localstack:4`) — no mocks, no pre-running services needed. Docker must be available.

## Versioning

When updating packages or adding features, bump these three places together:
1. `src/App/App.csproj` → `<Version>`
2. `CHANGELOG.md` → new `[x.y.z] — YYYY-MM-DD` section
3. `README.md` → `**Current Version:**` line and the Technologies table row(s)

## Project Structure

| Directory | Purpose |
|-----------|---------|
| `src/App` | ASP.NET Core entrypoint — DI wiring, Hangfire config, controllers |
| `src/HangFire.Jobs` | Hangfire infrastructure: `EnqueueCommand`, `CommandAttributeJobFilter`, `BatchJobService`, `MonitorBatchCommand` |
| `src/Application` | MediatR handlers, validators, `LogDispatcher` |
| `src/Domain` | Contracts (`IJobHelper`, `IBatchJobService`) and helpers (`BatchInfo`, `BatchKey`, etc.) |
| `src/Persistence` | Redis via `StackExchange.Redis` — `RedisOption` parses DB index from `host:port/N` suffix |
| `src/MessageQueue` | MassTransit/SQS — `ProcessAssetEventConsumer`, `AwsOption`, `SqsOption` |
| `src/AntiCorruption` | IATec Log Service typed HttpClient |
| `src/CrossCutting` | **Empty** — only holds project references to `IATec.Shared.Behaviors` and `MediatR` |

## Key Architecture Rules

- **Pattern 2 (Command = Job):** Commands are `sealed record` types implementing `IRequest<Result>`. They are enqueued with `backgroundJobClient.EnqueueCommand(command)` and dispatched to MediatR handlers via `ISender.Send()` inside the Hangfire worker. No `BaseJob` wrapper exists.
- Hangfire attributes (`[AutomaticRetry]`, `[Queue]`, `[CommandDisplayName]`) go on the **command record**, not the handler. `CommandAttributeJobFilter` (Order = -1) propagates them automatically.
- `MonitorBatchCommand` must use **positional constructor parameters** for correct Newtonsoft.Json round-trip serialization with Hangfire.
- `RedisOption.HostConnectionString` strips the `/N` suffix; `RedisOption.Database` is the parsed integer — always use these two properties, never parse the connection string manually.
- `UseConsole()` has an idempotency guard via a static lock — safe in multi-`WebApplicationFactory` test environments.

## Integration Test Wiring

- `InfraIntegrationTestFixture` extends `WebApplicationFactory<Program>` and implements `IAsyncLifetime`.
- It overrides Redis and AWS options via `ConfigureWebHost` using `services.RemoveAll<IConfigureOptions<T>>()` before re-registering — this is intentional to handle multiple `IConfigureOptions` registrations from `.Bind()`.
- The `TestProcessAssetEventConsumer` is connected dynamically via `IBus.ConnectReceiveEndpoint` **after** the host starts, to avoid a second `AddMassTransit` call replacing the bus config.
- Test collection: `[Collection(nameof(JobIntegrationFixtureCollection))]` — all integration test classes must use this.

## Template Placeholders (do not leave in production)

- `ProcessAssetCommandHandler` throws `NotImplementedException` — replace with real logic.
- `docker/Dockerfile` is 0 bytes.
- `{API_NAME}` appears in `src/App/Configurations/Extensions/ScalarExtension.cs` and README — replace when cloning.
- Dashboard credentials default to `admin`/`admin` in `appsettings.json`.

## NuGet Sources

`Hangfire.Pro` and `Hangfire.Pro.Redis` are commercial packages — a valid Hangfire Pro license (and optionally Ace for `Hangfire.Throttling`) is required. Ensure the private feed is configured in `NuGet.Config` or environment before restoring.
