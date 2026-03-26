using Application.Features.Assets.Jobs;
using Domain.Contracts.Helpers;
using Domain.Models.AssetAggregate.Jobs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Application.Tests.Features.Assets.Jobs;

public class ProcessAssetJobTests
{
    private readonly IJobHelper _jobHelper = Substitute.For<IJobHelper>();
    private readonly ILogger<ProcessAssetJob> _logger = Substitute.For<ILogger<ProcessAssetJob>>();

    public static TheoryData<ProcessAssetDataJobDto> ValidJobData => new()
    {
        new ProcessAssetDataJobDto
            { AssetId = Guid.NewGuid(), Code = "ASSET-001", Name = "Test Asset", Value = 100.50m },
        new ProcessAssetDataJobDto
            { AssetId = Guid.NewGuid(), Code = "ASSET-002", Name = "Another Asset", Value = 250m },
        new ProcessAssetDataJobDto { AssetId = Guid.NewGuid(), Code = "ASSET-003", Name = "Third Asset", Value = 0.01m }
    };

    public static TheoryData<ProcessAssetDataJobDto> CancellableJobData => new()
    {
        new ProcessAssetDataJobDto
            { AssetId = Guid.NewGuid(), Code = "ASSET-CANCEL-01", Name = "Cancelled Asset", Value = 50m },
        new ProcessAssetDataJobDto
            { AssetId = Guid.NewGuid(), Code = "ASSET-CANCEL-02", Name = "Another Cancelled", Value = 75m }
    };

    public static TheoryData<ProcessAssetDataJobDto, ProcessAssetDataJobDto> MultipleJobDataPairs => new()
    {
        {
            new ProcessAssetDataJobDto { AssetId = Guid.NewGuid(), Code = "ASSET-A", Name = "First Asset", Value = 1m },
            new ProcessAssetDataJobDto { AssetId = Guid.NewGuid(), Code = "ASSET-B", Name = "Second Asset", Value = 2m }
        },
        {
            new ProcessAssetDataJobDto
                { AssetId = Guid.NewGuid(), Code = "ASSET-C", Name = "Third Asset", Value = 100m },
            new ProcessAssetDataJobDto
                { AssetId = Guid.NewGuid(), Code = "ASSET-D", Name = "Fourth Asset", Value = 200m }
        }
    };

    private ProcessAssetJob CreateSut()
    {
        return new ProcessAssetJob(_jobHelper, _logger);
    }

    [Theory]
    [MemberData(nameof(ValidJobData))]
    public async Task ExecuteAsync_WithValidJobData_ShouldCompleteSuccessfully(ProcessAssetDataJobDto jobData)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.ExecuteAsync(jobData, null, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();

        _jobHelper.Received(1).Start(null);
        _jobHelper.Received(1).Finish(null);
        _jobHelper.Received(1).Finally(null);
        _jobHelper.DidNotReceive().Error(Arg.Any<object?>(), Arg.Any<Exception?>());
        _jobHelper.DidNotReceive().Error(Arg.Any<object?>(), Arg.Any<string>());
    }

    [Theory]
    [MemberData(nameof(ValidJobData))]
    public async Task ExecuteAsync_WithValidJobData_ShouldCallInfoWithAssetDetails(ProcessAssetDataJobDto jobData)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.ExecuteAsync(jobData, null, CancellationToken.None);

        // Assert — Info is called twice: once with "Processing asset {id} - {name}" and once with "Asset {id} processed successfully".
        _jobHelper.Received(2).Info(Arg.Any<object?>(), Arg.Is<string>(s => s.Contains(jobData.AssetId.ToString())));
        _jobHelper.Received(1).Info(Arg.Any<object?>(), Arg.Is<string>(s => s.Contains(jobData.Name)));
    }

    [Theory]
    [MemberData(nameof(CancellableJobData))]
    public async Task ExecuteAsync_WhenCancelled_ShouldThrowOperationCanceledException(ProcessAssetDataJobDto jobData)
    {
        // Arrange
        var sut = CreateSut();

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = () => sut.ExecuteAsync(jobData, null, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        _jobHelper.Received(1).Start(null);
        _jobHelper.Received(1).Error(null, "Job was cancelled");
        _jobHelper.Received(1).Finally(null);
        _jobHelper.DidNotReceive().Finish(Arg.Any<object?>());
    }

    [Theory]
    [MemberData(nameof(ValidJobData))]
    public async Task ExecuteAsync_ShouldFollowLifecycleOrder_StartThenRunThenFinally(ProcessAssetDataJobDto jobData)
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
        await sut.ExecuteAsync(jobData, null, CancellationToken.None);

        // Assert
        callOrder.Should().StartWith("Start");
        callOrder.Should().EndWith("Finally");
        callOrder.IndexOf("Finish").Should().BeGreaterThan(callOrder.IndexOf("Start"));
        callOrder.IndexOf("Finally").Should().BeGreaterThan(callOrder.IndexOf("Finish"));
    }

    [Theory]
    [MemberData(nameof(MultipleJobDataPairs))]
    public async Task ExecuteAsync_WithMultipleJobData_ShouldProcessEachIndependently(
        ProcessAssetDataJobDto jobData1, ProcessAssetDataJobDto jobData2)
    {
        // Act & Assert — each call creates a new SUT (simulating Hangfire's per-execution instantiation)
        var sut1 = CreateSut();
        await sut1.ExecuteAsync(jobData1, null, CancellationToken.None);

        var sut2 = CreateSut();
        await sut2.ExecuteAsync(jobData2, null, CancellationToken.None);

        // Both should have gone through full lifecycle
        _jobHelper.Received(2).Start(null);
        _jobHelper.Received(2).Finish(null);
        _jobHelper.Received(2).Finally(null);
    }
}