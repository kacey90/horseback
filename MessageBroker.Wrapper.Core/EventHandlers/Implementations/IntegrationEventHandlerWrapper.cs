using MessageBroker.Wrapper.Core.EventBus;
using System;
using System.Threading.Tasks;

namespace MessageBroker.Wrapper.Core.EventHandlers.Implementations
{
    public class IntegrationEventHandlerWrapper<TIntegrationEvent> : IIntegrationEventHandlerWrapper
        where TIntegrationEvent : IntegrationEvent
    {
        private readonly IIntegrationEventHandler<TIntegrationEvent> _handler;

        public IntegrationEventHandlerWrapper(IIntegrationEventHandler<TIntegrationEvent> handler)
        {
            _handler = handler;
        }

        public Type EventType => typeof(TIntegrationEvent);

        public async Task Handle(IntegrationEvent @event)
        {
            await _handler.Handle((TIntegrationEvent)@event);
        }
    }
}
