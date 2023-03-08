using System.Threading;
using System.Threading.Tasks;

namespace Horseback.Core.EventBus
{
    /// <summary>
    /// Interface for publishing messages to the message broker.
    /// </summary>
    public interface IMessagePublisher
    {
        /// <summary>
        /// Publishes a message to the message broker.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="topic">The topic to publish the message to.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task Publish<T>(T message, string? topic = null, CancellationToken cancellationToken = default) where T : IntegrationEvent;
    }
}
