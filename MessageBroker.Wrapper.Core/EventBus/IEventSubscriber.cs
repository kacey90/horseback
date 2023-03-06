using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Wrapper.Core.EventHandlers;

namespace MessageBroker.Wrapper.Core.EventBus
{
    public interface IEventSubscriber
    {
        Task Subscribe(string? topic = null, CancellationToken cancellationToken = default);
    }

    public interface IEventSubscriber<TIntegrationEvent, TIntegrationEventHandler> : IEventSubscriber
        where TIntegrationEvent : IntegrationEvent
        where TIntegrationEventHandler : IIntegrationEventHandler<TIntegrationEvent>
    {
    }
}
