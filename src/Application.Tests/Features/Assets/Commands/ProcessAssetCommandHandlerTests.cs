using Application.Features.Assets.Commands;
using Domain.Contracts.Helpers;
using HangFire.Jobs.Contracts;
using NSubstitute;
using Xunit;

namespace Application.Tests.Features.Assets.Commands;

public class ProcessAssetCommandHandlerTests
{
    private readonly IJobHelper _jobHelper = Substitute.For<IJobHelper>();
    private readonly IPerformContextAccessor _performContextAccessor = Substitute.For<IPerformContextAccessor>();

    public static TheoryData<ProcessAssetCommand> ValidCommands => new()
    {
        new ProcessAssetCommand
            { AssetId = Guid.NewGuid(), Code = "ASSET-001", Name = "Test Asset", Value = 100.50m },
        new ProcessAssetCommand
            { AssetId = Guid.NewGuid(), Code = "ASSET-002", Name = "Another Asset", Value = 250m },
        new ProcessAssetCommand
            { AssetId = Guid.NewGuid(), Code = "ASSET-003", Name = "Third Asset", Value = 0.01m }
    };

    public static TheoryData<ProcessAssetCommand> CancellableCommands => new()
    {
        new ProcessAssetCommand
            { AssetId = Guid.NewGuid(), Code = "ASSET-CANCEL-01", Name = "Cancelled Asset", Value = 50m },
        new ProcessAssetCommand
            { AssetId = Guid.NewGuid(), Code = "ASSET-CANCEL-02", Name = "Another Cancelled", Value = 75m }
    };

    public static TheoryData<ProcessAssetCommand> BatchCommands => new()
    {
        new ProcessAssetCommand
        {
            AssetId = Guid.NewGuid(), Code = "ASSET-BATCH-01", Name = "Batch Asset",
            Value = 30m, BatchKeyValue = "batch:progress:test-key"
        },
        new ProcessAssetCommand
        {
            AssetId = Guid.NewGuid(), Code = "ASSET-BATCH-02", Name = "Another Batch Asset",
            Value = 60m, BatchKeyValue = "batch:progress:other-key"
        }
    };

    public static TheoryData<ProcessAssetCommand, ProcessAssetCommand> MultipleCommandPairs => new()
    {
        {
            new ProcessAssetCommand { AssetId = Guid.NewGuid(), Code = "ASSET-A", Name = "First Asset", Value = 1m },
            new ProcessAssetCommand { AssetId = Guid.NewGuid(), Code = "ASSET-B", Name = "Second Asset", Value = 2m }
        },
        {
            new ProcessAssetCommand
                { AssetId = Guid.NewGuid(), Code = "ASSET-C", Name = "Third Asset", Value = 100m },
            new ProcessAssetCommand
                { AssetId = Guid.NewGuid(), Code = "ASSET-D", Name = "Fourth Asset", Value = 200m }
        }
    };

    private ProcessAssetCommandHandler CreateSut()
    {
        return new ProcessAssetCommandHandler(_jobHelper, _performContextAccessor);
    }

    // ──────────────────────────────────────────────
    //  Handle() — MediatR handler (BaseCommand lifecycle)
    // ──────────────────────────────────────────────

