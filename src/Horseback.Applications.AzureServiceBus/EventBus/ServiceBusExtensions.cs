using Azure.Messaging.ServiceBus.Administration;
using Horseback.Applications.AzureServiceBus.EventBus.Configuration;
using Horseback.Core.Abstractions;
using Horseback.Core.EventBus;
using Horseback.Core.EventBus.Mappers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Horseback.Applications.AzureServiceBus.EventBus
{
    public static class ServiceBusExtensions
    {
        /// <summary>
        /// Adds the Azure Service Bus as a message broker
        /// </summary>
        /// <param name="services">service collection</param>
        /// <param name="connectionString">azure service bus connection string</param>
        /// <param name="topicName">name of Topic</param>
        /// <param name="maxConcurrentCalls"></param>
        /// <param name="customRetryCount"></param>
        /// <param name="customRetryDelay"></param>
        /// <param name="autoCompleteMessage"></param>
        /// <returns></returns>
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

            return new DefaultMessageBrokerBuilder(services);
        }

        /// <summary>
        /// Add a receiver/subscriber for a specific integration event
        /// </summary>
        /// <typeparam name="TIntegrationEvent">Integration Event/Message Type</typeparam>
        /// <typeparam name="TIntegrationEventHandler">Event Handler</typeparam>
        /// <param name="builder"></param>
        /// <param name="messageAction">specific tpye of message. Used to run filters on the topic</param>
        /// <returns></returns>
        public static IMessageBrokerBuilder AddReceiver<TIntegrationEvent, TIntegrationEventHandler>(
            this IMessageBrokerBuilder builder, string messageAction)
            where TIntegrationEvent : IntegrationEvent
            where TIntegrationEventHandler : class, IIntegrationEventHandler<TIntegrationEvent>
        {
            builder.Services.AddScoped<IIntegrationEventHandler<TIntegrationEvent>, TIntegrationEventHandler>();
            builder.Services.AddScoped<IMessageSubscriber, 
                MessageSubscriber<TIntegrationEvent, TIntegrationEventHandler>>();

            var integrationEventMappingService = builder.Services.BuildServiceProvider().GetRequiredService<IntegrationEventMappingService>();

            integrationEventMappingService.IntegrationEventTypeMap.TryAdd(messageAction, typeof(TIntegrationEvent));
            builder.Services.AddSingleton(integrationEventMappingService);
            return builder;
        }

        /// <summary>
        /// Setup the Azure Service Bus for the application
        /// </summary>
        /// <typeparam name="T">Type of the application's entry point. Mostly Program.cs</typeparam>
        /// <param name="app"></param>
        /// <param name="topicName">Topic Name in Azure Service Bus (optional). \nUse this if you intend to use a topic different from what you specified in the config.</param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static async Task InitializeAzureServiceBus<T>(this IHost app, string topicName = "", ILogger? logger = null)
            where T : class
        {
            var serviceProvider = app.Services;
            var azureServiceBusConfig = serviceProvider.GetRequiredService<AzureServiceBusSubscriberConfiguration>();
            var topic = string.IsNullOrEmpty(topicName) ? azureServiceBusConfig.TopicName : topicName;
            var subscriptionName = $"{topic}_{typeof(T).Assembly.FullName.Split(',')[0].Trim()}_subscription";
            var administrationClient = new ServiceBusAdministrationClient(azureServiceBusConfig.ConnectionString);
            await Task.WhenAll(
                CreateTopic(topic, administrationClient, logger),
                CreateSubscription(azureServiceBusConfig.TopicName, subscriptionName, administrationClient, logger));
            var eventSubRegistrations = serviceProvider.GetServices<IMessageSubscriber>().ToList();
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
    }
}
