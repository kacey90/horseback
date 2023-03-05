using MessageBroker.Wrapper.Core.EventBus;

namespace SamplePulisherConsole;
public class PublishOrder
{
    private readonly IMessagePublisher _messagePublisher;

    public PublishOrder(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    public async Task PublishOrderMessage()
    {
        OrderSentMessage message = new() { OrderId = Guid.NewGuid(), Amount = 4550, CustomerEmail = "customer@email.com", CustomerName = "Customer Early man" };
        await _messagePublisher.Publish(message);
    }
}
