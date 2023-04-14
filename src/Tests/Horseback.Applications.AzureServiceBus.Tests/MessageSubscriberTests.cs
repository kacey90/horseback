using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Horseback.Applications.AzureServiceBus.Tests.Setup;
using Horseback.Core.EventBus;
using Moq;

namespace Horseback.Applications.AzureServiceBus.Tests;
public class MessageSubscriberTests : IClassFixture<TestBase>
{
    private readonly IMessageSubscriber _messageSubscriber;
    private readonly Mock<ServiceBusAdministrationClient> _serviceBusAdminClientMock;
    private readonly Mock<ServiceBusClient> _serviceBusClientMock;
    private readonly string _topic = "test_topic";
    private readonly string _subscription = "test_topic_TestIntegrationEvent_subscription";

    public MessageSubscriberTests(TestBase testBase)
    {
        _messageSubscriber = testBase.MessageSubscriber;
        _serviceBusAdminClientMock = testBase.ServiceBusAdminClientMock;
        _serviceBusClientMock = testBase.ServiceBusClientMock;
    }

    [Fact]
    public async Task Subscribe_WithValidTopicAndMessage_SubscribesToServiceBus()
    {
        _serviceBusAdminClientMock.Setup(x => x.TopicExistsAsync(_topic, It.IsAny<CancellationToken>()).Result)
            .Returns(Response.FromValue(true, null));
        var serviceBusReceiverMock = new Mock<ServiceBusReceiver>();
        _serviceBusClientMock.Setup(x => x.CreateReceiver(_topic)).Returns(serviceBusReceiverMock.Object);

        await _messageSubscriber.Subscribe();

        _serviceBusAdminClientMock.Verify(x => x.TopicExistsAsync(_topic, It.IsAny<CancellationToken>()), Times.Once);
        _serviceBusClientMock.Verify(x => x.CreateReceiver(_topic), Times.Once);
        _serviceBusClientMock.Verify(x => x.DisposeAsync(), Times.Never);
    }

    [Fact]
    public async Task Subscribe_WithInvalidTopicAndMessage_ThrowsException()
    {
        _serviceBusAdminClientMock.Setup(x => x.TopicExistsAsync(_topic, It.IsAny<CancellationToken>()).Result)
            .Returns(Response.FromValue(false, default));

        //_serviceBusAdminClientMock.SetupSequence(x => x.TopicExistsAsync(_topic, default))
        //    .ReturnsAsync(Response.FromValue(true, default));

        await Assert.ThrowsAsync<ServiceBusException>(async () => await _messageSubscriber.Subscribe());

        _serviceBusAdminClientMock.Verify(x => x.TopicExistsAsync(_topic, It.IsAny<CancellationToken>()), Times.Once);
        _serviceBusClientMock.Verify(x => x.CreateReceiver(_topic), Times.Never);
        _serviceBusClientMock.Verify(x => x.DisposeAsync(), Times.Never);
    }

    [Fact]
    public async Task Subscribe_Should_Create_Topic_And_Subscription_If_Not_Exist()
    {
        var responseMock = new Mock<Response>(MockBehavior.Default);

        _serviceBusAdminClientMock
            .Setup(x => x.TopicExistsAsync(_topic, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(
                Response.FromValue(false, responseMock.Object)));

        _serviceBusAdminClientMock
            .Setup(x => x.SubscriptionExistsAsync(_topic, _subscription, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(
                Response.FromValue(false, responseMock.Object)));

        await _messageSubscriber.Subscribe();

        _serviceBusAdminClientMock.Verify(x => x.CreateTopicAsync(_topic, default), Times.Once);
        _serviceBusAdminClientMock.Verify(x => x.CreateSubscriptionAsync(_topic, _subscription, default), Times.Once);
    }

    [Fact]
    public async Task Subscribe_Should_Not_Create_Topic_Or_Subscription_If_Exist()
    {
        // Simulate topic and subscription already exist
        _serviceBusAdminClientMock
            .Setup(x => x.TopicExistsAsync(_topic, It.IsAny<CancellationToken>()).Result)
            .Returns(Response.FromValue(false, null));

        _serviceBusAdminClientMock
            .Setup(x => x.SubscriptionExistsAsync(_topic, _subscription, It.IsAny<CancellationToken>()).Result)
            .Returns(Response.FromValue(false, null));

        // Act
        await _messageSubscriber.Subscribe();

        // Assert
        _serviceBusAdminClientMock.Verify(x => x.CreateTopicAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _serviceBusAdminClientMock.Verify(x => x.CreateSubscriptionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
