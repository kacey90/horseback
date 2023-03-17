using Microsoft.Extensions.DependencyInjection;

namespace Horseback.Core.Abstractions
{
    public class DefaultMessageBrokerBuilder : IMessageBrokerBuilder
    {
        public IHorsebackBuilder HorsebackBuilder { get; }

        public DefaultMessageBrokerBuilder(IHorsebackBuilder builder)
        {
            HorsebackBuilder = builder;
        }
    }
}
