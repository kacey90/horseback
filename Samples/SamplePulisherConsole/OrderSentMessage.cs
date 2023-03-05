using MessageBroker.Wrapper.Core.EventBus;

namespace SamplePulisherConsole;
public class OrderSentMessage : IntegrationEvent
{
    public Guid OrderId { get; set; }

    public decimal Amount { get; set; }

    public string CustomerName { get; set; }

    public string CustomerEmail { get; set; }
}
