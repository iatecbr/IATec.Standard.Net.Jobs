using Application.Features.Assets.Jobs;
using Domain.Contracts.Helpers;
using Domain.Contracts.Services;
using Domain.Models.AssetAggregate.Jobs;
using Domain.Models.JobAggregate;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Application.Tests.Features.Assets.Jobs;

public class ProcessAssetBatchJobTests
{
    private readonly IBatchJobService _batchJobService = Substitute.For<IBatchJobService>();
    private readonly IJobHelper _jobHelper = Substitute.For<IJobHelper>();
    private readonly ILogger<ProcessAssetBatchJob> _logger = Substitute.For<ILogger<ProcessAssetBatchJob>>();

    public static TheoryData<int> ValidAssetCounts => new() { 1, 3, 5, 10 };

    public static TheoryData<int, string> AssetCountsWithBatchIds => new()
    {
        { 5, "batch-xyz-789" },
        { 10, "batch-abc-456" },
        { 1, "batch-single-001" }
    };

    public static TheoryData<int> BatchNameAssetCounts => new() { 3, 7, 15 };

    private ProcessAssetBatchJob CreateSut()
    {
        return new ProcessAssetBatchJob(_jobHelper, _logger, _batchJobService);
    }

    private static ProcessAssetDataJobDto[] CreateAssets(int count)
    {
        return Enumerable.Range(1, count).Select(i => new ProcessAssetDataJobDto
        {
            AssetId = Guid.NewGuid(),
            Code = $"ASSET-{i:D3}",
            Name = $"Asset {i}",
            Value = i * 10m
        }).ToArray();
    }

    private void SetupBatchServiceReturns(string batchId = "batch-123", string batchName = "Test Batch",
        string batchKeyValue = "batch:progress:test-key")
    {
        _batchJobService.StartMonitoredBatch(
            Arg.Any<string>(),
            Arg.Any<Action<object, string>>()
        ).Returns(new BatchInfo
        {
            BatchId = batchId,
            BatchName = batchName,
            BatchKeyValue = batchKeyValue,
            CreatedAt = DateTime.UtcNow
        });
    }

    [Theory]
    [MemberData(nameof(ValidAssetCounts))]
    public async Task ExecuteAsync_WithValidAssets_ShouldCallStartMonitoredBatch(int assetCount)
    {
        // Arrange
        var sut = CreateSut();
        var assets = CreateAssets(assetCount);
        SetupBatchServiceReturns();

        // Act
        await sut.ExecuteAsync(assets, null, CancellationToken.None);

        // Assert
        _batchJobService.Received(1).StartMonitoredBatch(
            Arg.Any<string>(),
            Arg.Any<Action<object, string>>());

        _jobHelper.Received(1).Start(null);
        _jobHelper.Received(1).Finish(null);
        _jobHelper.Received(1).Finally(null);
        _jobHelper.DidNotReceive().Error(Arg.Any<object?>(), Arg.Any<Exception?>());
        _jobHelper.DidNotReceive().Error(Arg.Any<object?>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyAssets_ShouldReturnFailResult()
    {
        // Arrange
        var sut = CreateSut();
        var assets = Array.Empty<ProcessAssetDataJobDto>();

        // Act
        await sut.ExecuteAsync(assets, null, CancellationToken.None);

        // Assert — NotifyErrorAndStop calls Error, and BaseJob sees the failed Result and calls Error again
        _jobHelper.Received().Error(Arg.Any<object?>(), Arg.Is<string>(s => s.Contains("No assets provided")));

        // Should NOT call StartMonitoredBatch since there are no assets
        _batchJobService.DidNotReceive().StartMonitoredBatch(
            Arg.Any<string>(),
            Arg.Any<Action<object, string>>());

        // Finish should NOT be called (failed result path)
        _jobHelper.DidNotReceive().Finish(Arg.Any<object?>());

        // Finally is always called
        _jobHelper.Received(1).Finally(null);
    }

    [Theory]
    [MemberData(nameof(AssetCountsWithBatchIds))]
    public async Task ExecuteAsync_WithValidAssets_ShouldLogBatchInfo(int assetCount, string batchId)
    {
        // Arrange
        var sut = CreateSut();
        var assets = CreateAssets(assetCount);
        SetupBatchServiceReturns(batchId);

        // Act
        await sut.ExecuteAsync(assets, null, CancellationToken.None);

        // Assert — NotifyInfo is called twice:
        // 1. "Creating monitored batch for N assets..."
        // 2. "Batch created successfully | BatchId: ... | Total jobs: N"
        _jobHelper.Received(1).Info(Arg.Any<object?>(),
            Arg.Is<string>(s => s.Contains(assetCount.ToString()) && s.Contains("assets")));
        _jobHelper.Received(1).Info(Arg.Any<object?>(),
            Arg.Is<string>(s => s.Contains(batchId) && s.Contains(assetCount.ToString())));
    }

    [Theory]
    [MemberData(nameof(ValidAssetCounts))]
    public async Task ExecuteAsync_ShouldFollowLifecycleOrder(int assetCount)
    {
        // Arrange
        var sut = CreateSut();
        var assets = CreateAssets(assetCount);
        SetupBatchServiceReturns();

        var callOrder = new List<string>();

        _jobHelper.When(x => x.Start(Arg.Any<object?>()))
            .Do(_ => callOrder.Add("Start"));
        _jobHelper.When(x => x.Info(Arg.Any<object?>(), Arg.Any<string>()))
            .Do(_ => callOrder.Add("Info"));
        _jobHelper.When(x => x.Finish(Arg.Any<object?>()))
            .Do(_ => callOrder.Add("Finish"));
        _jobHelper.When(x => x.Finally(Arg.Any<object?>()))
            .Do(_ => callOrder.Add("Finally"));

        // Act
        await sut.ExecuteAsync(assets, null, CancellationToken.None);

        // Assert
        callOrder.Should().StartWith("Start");
        callOrder.Should().EndWith("Finally");
        callOrder.IndexOf("Finish").Should().BeGreaterThan(callOrder.IndexOf("Start"));
        callOrder.IndexOf("Finally").Should().BeGreaterThan(callOrder.IndexOf("Finish"));
    }

    [Theory]
    [MemberData(nameof(BatchNameAssetCounts))]
    public async Task ExecuteAsync_WithMultipleAssets_ShouldPassCorrectBatchName(int assetCount)
    {
        // Arrange
        var sut = CreateSut();
        var assets = CreateAssets(assetCount);
        SetupBatchServiceReturns();

        // Act
        await sut.ExecuteAsync(assets, null, CancellationToken.None);

        // Assert — batch name should include the asset count: "Process N Assets"
        _batchJobService.Received(1).StartMonitoredBatch(
            Arg.Is<string>(name => name.Contains(assetCount.ToString()) && name.Contains("Assets")),
            Arg.Any<Action<object, string>>());
    }
}