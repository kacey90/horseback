using MessageBroker.Wrapper.AzureServiceBus.EventBus.Configuration;
using MessageBroker.Wrapper.Core.Abstractions;
using MessageBroker.Wrapper.Core.EventBus;
using MessageBroker.Wrapper.Core.EventBus.Mappers;
using MessageBroker.Wrapper.Core.EventHandlers;
using MessageBroker.Wrapper.Core.EventHandlers.Implementations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
            builder.Services.AddScoped<TIntegrationEventHandler>();

            builder.Services.AddScoped<IIntegrationEventHandlerWrapper>(sp =>
                new IntegrationEventHandlerWrapper<TIntegrationEvent>(
                    sp.GetRequiredService<TIntegrationEventHandler>()));

            builder.Services.AddScoped<IEventSubscriber, 
                EventSubscriber<TIntegrationEvent, TIntegrationEventHandler>>();

            var integrationEventMappingService = builder.Services.BuildServiceProvider().GetRequiredService<IntegrationEventMappingService>();

            integrationEventMappingService.IntegrationEventTypeMap.TryAdd(messageAction, typeof(TIntegrationEvent));
            builder.Services.AddSingleton(integrationEventMappingService);
            return builder;
        }

        public static void InitializeAzureEventSubscribers(this IHost app, ILogger? logger = null)
        {
            // get all the registrations of IEventSubscriber<TIntegrationEvent, TIntegrationEventHandler> from the service provider
            var serviceProvider = app.Services;
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
                eventSubscriber.Subscribe();
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
