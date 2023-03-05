using MessageBroker.Wrapper.Core.EventBus;

namespace SampleReceiverConsole;
public class OrderSentMessage : IntegrationEvent
{
    public Guid OrderId { get; set; }

    public decimal Amount { get; set; }

    public string CustomerName { get; set; }

    public string CustomerEmail { get; set; }

    public override string ToString()
    {
        return $"Order Id: {OrderId} \nAmount: {Amount} \nCustomer Name: {CustomerName} \nCustomer Email: {CustomerEmail}";
    }
}
