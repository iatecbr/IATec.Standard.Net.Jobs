using Application.Jobs;
using Domain.Contracts.Helpers;
using FluentAssertions;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Application.Tests.Jobs;

public class BatchMonitorJobTests : IDisposable
{
    private const string DefaultBatchKeyValue = "batch:progress:test-key";
    private readonly IStorageConnection _connection = Substitute.For<IStorageConnection>();
    private readonly IJobHelper _jobHelper = Substitute.For<IJobHelper>();
    private readonly JobStorage _jobStorage = Substitute.For<JobStorage>();
    private readonly ILogger<BatchMonitorJob> _logger = Substitute.For<ILogger<BatchMonitorJob>>();

    public BatchMonitorJobTests()
    {
        // Set up the static JobStorage.Current so BatchMonitorJob can access it.
        JobStorage.Current = _jobStorage;
        _jobStorage.GetConnection().Returns(_connection);
    }

    public static TheoryData<string, string> BatchIdentifiers => new()
    {
        { "batch-001", "Test Batch" },
        { "batch-abc-xyz", "Import Users" },
        { "d1a43f5e-bd7d-421b-ba4c-4bfada60b336", "Process Assets" }
    };

    public static TheoryData<string, string, string, int> ValidMetadataScenarios => new()
    {
        { "batch-001", "Import Users", "batch:progress:import-users", 50 },
        { "batch-002", "Process Assets", "batch:progress:process-assets", 100 },
        { "batch-003", "Generate Reports", "batch:progress:reports", 10 }
    };

    public static TheoryData<string, string> StorageErrorScenarios => new()
    {
        { "batch-fail-001", "Failing Batch" },
        { "batch-fail-002", "Another Failing Batch" }
    };

    public static TheoryData<string, string> MetadataKeyScenarios => new()
    {
        { "batch-abc-123", "Key Test Batch" },
        { "batch-xyz-789", "Another Key Test" },
        { "simple-id", "Simple Batch" }
    };

    public void Dispose()
    {
        // NSubstitute mocks don't need explicit disposal, but we implement IDisposable
        // to signal that this test class modifies static state (JobStorage.Current).
        GC.SuppressFinalize(this);
    }

    private BatchMonitorJob CreateSut()
    {
        return new BatchMonitorJob(_jobHelper, _logger);
    }

