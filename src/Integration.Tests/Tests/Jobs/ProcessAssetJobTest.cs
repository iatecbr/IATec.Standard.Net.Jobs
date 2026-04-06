using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Hangfire;
using Integration.Tests.Configurations;

namespace Integration.Tests.Tests.Jobs;

[Collection(nameof(JobIntegrationFixtureCollection))]
public class ProcessAssetJobTest(InfraIntegrationTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _client = fixture.CreateClient();

    [Theory(DisplayName = "Submit single ProcessAssetCommand via API and verify job completes")]
    [Trait("Category", "Integration Test - Jobs")]
    [MemberData(nameof(JobIntegrationFixture.SingleAssetCommands), MemberType = typeof(JobIntegrationFixture))]
    public async Task ProcessAssetJob_SubmitViaApi_CompletesSuccessfully(
        Guid assetId, string code, string name, decimal value)
    {
        // Arrange
        var command = new { AssetId = assetId, Code = code, Name = name, Value = value };

        // Act — submit job via v2 API
        var response = await _client.PostAsJsonAsync(
            "/api/v2/Jobs/assets/process", command);

        // Assert — HTTP 202 Accepted
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var jobId = body.GetProperty("jobId").GetString();
        Assert.False(string.IsNullOrEmpty(jobId));

        // Wait for the Hangfire job to complete (polling)
        var succeeded = await WaitForJobState(jobId, "Succeeded", TimeSpan.FromSeconds(30));
        Assert.True(succeeded, $"Job {jobId} did not reach Succeeded state within timeout");
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
}
