using Horseback.Core.EventBus;

namespace SamplePulisherConsole;
public class OrderSentMessage : IntegrationEvent
{
    public Guid OrderId { get; set; }

    public decimal Amount { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;
}
