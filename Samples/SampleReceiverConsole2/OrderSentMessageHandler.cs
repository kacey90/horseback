using MessageBroker.Wrapper.Core.EventBus;

namespace SampleReceiverConsole2;
public class OrderSentMessageHandler : IIntegrationEventHandler<OrderSentMessage>
{
    public Task Handle(OrderSentMessage @event)
    {
        Console.WriteLine(@event.ToString());
        return Task.CompletedTask;
    }
}