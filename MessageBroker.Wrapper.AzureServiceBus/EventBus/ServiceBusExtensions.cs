using Azure.Messaging.ServiceBus.Administration;
using MessageBroker.Wrapper.AzureServiceBus.EventBus.Configuration;
using MessageBroker.Wrapper.Core.Abstractions;
using MessageBroker.Wrapper.Core.EventBus;
using MessageBroker.Wrapper.Core.EventBus.Mappers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.Wrapper.AzureServiceBus.EventBus
{
    public static class ServiceBusExtensions
    {
        public static IMessageBrokerBuilder AddAzureServiceBus(
            this IServiceCollection services,
            string connectionString,
            string topicName,
            int? maxConcurrentCalls = null,
            int? customRetryCount = null,
            int? customRetryDelay = null,
            bool autoCompleteMessage = false)
        {
            AzureServiceBusSubscriberConfiguration azureServiceBusConfig;
            if (maxConcurrentCalls.HasValue)
                azureServiceBusConfig = new AzureServiceBusSubscriberConfiguration(connectionString, topicName, maxConcurrentCalls.Value, autoCompleteMessage);
            else if (customRetryCount.HasValue)
                azureServiceBusConfig = new AzureServiceBusSubscriberConfiguration(connectionString, topicName, maxConcurrentCalls.Value, customRetryCount.Value, customRetryDelay.Value, autoCompleteMessage);
            else
                azureServiceBusConfig = new AzureServiceBusSubscriberConfiguration(connectionString, topicName);
            services.AddSingleton(azureServiceBusConfig);

            services.AddLogging();

            services.AddSingleton<IMessagePublisher, MessagePublisher>();
            services.AddSingleton<IntegrationEventMappingService>();
            //services.AddSingleton<IMessageSubscriber, MessageSubscriber>();

            return new DefaultMessageBrokerBuilder(services);
        }

        public static IMessageBrokerBuilder AddReceiver<TIntegrationEvent, TIntegrationEventHandler>(
            this IMessageBrokerBuilder builder, string messageAction)
            where TIntegrationEvent : IntegrationEvent
            where TIntegrationEventHandler : class, IIntegrationEventHandler<TIntegrationEvent>
        {
            builder.Services.AddScoped<IIntegrationEventHandler<TIntegrationEvent>, TIntegrationEventHandler>();
            builder.Services.AddScoped<IEventSubscriber, 
                EventSubscriber<TIntegrationEvent, TIntegrationEventHandler>>();

            var integrationEventMappingService = builder.Services.BuildServiceProvider().GetRequiredService<IntegrationEventMappingService>();

            integrationEventMappingService.IntegrationEventTypeMap.TryAdd(messageAction, typeof(TIntegrationEvent));
            builder.Services.AddSingleton(integrationEventMappingService);
            return builder;
        }

        public static async Task InitializeAzureEventSubscribers<T>(this IHost app, string topicName = "", ILogger? logger = null)
        {
            // get all the registrations of IEventSubscriber<TIntegrationEvent, TIntegrationEventHandler> from the service provider
            var serviceProvider = app.Services;
            var azureServiceBusConfig = serviceProvider.GetRequiredService<AzureServiceBusSubscriberConfiguration>();
            var topic = string.IsNullOrEmpty(topicName) ? azureServiceBusConfig.TopicName : topicName;
            var subscriptionName = $"{topic}_{typeof(T).Assembly.FullName.Split(',')[0].Trim()}_subscription";
            var administrationClient = new ServiceBusAdministrationClient(azureServiceBusConfig.ConnectionString);
            await Task.WhenAll(
                CreateTopic(topic, administrationClient, logger),
                CreateSubscription(azureServiceBusConfig.TopicName, subscriptionName, administrationClient, logger));
            var eventSubRegistrations = serviceProvider.GetServices<IEventSubscriber>().ToList();
            foreach (var eventSubscriber in eventSubRegistrations)
            {
                Type? integrationEventType = null;
                var eventSubscriberType = eventSubscriber.GetType();
                if (eventSubscriberType.IsGenericType)
                {
                    integrationEventType = eventSubscriberType.GetGenericArguments()[0];
                }
                logger?.LogInformation("Subscription for {IntegrationEvent}", integrationEventType is null ? "Unknown Type" : integrationEventType.FullName);
                await eventSubscriber.Subscribe();
            }
        }

        private static async Task CreateTopic(string topicName,
                                              ServiceBusAdministrationClient adminClient,
                                              ILogger? logger,
                                              CancellationToken cancellationToken = default)
        {
            if (!await adminClient.TopicExistsAsync(topicName, cancellationToken))
            {
                logger?.LogInformation("Creating topic {TopicName} in Azure Service Bus...", topicName);
                await adminClient.CreateTopicAsync(topicName, cancellationToken);
            }
        }

        private static async Task CreateSubscription(
            string topicName,
            string subscriptionName,
            ServiceBusAdministrationClient adminClient,
            ILogger? logger,
            CancellationToken cancellationToken = default)
        {
            if (!await adminClient.SubscriptionExistsAsync(topicName, subscriptionName))
            {
                logger?.LogInformation("Creating subscription {SubscriptionName} in Azure Service Bus...", subscriptionName);
                await adminClient.CreateSubscriptionAsync(topicName, subscriptionName);
            }
        }

        public static void InitializeAzureServiceBus(this IHost app, ILogger? logger = null)
        {
            var serviceProvider = app.Services;
            //var logger = serviceProvider.GetRequiredService<ILogger<MessageSubscriber>>();

            var messageSubscriber = serviceProvider.GetRequiredService<IMessageSubscriber>();
            if (messageSubscriber == null)
            {
                logger?.LogError("unable to resolve {0} service", nameof(IMessageSubscriber));
                return;
            }

            // find subscribers with handlers
            //var allInterfaces = from type in Assembly.GetEntryAssembly().GetTypes()
            //                    select type;
            var assembly = Assembly.GetCallingAssembly();
            var integrationEventHandlerTypes = 
                from type in assembly.GetTypes()
                where type.GetInterfaces().Any(i => 
                    i.IsGenericType && 
                    i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
                select type;

            foreach (var integrationEventHandlerType in integrationEventHandlerTypes )
            {
                var messageSubscriberHandler = serviceProvider.GetService(integrationEventHandlerType);
                if (messageSubscriberHandler == null)
                {
                    logger?.LogError("Unable to resolve {0} subscriber", integrationEventHandlerType.Name);
                    continue;
                }
                var eventType = messageSubscriberHandler.GetType().GetInterfaces()[0].GetGenericArguments()[0];
                if (eventType == null)
                {
                    logger?.LogError("Event Type is undefined");
                    return;
                }
                logger?.LogInformation("Subscription for {IntegrationEvent}", eventType.FullName);
                messageSubscriber.Subscribe(eventType, messageSubscriberHandler.GetType());
            }
        }
    }
}
