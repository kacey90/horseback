using System.Threading.Tasks;
using MessageBroker.Wrapper.Core.EventBus;

namespace MessageBroker.Wrapper.Core.EventHandlers
{
    public interface IIntegrationEventHandler
    {
    }

    public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
        where TIntegrationEvent : IntegrationEvent
    {
        Task Handle(TIntegrationEvent @event);
    }
}
