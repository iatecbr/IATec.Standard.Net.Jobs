using Application.Features.Assets.Commands;
using Asp.Versioning;
using Domain.Contracts.Helpers;
using Domain.Contracts.Services;
using Domain.Helpers;
using Hangfire;
using HangFire.Jobs.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;

/// <summary>
///     Controller showcasing all Hangfire job and batch monitoring possibilities,
///     along with API versioning examples (v1 deprecated, v2 current).
///     Versioning strategy:
///     - v1: Deprecated — endpoints still work but will be removed in future versions.
///     - v2: Current stable — preferred version for new integrations.
///     Monitoring capabilities:
///     1. Single job (fire-and-forget) via EnqueueCommand — POST assets/process
///     2. Batch via EnqueueCommand with ProcessAssetBatchCommand — POST assets/process-batch
///     3. Batch monitoring — GET batch/{batchId}/monitor
///     4. Batch cancellation — POST batch/{batchId}/cancel
///     5. Custom batch progress tracking — GET batch/{batchKey}/progress
/// </summary>
[ApiVersion(1.0, Deprecated = true)]
[ApiVersion(2.0)]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class JobsController(
    IBackgroundJobClient backgroundJobClient,
    IBatchJobService batchJobService,
    IJobHelper jobHelper) : ControllerBase
{
    // ──────────────────────────────────────────────
    //  v1 — Deprecated endpoints
    // ──────────────────────────────────────────────

    /// <summary>
    ///     [DEPRECATED] Submits a single asset processing job to Hangfire (fire-and-forget).
    ///     Uses <c>EnqueueCommand</c> to enqueue a <see cref="ProcessAssetCommand" />
    ///     as a Hangfire job via <c>ISender.Send(command)</c>.
    ///     Note: validation runs inside the Hangfire worker (MediatR pipeline), NOT during
    ///     the HTTP request. Invalid commands will fail the job, not return 400.
    ///     Use v2 endpoint instead for synchronous validation before enqueue.
    /// </summary>
    [MapToApiVersion(1.0)]
    [HttpPost("assets/process")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public IActionResult SubmitProcessAssetJobV1(
        [FromBody] ProcessAssetCommand command)
    {
        var jobId = backgroundJobClient.EnqueueCommand(command);

        return Accepted(new { Message = "Job submitted successfully", JobId = jobId, command.AssetId });
    }

    /// <summary>
    ///     [DEPRECATED] Submits a batch of asset processing jobs.
    ///     Use v2 endpoint instead.
    /// </summary>
    [MapToApiVersion(1.0)]
    [HttpPost("assets/process-batch")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult SubmitProcessAssetBatchV1([FromBody] ProcessAssetCommand[] commands)
    {
        if (commands.Length == 0)
            return BadRequest(new { Message = "At least one command is required" });

        var jobId = backgroundJobClient.EnqueueCommand(new ProcessAssetBatchCommand(commands));

        return Accepted(new
        {
            Message = $"Batch job submitted for {commands.Length} assets",
            JobId = jobId,
            TotalAssets = commands.Length
        });
    }

    // ──────────────────────────────────────────────
    //  v2 — Current stable endpoints
    // ──────────────────────────────────────────────

    /// <summary>
    ///     Submits a single asset processing job to Hangfire (fire-and-forget).
    ///     Uses <c>BackgroundJobClientExtensions.EnqueueCommand</c> to enqueue
    ///     a <see cref="ProcessAssetCommand" /> as a Hangfire job via <c>ISender.Send(command)</c>.
    ///     The <c>[Queue]</c> attribute on <see cref="ProcessAssetCommand" /> is read by reflection
    ///     and passed to Hangfire's <c>Enqueue(queue, ...)</c> overload, setting <c>Job.Queue</c> natively.
    ///     The actual business logic runs later inside the Hangfire worker when
    ///     <see cref="ProcessAssetCommandHandler" /> handles the command via MediatR.
    ///     Note: validation runs inside the Hangfire worker (MediatR pipeline), NOT during
    ///     the HTTP request. Invalid commands will fail the job, not return 400.
    /// </summary>
    [MapToApiVersion(2.0)]
    [HttpPost("assets/process")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public IActionResult SubmitProcessAssetJob(
        [FromBody] ProcessAssetCommand command)
    {
        var jobId = backgroundJobClient.EnqueueCommand(command);

        return Accepted(new { Message = "Job submitted successfully", JobId = jobId, command.AssetId });
    }

    /// <summary>
    ///     Submits a batch of asset processing jobs via a <see cref="ProcessAssetBatchCommand" />.
    ///     This enqueues a single command that internally creates a monitored Hangfire Pro batch.
    ///     Each asset becomes an individual <see cref="ProcessAssetCommand" /> inside the batch.
    ///     A <see cref="HangFire.Jobs.Commands.MonitorBatchCommand" /> is automatically enqueued by
    ///     the batch service for real-time progress tracking.
    ///     Monitoring chain:
    ///     - ProcessAssetBatchCommand → creates batch → enqueues N × ProcessAssetCommand → MonitorBatchCommand
    ///     - Query batch status via GET batch/{batchId}/monitor
    /// </summary>
    [MapToApiVersion(2.0)]
    [HttpPost("assets/process-batch")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult SubmitProcessAssetBatch([FromBody] IReadOnlyCollection<ProcessAssetCommand> commands)
    {
        if (commands.Count == 0)
            return BadRequest(new { Message = "At least one command is required" });

        var jobId = backgroundJobClient.EnqueueCommand(new ProcessAssetBatchCommand(commands));

        return Accepted(new
        {
            Message = $"Batch job submitted for {commands.Count} assets",
            JobId = jobId,
            TotalAssets = commands.Count,
            Note =
                "The batch will be created by ProcessAssetBatchCommandHandler. Monitor via GET batch/{{batchId}}/monitor after the job starts."
        });
    }

    // ──────────────────────────────────────────────
    //  v1 + v2 — Available in both versions
    // ──────────────────────────────────────────────

    /// <summary>
    ///     Gets the monitoring result for a batch, including:
    ///     - Status (Started, Processing, Completed, Cancelled)
    ///     - Timing (CreatedAt, CompletedAt, ElapsedTime with human-readable format)
    ///     - Progress (TotalJobs count)
    ///     - BatchName for identification
    /// </summary>
    [MapToApiVersion(1.0)]
    [MapToApiVersion(2.0)]
    [HttpGet("batch/{batchId}/monitor")]
    [ProducesResponseType(typeof(BatchMonitorResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetBatchMonitorResult(string batchId)
    {
        var result = batchJobService.GetBatchMonitorResult(batchId);

        if (result is null)
            return NotFound(new { Message = $"Batch {batchId} not found" });

        return Ok(result);
    }

    /// <summary>
    ///     Cancels a batch, preventing pending jobs from being executed.
    ///     Jobs that are already running will complete, but no new jobs will start.
    ///     The batch metadata status is updated to "Cancelled".
    /// </summary>
    [MapToApiVersion(1.0)]
    [MapToApiVersion(2.0)]
    [HttpPost("batch/{batchId}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult CancelBatch(string batchId)
    {
        batchJobService.CancelBatch(batchId);
        return Ok(new { Message = $"Batch {batchId} cancelled" });
    }

    /// <summary>
    ///     Gets the batch progress for a given batch key (custom granular tracking via Redis).
    ///     This uses IJobHelper.GetBatchProgress with a BatchKey for fine-grained progress monitoring.
    /// </summary>
    [MapToApiVersion(1.0)]
    [MapToApiVersion(2.0)]
    [HttpGet("batch/{batchKey}/progress")]
    [ProducesResponseType(typeof(BatchProgressInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetBatchProgress(string batchKey)
    {
        var progress = jobHelper.GetBatchProgress(BatchKey.FromJobId(batchKey));

        if (progress is null)
            return NotFound(new { Message = $"Batch {batchKey} not found" });

        return Ok(progress);
    }
}