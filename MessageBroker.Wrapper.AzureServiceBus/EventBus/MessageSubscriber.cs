using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using MessageBroker.Wrapper.AzureServiceBus.EventBus.Configuration;
using MessageBroker.Wrapper.Core.EventBus;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.Wrapper.AzureServiceBus.EventBus
{
    internal class MessageSubscriber : IMessageSubscriber, IAsyncDisposable
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ServiceBusAdministrationClient _administrationClient;
        private readonly AzureServiceBusSubscriberConfiguration _azureServiceBusSubConfig;
        private readonly ILogger<MessageSubscriber> _logger;
        private readonly IServiceProvider _serviceProvider;

        public MessageSubscriber(
            AzureServiceBusSubscriberConfiguration azureServiceBusSubConfig,
            ILogger<MessageSubscriber> logger,
            IServiceProvider serviceProvider)
        {
            _serviceBusClient = new ServiceBusClient(azureServiceBusSubConfig.ConnectionString);
            _administrationClient = new ServiceBusAdministrationClient(azureServiceBusSubConfig.ConnectionString);
            _azureServiceBusSubConfig = azureServiceBusSubConfig;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }
        public async ValueTask DisposeAsync()
        {
            await _serviceBusClient.DisposeAsync();
        }

        public async Task Subscribe(Type integrationEventType, Type integrationEventHandlerType, string? topic = null, CancellationToken cancellationToken = default)
        {
            var subscribeMethod = typeof(MessageSubscriber).GetMethod(nameof(Subscribe), new Type[] { typeof(string), typeof(CancellationToken) });

            var genericSubscribeMethod = subscribeMethod.MakeGenericMethod(integrationEventType, integrationEventHandlerType);

            await (Task)genericSubscribeMethod.Invoke(this, new object[] { topic, cancellationToken});
        }

        public async Task Subscribe<TIntegrationEvent, TIntegrationEventHandler>(string? topic = null, CancellationToken cancellationToken = default)
            where TIntegrationEvent : IntegrationEvent
            where TIntegrationEventHandler : IIntegrationEventHandler<TIntegrationEvent>
        {
            var topicName = topic ?? _azureServiceBusSubConfig.TopicName;
            var subscriptionName = $"{topicName}_{typeof(TIntegrationEvent).Assembly.FullName.Split(',')[0].Trim()}_subscription";
            if (!await _administrationClient.TopicExistsAsync(topicName, cancellationToken))
            {
                await _administrationClient.CreateTopicAsync(topicName, cancellationToken);
            }

            if (!await _administrationClient.SubscriptionExistsAsync(topicName, subscriptionName))
            {
                await _administrationClient.CreateSubscriptionAsync(topicName, subscriptionName);
            }
            var serviceBusProcessor = _serviceBusClient.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = _azureServiceBusSubConfig.AutoCompleteMessage,
                MaxConcurrentCalls = _azureServiceBusSubConfig.MaxConcurrentCalls
            });

            serviceBusProcessor.ProcessMessageAsync += async args =>
            {
                _logger.LogInformation("Received event {EventId} from Azure Service Bus...", args.Message.MessageId);

                var message = JsonSerializer.Deserialize<TIntegrationEvent>(args.Message.Body.ToString(), 
                    new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (message == null)
                {
                    _logger.LogError("Unable to deserialize message {EventId} from Azure Service Bus...", 
                        args.Message.MessageId);
                    return;
                }
                var integrationEventHandler = _serviceProvider.GetService(typeof(TIntegrationEventHandler)) as IIntegrationEventHandler<TIntegrationEvent>;
                if (integrationEventHandler == null)
                {
                    _logger.LogError("Unable to resolve integration event handler for event {EventId} from Azure Service Bus...", 
                        args.Message.MessageId);
                    return;
                }
                try
                {
                    await integrationEventHandler.Handle(message);
                    await args.CompleteMessageAsync(args.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing event {EventId} from Azure Service Bus...", 
                        args.Message.MessageId);
                    await args.AbandonMessageAsync(args.Message);
                }
            };

            serviceBusProcessor.ProcessErrorAsync += args =>
            {
                _logger.LogError(args.Exception, "Error while processing event from Azure Service Bus...");
                return Task.CompletedTask;
            };

            await AddFilters(typeof(TIntegrationEvent), subscriptionName);
            
            await serviceBusProcessor.StartProcessingAsync(cancellationToken);
        }

        private async Task AddFilters(Type eventType, string subscriptionName)
        {
            var rules = _administrationClient.GetRulesAsync(_azureServiceBusSubConfig.TopicName, subscriptionName)
                .ConfigureAwait(false);
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

                await _administrationClient.CreateRuleAsync(_azureServiceBusSubConfig.TopicName, subscriptionName, createRuleOptions);
            }
        }

        public async Task Subscribe<TIntegrationEvent>(IIntegrationEventHandler<TIntegrationEvent> integrationEventHandler, string? topic = null, CancellationToken cancellationToken = default) where TIntegrationEvent : IntegrationEvent
        {
            var topicName = topic ?? _azureServiceBusSubConfig.TopicName;
            var serviceBusProcessor = _serviceBusClient.CreateProcessor(topicName, typeof(TIntegrationEvent).FullName, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = _azureServiceBusSubConfig.AutoCompleteMessage,
                MaxConcurrentCalls = _azureServiceBusSubConfig.MaxConcurrentCalls
            });

            serviceBusProcessor.ProcessMessageAsync += async args =>
            {
                _logger.LogInformation("Received event {EventId} from Azure Service Bus...", args.Message.MessageId);

                var message = JsonSerializer.Deserialize<TIntegrationEvent>(args.Message.Body.ToString(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (message == null)
                {
                    _logger.LogError("Unable to deserialize message {EventId} from Azure Service Bus...", args.Message.MessageId);
                    return;
                }
                try
                {
                    await integrationEventHandler.Handle(message);
                    await args.CompleteMessageAsync(args.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing event {EventId} from Azure Service Bus...", args.Message.MessageId);
                    await args.AbandonMessageAsync(args.Message);
                }
            };

            serviceBusProcessor.ProcessErrorAsync += args =>
            {
                _logger.LogError(args.Exception, "Error while processing event from Azure Service Bus...");
                return Task.CompletedTask;
            };

            await serviceBusProcessor.StartProcessingAsync(cancellationToken);
        }
    }
}
