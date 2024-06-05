﻿using CastorDJ.Player;
using CastorDJ.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET.Extensions;
using Lavalink4NET.InactivityTracking;
using Lavalink4NET.InactivityTracking.Extensions;
using Lavalink4NET.InactivityTracking.Trackers.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
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
                    services.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()));
                    services.AddHostedService<InteractionHandlingService>();
                    services.AddHostedService<DiscordStartupService>();
                    services.AddScoped<IVolumeService, VolumeService>();

                    services.AddLavalink();
                    services.AddInactivityTracking();

                    services.ConfigureLavalink(config =>
                    {
                        config.BaseAddress = new Uri("http://localhost:2333/");
                        config.Passphrase = "senhasegura";
                        config.ReadyTimeout = TimeSpan.FromSeconds(15);
                    });

                    services.ConfigureInactivityTracking(x =>
                    {
                        x.InactivityBehavior = PlayerInactivityBehavior.None;
                    });

                    services.Configure<UsersInactivityTrackerOptions>(config =>
                    {
                        config.Threshold = 1;
                        config.Timeout = TimeSpan.FromSeconds(5);
                        config.ExcludeBots = true;
                    });

                })
                .Build();

            var configuration = host.Services.GetRequiredService<IConfiguration>();

            var sentryDsn = configuration["sentryDsn"];
            if (!string.IsNullOrWhiteSpace(sentryDsn))
            {
                SentrySdk.Init(opt =>
                {
                    opt.Dsn = sentryDsn;
                    opt.Debug = true;
                    opt.AutoSessionTracking = true;
                    opt.TracesSampleRate = 1.0;
                    opt.Release = Environment.GetEnvironmentVariable("COMMIT");
                });
            }

            await host.RunAsync();
        }
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
