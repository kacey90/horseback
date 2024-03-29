﻿using Microsoft.Extensions.DependencyInjection;

namespace Horseback.Core.Abstractions
{
    public interface IMessageBrokerBuilder
    {
        IHorsebackBuilder HorsebackBuilder { get; }
    }
}
