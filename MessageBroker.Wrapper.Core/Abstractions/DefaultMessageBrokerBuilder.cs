using Microsoft.Extensions.DependencyInjection;

namespace MessageBroker.Wrapper.Core.Abstractions
{
    public class DefaultMessageBrokerBuilder : IMessageBrokerBuilder
    {
        public IServiceCollection Services { get; }

        public DefaultMessageBrokerBuilder(IServiceCollection services)
        {
            Services = services;
        }
    }
}
