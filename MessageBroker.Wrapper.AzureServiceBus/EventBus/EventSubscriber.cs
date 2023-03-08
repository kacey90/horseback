using Azure.Messaging.ServiceBus.Administration;
using Azure.Messaging.ServiceBus;
using MessageBroker.Wrapper.AzureServiceBus.EventBus.Configuration;
using MessageBroker.Wrapper.Core.EventBus;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using MessageBroker.Wrapper.Core.EventBus.Mappers;

namespace MessageBroker.Wrapper.AzureServiceBus.EventBus
{
    internal class EventSubscriber<TIntegrationEvent, TIntegrationEventHandler> : IEventSubscriber<TIntegrationEvent, TIntegrationEventHandler>
        where TIntegrationEvent : IntegrationEvent
        where TIntegrationEventHandler : IIntegrationEventHandler<TIntegrationEvent>
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ServiceBusAdministrationClient _administrationClient;
        private readonly AzureServiceBusSubscriberConfiguration _azureServiceBusSubConfig;
        private readonly ILogger<EventSubscriber<TIntegrationEvent, TIntegrationEventHandler>> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IntegrationEventMappingService _integrationEventMappingService;

        private ServiceBusProcessor _serviceBusProcessor = null!;

        public EventSubscriber(
            AzureServiceBusSubscriberConfiguration azureServiceBusSubConfig,
            ILogger<EventSubscriber<TIntegrationEvent, TIntegrationEventHandler>> logger,
            IServiceProvider serviceProvider,
            IntegrationEventMappingService integrationEventMappingService)
        {
            _serviceBusClient = new ServiceBusClient(azureServiceBusSubConfig.ConnectionString);
            _administrationClient = new ServiceBusAdministrationClient(azureServiceBusSubConfig.ConnectionString);
            _azureServiceBusSubConfig = azureServiceBusSubConfig;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _integrationEventMappingService = integrationEventMappingService;
        }

        public async Task Subscribe(string? topic = null, CancellationToken cancellationToken = default)
        {
            var topicName = topic ?? _azureServiceBusSubConfig.TopicName;
            var subscriptionName = $"{topicName}_{typeof(TIntegrationEvent).Assembly.FullName.Split(',')[0].Trim()}_subscription";
            
            _serviceBusProcessor = _serviceBusClient.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = _azureServiceBusSubConfig.AutoCompleteMessage,
                MaxConcurrentCalls = _azureServiceBusSubConfig.MaxConcurrentCalls
            });

            _serviceBusProcessor.ProcessMessageAsync += async args =>
            {
                _logger.LogInformation("Received event {EventId} from Azure Service Bus...", args.Message.MessageId);

                //using var scope = _serviceProvider.CreateScope();
                Type messageType = _integrationEventMappingService.IntegrationEventTypeMap[args.Message.ApplicationProperties["MessageType"].ToString()];

                var message = JsonSerializer.Deserialize(args.Message.Body.ToString(), messageType);
                //dynamic typedMessage = message;
                //typedMessage = Convert.ChangeType(typedMessage, messageType);

                using var scope = _serviceProvider.CreateScope();
                //var handlerInterfaceType = typeof(IIntegrationEventHandler<>).MakeGenericType(messageType);

                var handlerTypes = scope.ServiceProvider.GetServices<IIntegrationEventHandler<TIntegrationEvent>>()
                    .Where(handler => handler.GetType().GetInterfaces().Any(i =>
                        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>) &&
                        i.GetGenericArguments().Single() == messageType))
                    .ToList();

                foreach (var eventHandler in handlerTypes)
                {
                   // var typedEventHandler = (IIntegrationEventHandler<TIntegrationEvent>)eventHandler;
                   await eventHandler.Handle((TIntegrationEvent)message);
                   //await eventHandler.Handle(typedMessage);
                }

                await args.CompleteMessageAsync(args.Message);
            };

            _serviceBusProcessor.ProcessErrorAsync += args =>
            {
                _logger.LogError(args.Exception, "Error while processing event from Azure Service Bus...");
                _logger.LogDebug($"- ErrorSource: {args.ErrorSource}");
                _logger.LogDebug($"- Entity Path: {args.EntityPath}");
                _logger.LogDebug($"- FullyQualifiedNamespace: {args.FullyQualifiedNamespace}");
                return Task.CompletedTask;
            };

            await RemoveDefaultFilters(subscriptionName);
            await AddFilters(typeof(TIntegrationEvent), subscriptionName);

            _logger.LogInformation("Message processing started for topic {TopicName} and subscription {SubscriptionName}...",
                               topicName, subscriptionName);
            await _serviceBusProcessor.StartProcessingAsync(cancellationToken);
        }

        private async Task RemoveDefaultFilters(string subscriptionName)
        {
            // check if default rule exists
            if(await _administrationClient.RuleExistsAsync(_azureServiceBusSubConfig.TopicName,
                subscriptionName, "$Default"))
            {
                _logger.LogInformation("Removing $Default rule for topic {TopicName} and subscription {SubscriptionName}...",
                                       _azureServiceBusSubConfig.TopicName, subscriptionName);
                await _administrationClient.DeleteRuleAsync(_azureServiceBusSubConfig.TopicName, subscriptionName, "$Default");
            }
        }

        private async Task AddFilters(Type eventType, string subscriptionName)
        {
            var rules = _administrationClient.GetRulesAsync(_azureServiceBusSubConfig.TopicName, subscriptionName);
            var ruleProperties = new List<RuleProperties>();
            await foreach (var rule in rules)
            {
                ruleProperties.Add(rule);
            }

            if (!ruleProperties.Any(r => r.Name == $"{eventType.Name}_Rule"))
            {
                CreateRuleOptions createRuleOptions = new CreateRuleOptions()
                {
                    Name = $"{eventType.Name}_Rule",
                    Filter = new SqlRuleFilter($"MessageType = '{eventType.Name}'")
                };

                _logger.LogInformation("Creating {Rule} for topic {TopicName} and subscription {SubscriptionName}...",
                    createRuleOptions.Name, _azureServiceBusSubConfig.TopicName, subscriptionName);
                await _administrationClient.CreateRuleAsync(_azureServiceBusSubConfig.TopicName, subscriptionName, createRuleOptions);
            }
        }

        public async Task CloseQueueAsync()
        {
            await _serviceBusProcessor.CloseAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_serviceBusProcessor != null)
            {
                await _serviceBusProcessor.DisposeAsync().ConfigureAwait(false);
            }

            if (_serviceBusClient != null)
            {
                await _serviceBusClient.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
