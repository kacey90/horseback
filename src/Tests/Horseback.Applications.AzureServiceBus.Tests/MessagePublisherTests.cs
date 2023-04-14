using Horseback.Core.EventBus;
using Moq;
using Horseback.Applications.AzureServiceBus.Tests.Setup;
using Azure.Messaging.ServiceBus.Administration;
using Azure.Messaging.ServiceBus;
using Azure;

namespace Horseback.Applications.AzureServiceBus.Tests;

public class MessagePublisherTests : IClassFixture<TestBase>
{
    private readonly IMessagePublisher _messagePublisher;
    private readonly Mock<ServiceBusAdministrationClient> _serviceBusAdminClientMock;
    private readonly Mock<ServiceBusClient> _serviceBusClientMock;
    private readonly string _topic = "test_topic";

    public MessagePublisherTests(TestBase testBase)
    {
        _messagePublisher = testBase.MessagePublisher;
        _serviceBusAdminClientMock = testBase.ServiceBusAdminClientMock;
        _serviceBusClientMock = testBase.ServiceBusClientMock;
    }

    [Fact]
    public async Task Publish_WithValidMessageAndTopic_PublishesMessageToServiceBus()
    {
        var message = new TestIntegrationEvent
        {
            Foo = "Bar"
        };

        _serviceBusAdminClientMock.Setup(x => x.TopicExistsAsync(_topic, It.IsAny<CancellationToken>()).Result)
            .Returns(Response.FromValue(true, null));

        var serviceBusSenderMock = new Mock<ServiceBusSender>();
        _serviceBusClientMock.Setup(x => x.CreateSender(_topic)).Returns(serviceBusSenderMock.Object);

        await _messagePublisher.Publish(message);

        _serviceBusAdminClientMock.Verify(x => x.TopicExistsAsync(_topic, It.IsAny<CancellationToken>()), Times.Once);
        _serviceBusClientMock.Verify(x => x.CreateSender(_topic), Times.Once);
        _serviceBusClientMock.Verify(x => x.DisposeAsync(), Times.Never);
    }
}