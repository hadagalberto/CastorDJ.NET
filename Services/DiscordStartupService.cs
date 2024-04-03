using CastorDJ.Utils;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CastorDJ.Services
{
    public class DiscordStartupService : IHostedService
    {

        private readonly DiscordSocketClient _discord;
        private readonly IConfiguration _config;
        private readonly ILogger<DiscordSocketClient> _logger;

        public DiscordStartupService(DiscordSocketClient discord, IConfiguration config, ILogger<DiscordSocketClient> logger)
        {
            _discord = discord;
            _config = config;
            _logger = logger;

            _discord.Log += msg => LogHelper.OnLogAsync(_logger, msg);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _discord.LoginAsync(TokenType.Bot, _config["botToken"]).ConfigureAwait(false);
            await _discord.StartAsync().ConfigureAwait(false);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _discord.LogoutAsync().ConfigureAwait(false);
            await _discord.StopAsync().ConfigureAwait(false);
        }
    }
}
