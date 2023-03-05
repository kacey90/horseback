using MediatR;
using System;

namespace MessageBroker.Wrapper.Core.EventBus
{
    public abstract class IntegrationEvent : INotification
    {
        public Guid Id { get; }
        public DateTime DateOccurred { get; }

        protected IntegrationEvent()
        {
            Id = Guid.NewGuid();
            DateOccurred = DateTime.UtcNow;
        }

        protected IntegrationEvent(Guid id, DateTime dateOccurred)
        {
            Id = id;
            DateOccurred = dateOccurred;
        }
    }
}
