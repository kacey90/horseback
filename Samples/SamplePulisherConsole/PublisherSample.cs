using MessageBroker.Wrapper.Core.EventBus;

namespace SamplePulisherConsole;
internal class PublisherSample
{
    private readonly IMessagePublisher _messagePublisher;

    public PublisherSample(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    public async Task PublishMessage()
    {
        SampleMessage message = new(Guid.NewGuid(), DateTime.Now, $"Hello from the other side of {typeof(PublisherSample).Assembly.FullName}");
        await _messagePublisher.Publish(message);
    }
}
