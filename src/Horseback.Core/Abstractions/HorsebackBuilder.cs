using Microsoft.Extensions.DependencyInjection;
using System;

namespace Horseback.Core.Abstractions
{
    public class HorsebackBuilder : IHorsebackBuilder
    {
        public IServiceCollection Services { get; }

        public HorsebackBuilder(IServiceCollection services)
        {
            Services = services;
        }
    }
}
