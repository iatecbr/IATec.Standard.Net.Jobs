using Domain.Models.JobAggregate;
using Hangfire;
using Integration.Tests.Configurations;

namespace Integration.Tests.Tests.Redis;

[Collection(nameof(RedisBatchProgressFixtureCollection))]
public class RedisBatchProgressTest
{
    public RedisBatchProgressTest(
        RedisBatchProgressFixture redisBatchProgressFixture,
        InfraIntegrationTestFixture infraIntegrationTestFixture)
    {
        redisBatchProgressFixture.Connection = infraIntegrationTestFixture.Connection;
    }

    public static TheoryData<int> BatchSizes => [1, 5, 10, 50];

    public static TheoryData<int, int> CompletionScenarios => new()
    {
        { 10, 7 },
        { 5, 5 },
        { 20, 0 },
        { 3, 1 }
    };

    public static TheoryData<int, int, int> MixedProgressScenarios => new()
    {
        { 10, 6, 2 },
        { 5, 3, 1 },
        { 20, 10, 5 }
    };

    [Theory(DisplayName = "Initialize batch progress hash with correct total")]
    [Trait("Category", "Redis Integration Test - BatchProgress")]
    [MemberData(nameof(BatchSizes))]
    public void BatchProgress_Initialize_CreatesHashWithTotal(int totalItems)
    {
        // Arrange
        var batchKey = BatchKey.CreateNew();

        // Act
        using var connection = JobStorage.Current.GetConnection();
        connection.SetRangeInHash(batchKey.Value, new Dictionary<string, string>
        {
            { "Total", totalItems.ToString() },
            { "Completed", "0" },
            { "Failed", "0" },
            { "StartedAt", DateTime.UtcNow.ToString("O") }
        });

        // Assert
        var hash = connection.GetAllEntriesFromHash(batchKey.Value);
        Assert.NotNull(hash);
        Assert.True(hash.ContainsKey("Total"));
        Assert.Equal(totalItems.ToString(), hash["Total"]);
        Assert.Equal("0", hash["Completed"]);
        Assert.Equal("0", hash["Failed"]);
        Assert.True(hash.ContainsKey("StartedAt"));
    }

    [Theory(DisplayName = "Increment batch progress completed count")]
    [Trait("Category", "Redis Integration Test - BatchProgress")]
    [MemberData(nameof(CompletionScenarios))]
    public void BatchProgress_Increment_UpdatesCompletedCount(int total, int completedCount)
    {
        // Arrange
        var batchKey = BatchKey.CreateNew();
        using var connection = JobStorage.Current.GetConnection();

        connection.SetRangeInHash(batchKey.Value, new Dictionary<string, string>
        {
            { "Total", total.ToString() },
            { "Completed", "0" },
            { "Failed", "0" },
            { "StartedAt", DateTime.UtcNow.ToString("O") }
        });

        // Act
        for (var i = 0; i < completedCount; i++)
        {
            var hash = connection.GetAllEntriesFromHash(batchKey.Value);
            var completed = int.Parse(hash.GetValueOrDefault("Completed", "0"));
            completed++;

            var updateData = new Dictionary<string, string>
            {
                { "Completed", completed.ToString() },
                { "LastUpdated", DateTime.UtcNow.ToString("O") }
            };

            if (completed >= total)
            {
                updateData.Add("CompletedAt", DateTime.UtcNow.ToString("O"));
                updateData.Add("Status", "Completed");
            }

            connection.SetRangeInHash(batchKey.Value, updateData);
        }

        // Assert
        var finalHash = connection.GetAllEntriesFromHash(batchKey.Value);
        Assert.Equal(completedCount.ToString(), finalHash["Completed"]);

        if (completedCount >= total)
        {
            Assert.True(finalHash.ContainsKey("CompletedAt"));
            Assert.Equal("Completed", finalHash["Status"]);
        }
    }

    [Theory(DisplayName = "Get batch progress with correct info")]
    [Trait("Category", "Redis Integration Test - BatchProgress")]
    [MemberData(nameof(MixedProgressScenarios))]
    public void BatchProgress_GetProgress_ReturnsCorrectInfo(int total, int completed, int failed)
    {
        // Arrange
        var batchKey = BatchKey.CreateNew();
        using var connection = JobStorage.Current.GetConnection();

        connection.SetRangeInHash(batchKey.Value, new Dictionary<string, string>
        {
            { "Total", total.ToString() },
            { "Completed", completed.ToString() },
            { "Failed", failed.ToString() },
            { "StartedAt", DateTime.UtcNow.ToString("O") },
            { "LastUpdated", DateTime.UtcNow.ToString("O") }
        });

        // Act
        var hash = connection.GetAllEntriesFromHash(batchKey.Value);
        var readTotal = int.Parse(hash.GetValueOrDefault("Total", "0"));
        var readCompleted = int.Parse(hash.GetValueOrDefault("Completed", "0"));
        var readFailed = int.Parse(hash.GetValueOrDefault("Failed", "0"));
        var totalProcessed = readCompleted + readFailed;
        var pending = readTotal - totalProcessed;
        var percentage = readTotal > 0 ? totalProcessed * 100.0 / readTotal : 0;

        // Assert
        Assert.Equal(total, readTotal);
        Assert.Equal(completed, readCompleted);
        Assert.Equal(failed, readFailed);
        Assert.Equal(total - completed - failed, pending);

        var expectedPercentage = (completed + failed) * 100.0 / total;
        Assert.Equal(expectedPercentage, percentage, precision: 1);
    }
}
