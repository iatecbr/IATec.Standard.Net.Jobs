using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Hangfire;
using Integration.Tests.Configurations;
using StackExchange.Redis;

namespace Integration.Tests.Tests.Jobs;

[Collection(nameof(JobIntegrationFixtureCollection))]
public class ProcessAssetBatchJobTest(InfraIntegrationTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _client = fixture.CreateClient();
    private readonly IConnectionMultiplexer _redisConnection = fixture.Connection;

    [Theory(DisplayName = "Submit batch of ProcessAssetCommands via API and verify all jobs complete")]
    [Trait("Category", "Integration Test - Jobs")]
    [MemberData(nameof(JobIntegrationFixture.BatchSizes), MemberType = typeof(JobIntegrationFixture))]
    public async Task ProcessAssetBatchJob_SubmitViaApi_AllJobsComplete(int batchSize)
    {
        // Arrange
        var commands = Enumerable.Range(1, batchSize)
            .Select(i => new
            {
                AssetId = Guid.NewGuid(),
                Code = $"BAT-{i:D4}",
                Name = $"Batch Integration Asset {i}",
                Value = i * 10.0m
            })
            .ToArray();

        // Act — submit batch via v2 API
        var response = await _client.PostAsJsonAsync(
            "/api/v2/Jobs/assets/process-batch", commands);

        // Assert — HTTP 202 Accepted
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var orchestratorJobId = body.GetProperty("jobId").GetString();
        Assert.False(string.IsNullOrEmpty(orchestratorJobId));

        // Wait for the orchestrator job (ProcessAssetBatchCommand) to complete
        var orchestratorSucceeded = await WaitForJobState(
            orchestratorJobId, "Succeeded", TimeSpan.FromSeconds(30));
        Assert.True(orchestratorSucceeded,
            $"Orchestrator job {orchestratorJobId} did not reach Succeeded state within timeout");

        // Wait for batch progress completion (child jobs report progress via IncrementBatchProgress)
        var batchCompleted = await WaitForBatchProgressCompletion(
            _redisConnection, batchSize, TimeSpan.FromSeconds(60));
        Assert.True(batchCompleted,
            $"Batch with {batchSize} jobs did not complete within timeout");
    }

    private static async Task<bool> WaitForJobState(string? jobId, string expectedState, TimeSpan timeout)
    {
        if (string.IsNullOrEmpty(jobId))
            return false;

        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            using var connection = JobStorage.Current.GetConnection();
            var jobData = connection.GetJobData(jobId);

            if (jobData?.State == expectedState)
                return true;

            await Task.Delay(250);
        }

        return false;
    }

    private static async Task<bool> WaitForBatchProgressCompletion(
        IConnectionMultiplexer redis, int expectedTotal, TimeSpan timeout)
    {
        var database = redis.GetDatabase(1);
        var server = redis.GetServers()[0];
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            await foreach (var key in server.KeysAsync(1, "hangfire:batch:progress:*"))
            {
                var hash = await database.HashGetAllAsync(key);
                if (hash.Length == 0) continue;

                var entries = hash.ToDictionary(
                    e => e.Name.ToString(),
                    e => e.Value.ToString());

                var total = int.Parse(entries.GetValueOrDefault("Total") ?? "0");
                var completed = int.Parse(entries.GetValueOrDefault("Completed") ?? "0");
                var failed = int.Parse(entries.GetValueOrDefault("Failed") ?? "0");

                if (total == expectedTotal && completed + failed >= total)
                    return true;
            }

            await Task.Delay(500);
        }

        return false;
    }
}
