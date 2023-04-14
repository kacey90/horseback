using Horseback.Core.EventBus;

namespace Horseback.Applications.AzureServiceBus.Tests;
public class TestIntegrationEventHandler : IIntegrationEventHandler<TestIntegrationEvent>
{
    public string Result { get; private set; }

    public TestIntegrationEventHandler(string result = "")
    {
        Result = result;
    }

    public Task Handle(TestIntegrationEvent @event)
    {
        Result = @event.Foo;
        return Task.CompletedTask;
    }
}
