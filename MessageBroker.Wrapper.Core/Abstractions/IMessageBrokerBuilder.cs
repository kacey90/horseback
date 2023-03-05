using Microsoft.Extensions.DependencyInjection;

namespace MessageBroker.Wrapper.Core.Abstractions
{
    public interface IMessageBrokerBuilder
    {
        IServiceCollection Services { get; }
    }
}