    [Theory(DisplayName = "Handle with valid command should complete successfully")]
    [Trait("Category", "ProcessAssetCommand")]
    [MemberData(nameof(ValidCommands))]
    public async Task Handle_WithValidCommand_ShouldCompleteSuccessfully(ProcessAssetCommand command)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _jobHelper.Received(1).Start(null);
        _jobHelper.Received(1).Finish(null);
        _jobHelper.Received(1).Finally(null);
        _jobHelper.DidNotReceive().Error(Arg.Any<object?>(), Arg.Any<Exception?>());
        _jobHelper.DidNotReceive().Error(Arg.Any<object?>(), Arg.Any<string>());
    }

    [Theory(DisplayName = "Handle when cancelled should throw OperationCanceledException")]
    [Trait("Category", "ProcessAssetCommand")]
    [MemberData(nameof(CancellableCommands))]
    public async Task Handle_WhenCancelled_ShouldThrowOperationCanceledException(ProcessAssetCommand command)
    {
        // Arrange
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.Handle(command, cts.Token));

        _jobHelper.Received(1).Start(null);
        _jobHelper.Received(1).Error(null, "Command was cancelled");
        _jobHelper.Received(1).Finally(null);
        _jobHelper.DidNotReceive().Finish(Arg.Any<object?>());
    }

    [Theory(DisplayName = "Handle should follow lifecycle order: Start then Run then Finally")]
    [Trait("Category", "ProcessAssetCommand")]
    [MemberData(nameof(ValidCommands))]
    public async Task Handle_ShouldFollowLifecycleOrder_StartThenRunThenFinally(ProcessAssetCommand command)
    {
        // Arrange
        var sut = CreateSut();

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
        await sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("Start", callOrder[0]);
        Assert.Equal("Finally", callOrder[^1]);
        Assert.True(callOrder.IndexOf("Finish") > callOrder.IndexOf("Start"));
        Assert.True(callOrder.IndexOf("Finally") > callOrder.IndexOf("Finish"));
    }

    [Theory(DisplayName = "Handle with multiple commands should process each independently")]
    [Trait("Category", "ProcessAssetCommand")]
    [MemberData(nameof(MultipleCommandPairs))]
    public async Task Handle_WithMultipleCommands_ShouldProcessEachIndependently(
        ProcessAssetCommand command1, ProcessAssetCommand command2)
    {
        // Arrange & Act — each call creates a new SUT (simulating Hangfire's per-execution instantiation)
        var sut1 = CreateSut();
        await sut1.Handle(command1, CancellationToken.None);

        var sut2 = CreateSut();
        await sut2.Handle(command2, CancellationToken.None);

        // Assert — both should have gone through full lifecycle
        _jobHelper.Received(2).Start(null);
        _jobHelper.Received(2).Finish(null);
        _jobHelper.Received(2).Finally(null);
    }

    [Theory(DisplayName = "Handle should notify info with asset code")]
    [Trait("Category", "ProcessAssetCommand")]
    [MemberData(nameof(ValidCommands))]
    public async Task Handle_ShouldNotifyInfoWithAssetCode(ProcessAssetCommand command)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — should log with asset code
        _jobHelper.Received(1).Info(Arg.Any<object?>(),
            Arg.Is<string>(s => s.Contains(command.Code) && s.Contains("Processing")));
        _jobHelper.Received(1).Info(Arg.Any<object?>(),
            Arg.Is<string>(s => s.Contains(command.Code) && s.Contains("processed successfully")));
    }

    [Theory(DisplayName = "Handle with batch key should increment batch progress")]
    [Trait("Category", "ProcessAssetCommand")]
    [MemberData(nameof(BatchCommands))]
    public async Task Handle_WithBatchKey_ShouldIncrementBatchProgress(ProcessAssetCommand command)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — should report progress to batch tracker
        _jobHelper.Received(1).IncrementBatchProgress(
            Arg.Any<Domain.Models.JobAggregate.BatchKey>(),
            Arg.Any<object?>(),
            false);
    }

    [Theory(DisplayName = "Handle without batch key should not increment batch progress")]
    [Trait("Category", "ProcessAssetCommand")]
    [MemberData(nameof(ValidCommands))]
    public async Task Handle_WithoutBatchKey_ShouldNotIncrementBatchProgress(ProcessAssetCommand command)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — no batch key means no progress tracking
        _jobHelper.DidNotReceive().IncrementBatchProgress(
            Arg.Any<Domain.Models.JobAggregate.BatchKey>(),
            Arg.Any<object?>(),
            Arg.Any<bool>());
    }
}
