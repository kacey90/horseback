using System;
using System.Collections.Generic;

namespace MessageBroker.Wrapper.Core.EventBus.Mappers
{
    public sealed class IntegrationEventMappingService
    {
        public Dictionary<string, Type> IntegrationEventTypeMap { get; } = new Dictionary<string, Type>();
    }
}
