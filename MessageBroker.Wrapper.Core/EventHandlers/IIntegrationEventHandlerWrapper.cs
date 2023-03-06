using MessageBroker.Wrapper.Core.EventBus;
using System;
using System.Threading.Tasks;

namespace MessageBroker.Wrapper.Core.EventHandlers
{
    public interface IIntegrationEventHandlerWrapper : IIntegrationEventHandler
    {
        Type EventType { get; }
        Task Handle(IntegrationEvent @event);
    }
}
