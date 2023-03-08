using Microsoft.Extensions.DependencyInjection;

namespace Horseback.Core.Abstractions
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
