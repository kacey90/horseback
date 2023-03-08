using Horseback.Core.EventBus;

namespace SamplePulisherConsole;

public class SampleMessage : IntegrationEvent
{
    public SampleMessage(Guid id, DateTime dateOccurred, string message) : base(id, dateOccurred)
    {
        MessageId = id;
        Message = message;
    }

    public Guid MessageId { get; }
    public string Message { get; }
}