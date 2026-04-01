using Application.Features.Assets.Commands;
using Domain.Contracts.Helpers;
using Domain.Contracts.Services;
using Domain.Models.JobAggregate;
using HangFire.Jobs.Contracts;
using NSubstitute;
using Xunit;

namespace Application.Tests.Features.Assets.Commands;

public class ProcessAssetBatchCommandHandlerTests
{
    private readonly IBatchJobService _batchJobService = Substitute.For<IBatchJobService>();
    private readonly IJobHelper _jobHelper = Substitute.For<IJobHelper>();
    private readonly IPerformContextAccessor _performContextAccessor = Substitute.For<IPerformContextAccessor>();

    public static TheoryData<int> ValidAssetCounts => [1, 3, 5, 10];

    public static TheoryData<int, string> AssetCountsWithBatchIds => new()
    {
        { 5, "batch-xyz-789" },
        { 10, "batch-abc-456" },
        { 1, "batch-single-001" }
    };

    public static TheoryData<int> BatchNameAssetCounts => [3, 7, 15];

    private ProcessAssetBatchCommandHandler CreateSut()
    {
        return new ProcessAssetBatchCommandHandler(
            _jobHelper, _performContextAccessor, _batchJobService);
    }

    private static ProcessAssetCommand[] CreateCommands(int count)
    {
        return Enumerable.Range(1, count).Select(i => new ProcessAssetCommand
        {
            AssetId = Guid.NewGuid(),
            Code = $"ASSET-{i:D4}",
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

    [Theory(DisplayName = "Handle with valid commands should call StartMonitoredBatch")]
    [Trait("Category", "ProcessAssetBatchCommand")]
    [MemberData(nameof(ValidAssetCounts))]
    public async Task Handle_WithValidCommands_ShouldCallStartMonitoredBatch(int assetCount)
    {
        // Arrange
        var sut = CreateSut();
        var commands = CreateCommands(assetCount);
        var batchCommand = new ProcessAssetBatchCommand { Commands = commands };
        SetupBatchServiceReturns();

        // Act
        var result = await sut.Handle(batchCommand, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _batchJobService.Received(1).StartMonitoredBatch(
            Arg.Any<string>(),
            Arg.Any<Action<object, string>>());

        _jobHelper.Received(1).Start(null);
        _jobHelper.Received(1).Finish(null);
        _jobHelper.Received(1).Finally(null);
        _jobHelper.DidNotReceive().Error(Arg.Any<object?>(), Arg.Any<Exception?>());
        _jobHelper.DidNotReceive().Error(Arg.Any<object?>(), Arg.Any<string>());
    }

    [Theory(DisplayName = "Handle with empty commands should return fail result")]
    [Trait("Category", "ProcessAssetBatchCommand")]
    [InlineData(0)]
    public async Task Handle_WithEmptyCommands_ShouldReturnFailResult(int assetCount)
    {
        // Arrange
        var sut = CreateSut();
        var commands = CreateCommands(assetCount);
        var batchCommand = new ProcessAssetBatchCommand { Commands = commands };

        // Act
        var result = await sut.Handle(batchCommand, CancellationToken.None);

        // Assert — NotifyErrorAndStop calls Error, and BaseCommand sees the failed Result and calls Error again
        Assert.True(result.IsFailed);
        _jobHelper.Received().Error(Arg.Any<object?>(), Arg.Is<string>(s => s.Contains("No assets provided")));

        // Should NOT call StartMonitoredBatch since there are no commands
        _batchJobService.DidNotReceive().StartMonitoredBatch(
            Arg.Any<string>(),
            Arg.Any<Action<object, string>>());

        // Finish should NOT be called (failed result path)
        _jobHelper.DidNotReceive().Finish(Arg.Any<object?>());

        // Finally is always called
        _jobHelper.Received(1).Finally(null);
    }

    [Theory(DisplayName = "Handle with valid commands should log batch info")]
    [Trait("Category", "ProcessAssetBatchCommand")]
    [MemberData(nameof(AssetCountsWithBatchIds))]
    public async Task Handle_WithValidCommands_ShouldLogBatchInfo(int assetCount, string batchId)
    {
        // Arrange
        var sut = CreateSut();
        var commands = CreateCommands(assetCount);
        var batchCommand = new ProcessAssetBatchCommand { Commands = commands };
        SetupBatchServiceReturns(batchId);

        // Act
        await sut.Handle(batchCommand, CancellationToken.None);

        // Assert — NotifyInfo is called with batch creation message and success message
        _jobHelper.Received(1).Info(Arg.Any<object?>(),
            Arg.Is<string>(s => s.Contains(assetCount.ToString()) && s.Contains("assets")));
        _jobHelper.Received(1).Info(Arg.Any<object?>(),
            Arg.Is<string>(s => s.Contains(batchId) && s.Contains(assetCount.ToString())));
    }

    [Theory(DisplayName = "Handle should follow lifecycle order")]
    [Trait("Category", "ProcessAssetBatchCommand")]
    [MemberData(nameof(ValidAssetCounts))]
    public async Task Handle_ShouldFollowLifecycleOrder(int assetCount)
    {
        // Arrange
        var sut = CreateSut();
        var commands = CreateCommands(assetCount);
        var batchCommand = new ProcessAssetBatchCommand { Commands = commands };
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
        await sut.Handle(batchCommand, CancellationToken.None);

        // Assert
        Assert.Equal("Start", callOrder[0]);
        Assert.Equal("Finally", callOrder[^1]);
        Assert.True(callOrder.IndexOf("Finish") > callOrder.IndexOf("Start"));
        Assert.True(callOrder.IndexOf("Finally") > callOrder.IndexOf("Finish"));
    }

    [Theory(DisplayName = "Handle with multiple commands should pass correct batch name")]
    [Trait("Category", "ProcessAssetBatchCommand")]
    [MemberData(nameof(BatchNameAssetCounts))]
    public async Task Handle_WithMultipleCommands_ShouldPassCorrectBatchName(int assetCount)
    {
        // Arrange
        var sut = CreateSut();
        var commands = CreateCommands(assetCount);
        var batchCommand = new ProcessAssetBatchCommand { Commands = commands };
        SetupBatchServiceReturns();

        // Act
        await sut.Handle(batchCommand, CancellationToken.None);

        // Assert — batch name should include the asset count: "Process N Assets"
        _batchJobService.Received(1).StartMonitoredBatch(
            Arg.Is<string>(name => name.Contains(assetCount.ToString()) && name.Contains("Assets")),
            Arg.Any<Action<object, string>>());
    }
}
