using Horseback.Core.EventBus;

namespace SampleReceiverConsole;
public class SampleMessageHandler : IIntegrationEventHandler<SampleMessage>
{
    public Task Handle(SampleMessage @event)
    {
        Console.WriteLine("Sample Message Id: {0} \nSample Message: {1}", @event.MessageId, @event.Message);
        return Task.CompletedTask;
    }
}

public class SampleMessage : IntegrationEvent
{
    public Guid MessageId { get; set; }
    public string Message { get; set; } = string.Empty;
}
