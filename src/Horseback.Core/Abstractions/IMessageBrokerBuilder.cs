using Microsoft.Extensions.DependencyInjection;

namespace Horseback.Core.Abstractions
{
    public interface IMessageBrokerBuilder
    {
        IServiceCollection Services { get; }
    }
}
