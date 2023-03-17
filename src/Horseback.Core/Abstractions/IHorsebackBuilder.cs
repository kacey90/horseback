using Microsoft.Extensions.DependencyInjection;

namespace Horseback.Core.Abstractions
{
    public interface IHorsebackBuilder
    {
        IServiceCollection Services { get; }
    }
}
