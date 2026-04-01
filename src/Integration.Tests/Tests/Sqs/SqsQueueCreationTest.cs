using Amazon.SQS;
using Amazon.SQS.Model;
using Integration.Tests.Configurations;

namespace Integration.Tests.Tests.Sqs;

[Collection(nameof(SqsQueueCreationFixtureCollection))]
public class SqsQueueCreationTest
{
    private readonly SqsQueueCreationFixture _sqsQueueCreationFixture;

    public SqsQueueCreationTest(
        SqsQueueCreationFixture sqsQueueCreationFixture,
        InfraIntegrationTestFixture infraIntegrationTestFixture)
    {
        _sqsQueueCreationFixture = sqsQueueCreationFixture;

        _sqsQueueCreationFixture.LocalStackServiceUrl = infraIntegrationTestFixture.LocalStackServiceUrl;
    }

    public static TheoryData<string> QueueNames => new()
    {
        "test-queue-alpha",
        "test-queue-beta",
        "test-queue-gamma"
    };

    public static TheoryData<string, Dictionary<string, string>> QueueWithAttributes => new()
    {
        {
            "test-configured-queue-1",
            new Dictionary<string, string>
            {
                { QueueAttributeName.VisibilityTimeout, "30" },
                { QueueAttributeName.MessageRetentionPeriod, "86400" }
            }
        },
        {
            "test-configured-queue-2",
            new Dictionary<string, string>
            {
                { QueueAttributeName.VisibilityTimeout, "60" },
                { QueueAttributeName.ReceiveMessageWaitTimeSeconds, "20" }
            }
        }
    };

    [Theory(DisplayName = "Create SQS queue on LocalStack")]
    [Trait("Category", "SQS Integration Test - QueueCreation")]
    [MemberData(nameof(QueueNames))]
    public async Task SqsQueueCreation_CreateQueue_Succeeds(string queueName)
    {
        // Arrange
        using var client = CreateSqsClient();

        // Act
        var createResponse = await client.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = queueName
        });

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, createResponse.HttpStatusCode);
        Assert.Contains(queueName, createResponse.QueueUrl);

        // Verify queue exists in list
        var listResponse = await client.ListQueuesAsync(new ListQueuesRequest
        {
            QueueNamePrefix = queueName
        });
        Assert.Single(listResponse.QueueUrls, url => url.Contains(queueName));

        // Cleanup
        await client.DeleteQueueAsync(createResponse.QueueUrl);
    }

    [Theory(DisplayName = "Create SQS queue with custom attributes")]
    [Trait("Category", "SQS Integration Test - QueueCreation")]
    [MemberData(nameof(QueueWithAttributes))]
    public async Task SqsQueueCreation_CreateQueueWithAttributes_AppliesSettings(
        string queueName, Dictionary<string, string> attributes)
    {
        // Arrange
        using var client = CreateSqsClient();

        // Act
        var createResponse = await client.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = queueName,
            Attributes = attributes
        });

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, createResponse.HttpStatusCode);

        var attrResponse = await client.GetQueueAttributesAsync(new GetQueueAttributesRequest
        {
            QueueUrl = createResponse.QueueUrl,
            AttributeNames = attributes.Keys.ToList()
        });

        foreach (var attr in attributes)
        {
            Assert.True(attrResponse.Attributes.ContainsKey(attr.Key),
                $"Attribute '{attr.Key}' should exist in queue attributes");
            Assert.Equal(attr.Value, attrResponse.Attributes[attr.Key]);
        }

        // Cleanup
        await client.DeleteQueueAsync(createResponse.QueueUrl);
    }

    private AmazonSQSClient CreateSqsClient()
    {
        return new AmazonSQSClient(
            "test",
            "test",
            new AmazonSQSConfig
            {
                ServiceURL = _sqsQueueCreationFixture.LocalStackServiceUrl,
                AuthenticationRegion = "us-east-1"
            });
    }
}
