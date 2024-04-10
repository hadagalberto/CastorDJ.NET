﻿using CastorDJ.Player;
using CastorDJ.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET.Extensions;
using Lavalink4NET.InactivityTracking;
using Lavalink4NET.InactivityTracking.Extensions;
using Lavalink4NET.InactivityTracking.Trackers.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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
                        LogLevel = LogSeverity.Verbose,
                        GatewayIntents = GatewayIntents.GuildMembers | GatewayIntents.Guilds | GatewayIntents.GuildVoiceStates,
                    });

                    services.AddSingleton(discordClient);
                    services.AddSingleton<InteractionService>();
                    services.AddHostedService<InteractionHandlingService>();
                    services.AddHostedService<DiscordStartupService>();
                    services.AddScoped<IVolumeService, VolumeService>();

                    services.AddLavalink();
                    services.ConfigureLavalink(config =>
                    {
                        config.BaseAddress = new Uri("http://lavalink:2333/");
                        config.Passphrase = "senhasegura";
                        config.ReadyTimeout = TimeSpan.FromSeconds(15);
                    });

                    services.ConfigureInactivityTracking(options =>
                    {
                        options.UseDefaultTrackers = true;
                        options.TimeoutBehavior = InactivityTrackingTimeoutBehavior.Lowest;
                    });

                })
                .Build();

            await host.RunAsync();
        }
    }
}
