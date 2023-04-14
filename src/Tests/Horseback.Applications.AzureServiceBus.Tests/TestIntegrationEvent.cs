using Horseback.Core.EventBus;

namespace Horseback.Applications.AzureServiceBus.Tests;
public class TestIntegrationEvent : IntegrationEvent
{
    public string Foo { get; set; } = string.Empty;
}
