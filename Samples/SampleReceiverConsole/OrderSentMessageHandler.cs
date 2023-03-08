using Horseback.Core.EventBus;

namespace SampleReceiverConsole;
public class OrderSentMessageHandler : IIntegrationEventHandler<OrderSentMessage>
{
    public Task Handle(OrderSentMessage @event)
    {
        Console.WriteLine(@event.ToString());
        return Task.CompletedTask;
    }
}
