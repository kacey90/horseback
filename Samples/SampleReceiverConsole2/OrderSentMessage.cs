using Horseback.Core.EventBus;

namespace SampleReceiverConsole2;
public class OrderSentMessage : IntegrationEvent
{
    public Guid OrderId { get; set; }

    public decimal Amount { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"Order Id: {OrderId} \nAmount: {Amount} \nCustomer Name: {CustomerName} \nCustomer Email: {CustomerEmail}";
    }
}
