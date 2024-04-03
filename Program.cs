using CastorDJ.Player;
using CastorDJ.Services;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CastorDJ
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(config =>
                {
                    config.AddJsonFile("config.json", false);
                })
                .ConfigureServices(services =>
                {
                    var discordClient = new DiscordSocketClient(new DiscordSocketConfig
                    {
                        AlwaysDownloadUsers = true,
                        MessageCacheSize = 10000,
                        LogLevel = Discord.LogSeverity.Verbose,
                        GatewayIntents = Discord.GatewayIntents.GuildMembers | Discord.GatewayIntents.Guilds
                    });

                    services.AddSingleton(discordClient);
                    services.AddSingleton<InteractionService>();
                    services.AddHostedService<InteractionHandlingService>();
                    services.AddHostedService<DiscordStartupService>();
                    services.AddScoped<IVolumeService, VolumeService>();

                    services.AddLavalink();
                    services.ConfigureLavalink(config =>
                    {
                        //config.BaseAddress = new Uri("http://localhost:2333");
                        config.WebSocketUri = new Uri("ws://localhost:2333/v4/websocket");
                        config.Passphrase = "senhasegura";
                        config.ReadyTimeout = TimeSpan.FromSeconds(15);
                    });

                })
                .Build();

            await host.RunAsync();
        }
    }
}
