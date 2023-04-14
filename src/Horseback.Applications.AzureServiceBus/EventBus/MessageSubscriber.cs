using Azure.Messaging.ServiceBus.Administration;
using Azure.Messaging.ServiceBus;
using Horseback.Applications.AzureServiceBus.EventBus.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Horseback.Core.EventBus;
using Horseback.Core.EventBus.Mappers;
using Horseback.Core.EventBus.Config;

namespace Horseback.Applications.AzureServiceBus.EventBus
{
    internal class MessageSubscriber<TIntegrationEvent, TIntegrationEventHandler> : IMessageSubscriber<TIntegrationEvent, TIntegrationEventHandler>
        where TIntegrationEvent : IntegrationEvent
        where TIntegrationEventHandler : IIntegrationEventHandler<TIntegrationEvent>
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ServiceBusAdministrationClient _administrationClient;
        private readonly AzureServiceBusSubscriberConfiguration _azureServiceBusSubConfig;
        private readonly ILogger<MessageSubscriber<TIntegrationEvent, TIntegrationEventHandler>> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IntegrationEventMappingService _integrationEventMappingService;
        private readonly string? _topic;

        private ServiceBusProcessor _serviceBusProcessor = null!;

        public MessageSubscriber(
            ServiceBusClient serviceBusClient,
            ServiceBusAdministrationClient serviceBusAdministration,
            AzureServiceBusSubscriberConfiguration azureServiceBusSubConfig,
            ILogger<MessageSubscriber<TIntegrationEvent, TIntegrationEventHandler>> logger,
            IServiceProvider serviceProvider,
            IntegrationEventMappingService integrationEventMappingService,
            IEnumerable<MessageTopicRegistration> topics)
        {
            _serviceBusClient = serviceBusClient;
            _administrationClient = serviceBusAdministration;
            _azureServiceBusSubConfig = azureServiceBusSubConfig;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _integrationEventMappingService = integrationEventMappingService;
            _topic = topics.FirstOrDefault(t => t.SubscriberType == typeof(TIntegrationEvent))?.Topic;
        }

        public async Task Subscribe(CancellationToken cancellationToken = default)
        {
            var topicName = _topic ?? _azureServiceBusSubConfig.TopicName;
            var subscriptionName = $"{topicName}_{typeof(TIntegrationEvent).Assembly.FullName.Split(',')[0].Trim()}_subscription";

            await CreateTopic(topicName, cancellationToken).ConfigureAwait(false);
            //await CreateSubscription(subscriptionName, topicName, cancellationToken).ConfigureAwait(false);
            
            _serviceBusProcessor = _serviceBusClient.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = _azureServiceBusSubConfig.AutoCompleteMessage,
                MaxConcurrentCalls = _azureServiceBusSubConfig.MaxConcurrentCalls
            });

            _serviceBusProcessor.ProcessMessageAsync += async args =>
            {
                _logger.LogInformation("Received event {EventId} from Azure Service Bus...", args.Message.MessageId);

                Type messageType = _integrationEventMappingService.IntegrationEventTypeMap[args.Message.ApplicationProperties["MessageType"].ToString()];

                var message = JsonSerializer.Deserialize(args.Message.Body.ToString(), messageType);
                
                using var scope = _serviceProvider.CreateScope();

                var handlerTypes = scope.ServiceProvider.GetServices<IIntegrationEventHandler<TIntegrationEvent>>()
                    .Where(handler => handler.GetType().GetInterfaces().Any(i =>
                        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>) &&
                        i.GetGenericArguments().Single() == messageType))
                    .ToList();

                foreach (var eventHandler in handlerTypes)
                {
                   await eventHandler.Handle((TIntegrationEvent)message);
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

        private async Task CreateTopic(string topicName, CancellationToken cancellationToken = default)
        {
            if (!await _administrationClient.TopicExistsAsync(topicName, cancellationToken))
            {
                _logger.LogInformation("Creating topic {TopicName} in Azure Service Bus...", topicName);
                await _administrationClient.CreateTopicAsync(topicName, cancellationToken);
            }
        }

        private async Task CreateSubscription(
            string subscriptionName,
            string topicName,
            CancellationToken cancellationToken = default)
        {
            if (!await _administrationClient.SubscriptionExistsAsync(topicName, subscriptionName, cancellationToken))
            {
                _logger.LogInformation("Creating subscription {SubscriptionName} in Azure Service Bus...", subscriptionName);
                await _administrationClient.CreateSubscriptionAsync(topicName, subscriptionName);
            }
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
