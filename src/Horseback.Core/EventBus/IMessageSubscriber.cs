using System;
using System.Threading;
using System.Threading.Tasks;

namespace Horseback.Core.EventBus
{
    public interface IMessageSubscriber : IAsyncDisposable
    {
        Task Subscribe(string? topic = null, CancellationToken cancellationToken = default);
        Task CloseQueueAsync();
    }

    public interface IMessageSubscriber<TIntegrationEvent, TIntegrationEventHandler> : IMessageSubscriber
        where TIntegrationEvent : IntegrationEvent
        where TIntegrationEventHandler : IIntegrationEventHandler<TIntegrationEvent>
    {
    }
}
