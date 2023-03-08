using System;
using System.Collections.Generic;

namespace Horseback.Core.EventBus.Mappers
{
    public sealed class IntegrationEventMappingService
    {
        public Dictionary<string, Type> IntegrationEventTypeMap { get; } = new Dictionary<string, Type>();
    }
}
