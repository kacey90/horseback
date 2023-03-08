using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.Wrapper.Core.EventBus
{
    public interface IEventSubscriber : IAsyncDisposable
    {
        Task Subscribe(string? topic = null, CancellationToken cancellationToken = default);
        Task CloseQueueAsync();
    }

    public interface IEventSubscriber<TIntegrationEvent, TIntegrationEventHandler> : IEventSubscriber
        where TIntegrationEvent : IntegrationEvent
        where TIntegrationEventHandler : IIntegrationEventHandler<TIntegrationEvent>
    {
    }
}
