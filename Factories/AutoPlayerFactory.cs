using CastorDJ.Player;
using Lavalink4NET.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CastorDJ.Factories
{
    public static class AutoPlayerFactory
    {

        public static ValueTask<AutoPlayer> CreatePlayerAsync(IPlayerProperties<AutoPlayer, AutoPlayerOptions> properties, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(properties);

            return ValueTask.FromResult(new AutoPlayer(properties));
        }

    }
}
