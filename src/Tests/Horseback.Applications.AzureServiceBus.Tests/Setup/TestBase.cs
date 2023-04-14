using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Horseback.Applications.AzureServiceBus.EventBus;
using Horseback.Applications.AzureServiceBus.EventBus.Configuration;
using Horseback.Core.EventBus;
using Horseback.Core.EventBus.Config;
using Horseback.Core.EventBus.Mappers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Horseback.Applications.AzureServiceBus.Tests.Setup;
public class TestBase : IAsyncLifetime
{
    protected AzureServiceBusSubscriberConfiguration ServiceBusConfiguration { get; private set; }
    public Mock<ServiceBusClient>  ServiceBusClientMock { get; private set; }
    public Mock<ServiceBusAdministrationClient> ServiceBusAdminClientMock { get; private set; }
    public IMessagePublisher MessagePublisher { get; private set; }
    public IMessageSubscriber MessageSubscriber { get; private set; }

    public TestBase()
    {
        ServiceBusClientMock = new Mock<ServiceBusClient>();
        ServiceBusAdminClientMock = new Mock<ServiceBusAdministrationClient>();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public Task InitializeAsync()
    {
        var mockPublisherLogger = new Mock<ILogger<MessagePublisher>>();
        var mockSubscriberLogger = new Mock<ILogger<MessageSubscriber<TestIntegrationEvent, TestIntegrationEventHandler>>>();

        ServiceBusConfiguration = new AzureServiceBusSubscriberConfiguration(
            connectionString: "Endpoint=sb://horseback.servicebus.windows.net/;",
            topicName: "test_topic");

        MessagePublisher = new MessagePublisher(
            mockPublisherLogger.Object,
            ServiceBusConfiguration, ServiceBusClientMock.Object, ServiceBusAdminClientMock.Object);

        var integrationEventMappingService = new IntegrationEventMappingService();
        integrationEventMappingService.IntegrationEventTypeMap.Add(nameof(TestIntegrationEvent), typeof(TestIntegrationEvent));

        var services = new ServiceCollection();
        services.AddSingleton(integrationEventMappingService);
        services.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>, TestIntegrationEventHandler>();
        
        var serviceProvider = services.BuildServiceProvider();

        MessageSubscriber = new MessageSubscriber<TestIntegrationEvent, TestIntegrationEventHandler>(
            ServiceBusClientMock.Object, ServiceBusAdminClientMock.Object, ServiceBusConfiguration,
            mockSubscriberLogger.Object, serviceProvider, integrationEventMappingService,
            new[] { new MessageTopicRegistration("test_topic", typeof(TestIntegrationEvent)) });

        return Task.CompletedTask;
    }
}
