using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Horseback.Applications.AzureServiceBus.EventBus.Configuration;
using Horseback.Core.EventBus;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Horseback.Applications.AzureServiceBus.EventBus
{
    /// <summary>
    /// Implementation to publish messages to Azure Service Bus
    /// </summary>
    internal class MessagePublisher : IMessagePublisher, IAsyncDisposable
    {
        private readonly ILogger<MessagePublisher> _logger;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ServiceBusAdministrationClient _serviceBusAdminClient;
        private readonly AzureServiceBusSubscriberConfiguration _azureServiceBusConfig;

        public MessagePublisher(
            ILogger<MessagePublisher> logger,
            AzureServiceBusSubscriberConfiguration azureServiceBusConfig,
            ServiceBusClient serviceBusClient,
            ServiceBusAdministrationClient serviceBusAdministration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _azureServiceBusConfig = azureServiceBusConfig;
            _serviceBusClient = serviceBusClient;
            _serviceBusAdminClient = serviceBusAdministration;
        }

        public ValueTask DisposeAsync()
        {
            return _serviceBusClient.DisposeAsync();
        }

        /// <summary>
        /// Publishes a message to the azure service bus
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <param name="topic"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Publish<T>(T message, string? topic = null, CancellationToken cancellationToken = default) where T : IntegrationEvent
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            _logger.LogInformation("Publishing event {EventId} to Azure Service Bus...", message.Id);
            var topicName = topic ?? _azureServiceBusConfig.TopicName;
            
            //check if topic exists and create if not
            if (!await _serviceBusAdminClient.TopicExistsAsync(topicName))
            {
                await _serviceBusAdminClient.CreateTopicAsync(topicName);
            }
            var serviceBusSender = _serviceBusClient.CreateSender(topicName);

            var payload = JsonSerializer.Serialize(message, typeof(T));
            var serviceBusMessage = new ServiceBusMessage(payload)
            {
                MessageId = message.Id.ToString(),
                ApplicationProperties =
                {
                    { "MessageType", typeof(T).Name }
                }
            };

            await serviceBusSender.SendMessageAsync(serviceBusMessage, cancellationToken);
        }
    }
}
