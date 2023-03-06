using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Wrapper.Core.EventHandlers;

namespace MessageBroker.Wrapper.Core.EventBus
{
    public interface IMessageSubscriber
    {
        Task Subscribe(
            Type integrationEventType,
            Type integrationEventHandlerType,
            string? topic = null,
            CancellationToken cancellationToken = default);

        Task Subscribe<TIntegrationEvent, TIntegrationEventHandler>(
            string? topic = null,
            CancellationToken cancellationToken = default)
            where TIntegrationEvent : IntegrationEvent
            where TIntegrationEventHandler : IIntegrationEventHandler<TIntegrationEvent>;

        Task Subscribe<TIntegrationEvent>(
            IIntegrationEventHandler<TIntegrationEvent> integrationEventHandler,
            string? topic = null,
            CancellationToken cancellationToken = default)
            where TIntegrationEvent : IntegrationEvent;
    }
}