    [Theory]
    [MemberData(nameof(BatchIdentifiers))]
    public async Task ExecuteAsync_WithNoMetadata_ShouldCompleteSuccessfullyAndWarn(string batchId, string batchName)
    {
        // Arrange
        var sut = CreateSut();

        _connection.GetAllEntriesFromHash(Arg.Any<string>())
            .Returns((Dictionary<string, string>?)null);

        // Act
        var act = () => sut.ExecuteAsync(batchId, batchName, DefaultBatchKeyValue, null, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();

        _jobHelper.Received(1).Start(null);
        _jobHelper.Received(1).Finish(null);
        _jobHelper.Received(1).Finally(null);
        _jobHelper.DidNotReceive().Error(Arg.Any<object?>(), Arg.Any<Exception?>());
    }

    [Theory]
    [MemberData(nameof(BatchIdentifiers))]
    public async Task ExecuteAsync_WithEmptyMetadata_ShouldCompleteSuccessfullyAndWarn(string batchId, string batchName)
    {
        // Arrange
        var sut = CreateSut();

        _connection.GetAllEntriesFromHash(Arg.Any<string>())
            .Returns(new Dictionary<string, string>());

        // Act
        await sut.ExecuteAsync(batchId, batchName, DefaultBatchKeyValue, null, CancellationToken.None);

        // Assert
        _jobHelper.Received(1).Start(null);
        _jobHelper.Received(1).Finish(null);
        _jobHelper.Received(1).Finally(null);
    }

    [Theory]
    [MemberData(nameof(ValidMetadataScenarios))]
    public async Task ExecuteAsync_WithValidMetadata_ShouldLogSummaryAndUpdateCompletion(
        string batchId, string batchName, string batchKeyValue, int totalJobs)
    {
        // Arrange
        var sut = CreateSut();
        var createdAt = DateTime.UtcNow.AddMinutes(-5);

        // Metadata hash (queried by metadataKey)
        _connection.GetAllEntriesFromHash($"batch:monitor:{batchId}")
            .Returns(new Dictionary<string, string>
            {
                { "CreatedAt", createdAt.ToString("O") },
                { "TotalJobs", totalJobs.ToString() },
                { "BatchName", batchName }
            });

        // Progress hash (queried by batchKeyValue) — all jobs completed
        _connection.GetAllEntriesFromHash(batchKeyValue)
            .Returns(new Dictionary<string, string>
            {
                { "Total", totalJobs.ToString() },
                { "Completed", totalJobs.ToString() },
                { "Failed", "0" }
            });

        // Act
        await sut.ExecuteAsync(batchId, batchName, batchKeyValue, null, CancellationToken.None);

        // Assert — lifecycle completed
        _jobHelper.Received(1).Start(null);
        _jobHelper.Received(1).Finish(null);
        _jobHelper.Received(1).Finally(null);

        // Assert — completion metadata was written back to Redis
        _connection.Received().SetRangeInHash(
            $"batch:monitor:{batchId}",
            Arg.Is<IEnumerable<KeyValuePair<string, string>>>(kvs =>
                kvs.Any(kv => kv.Key == "Status" && kv.Value == "Completed") &&
                kvs.Any(kv => kv.Key == "CompletedAt") &&
                kvs.Any(kv => kv.Key == "ElapsedMs")));
    }

    [Theory]
    [MemberData(nameof(StorageErrorScenarios))]
    public async Task ExecuteAsync_WhenStorageThrows_ShouldStillCompleteSuccessfully(string batchId, string batchName)
    {
        // Arrange
        var sut = CreateSut();

        _connection.GetAllEntriesFromHash(Arg.Any<string>())
            .Throws(new InvalidOperationException("Redis connection failed"));

        // Act — should NOT throw because BatchMonitorJob catches storage exceptions internally
        var act = () => sut.ExecuteAsync(batchId, batchName, DefaultBatchKeyValue, null, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();

        _jobHelper.Received(1).Start(null);
        _jobHelper.Received(1).Finally(null);
    }

    [Theory]
    [MemberData(nameof(BatchIdentifiers))]
    public async Task ExecuteAsync_WhenCancelled_ShouldCompleteGracefully(string batchId, string batchName)
    {
        // Arrange
        var sut = CreateSut();

        _connection.GetAllEntriesFromHash(Arg.Any<string>())
            .Returns((Dictionary<string, string>?)null);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act — With no metadata, RunAsync returns early (before the polling loop),
        // so cancellation doesn't affect the outcome.
        var act = () => sut.ExecuteAsync(batchId, batchName, DefaultBatchKeyValue, null, cts.Token);

        // Assert
        await act.Should().NotThrowAsync();

        _jobHelper.Received(1).Start(null);
        _jobHelper.Received(1).Finish(null);
        _jobHelper.Received(1).Finally(null);
    }

    [Theory]
    [MemberData(nameof(MetadataKeyScenarios))]
    public async Task ExecuteAsync_ShouldUseCorrectMetadataKey(string batchId, string batchName)
    {
        // Arrange
        var sut = CreateSut();
        var expectedKey = $"batch:monitor:{batchId}";

        _connection.GetAllEntriesFromHash(Arg.Any<string>())
            .Returns((Dictionary<string, string>?)null);

        // Act
        await sut.ExecuteAsync(batchId, batchName, DefaultBatchKeyValue, null, CancellationToken.None);

        // Assert — should query Redis with the correct key pattern
        _connection.Received(1).GetAllEntriesFromHash(expectedKey);
    }

    [Theory]
    [MemberData(nameof(BatchIdentifiers))]
    public async Task ExecuteAsync_ShouldFollowLifecycleOrder(string batchId, string batchName)
    {
        // Arrange
        var sut = CreateSut();
        var callOrder = new List<string>();

        _connection.GetAllEntriesFromHash(Arg.Any<string>())
            .Returns((Dictionary<string, string>?)null);

        _jobHelper.When(x => x.Start(Arg.Any<object?>()))
            .Do(_ => callOrder.Add("Start"));
        _jobHelper.When(x => x.Info(Arg.Any<object?>(), Arg.Any<string>()))
            .Do(_ => callOrder.Add("Info"));
        _jobHelper.When(x => x.Finish(Arg.Any<object?>()))
            .Do(_ => callOrder.Add("Finish"));
        _jobHelper.When(x => x.Finally(Arg.Any<object?>()))
            .Do(_ => callOrder.Add("Finally"));

        // Act
        await sut.ExecuteAsync(batchId, batchName, DefaultBatchKeyValue, null, CancellationToken.None);

        // Assert
        callOrder.Should().StartWith("Start");
        callOrder.Should().EndWith("Finally");
        callOrder.IndexOf("Finish").Should().BeGreaterThan(callOrder.IndexOf("Start"));
    }
}