using MessageBroker.Wrapper.Core.EventHandlers;

namespace SampleReceiverConsole;
public class OrderSentMessageHandler : IIntegrationEventHandler<OrderSentMessage>
{
    public Task Handle(OrderSentMessage @event)
    {
        Console.WriteLine(@event.ToString());
        return Task.CompletedTask;
    }
}
