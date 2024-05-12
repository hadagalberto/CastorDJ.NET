﻿using CastorDJ.Player;
using CastorDJ.Utils;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CastorDJ.Services
{
    public class InteractionHandlingService : IHostedService
    {

        private readonly DiscordSocketClient _discord;
        private readonly InteractionService _interactions;
        private readonly IServiceProvider _services;
        private readonly ILogger<InteractionService> _logger;
        private readonly IAudioService _audioService;

        public InteractionHandlingService(
            DiscordSocketClient discord,
            InteractionService interactions,
            IServiceProvider services,
            ILogger<InteractionService> logger,
            IAudioService audioService)
        {
            _discord = discord;
            _interactions = interactions;
            _services = services;
            _logger = logger;
            _audioService = audioService;

            _interactions.Log += msg => LogHelper.OnLogAsync(_logger, msg);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _discord.Ready += () => _interactions.RegisterCommandsGloballyAsync(true);

            await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _discord.InteractionCreated += OnInteractionAsync;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _interactions.Dispose();
            return Task.CompletedTask;
        }

        private async Task OnInteractionAsync(SocketInteraction interaction)
        {
            try
            {
                var context = new SocketInteractionContext(_discord, interaction);
                var result = await _interactions.ExecuteCommandAsync(context, _services);

                _ = Task.Run(() => {
                    var playerCount = _audioService.Players.GetPlayers<AutoPlayer>().Count();

                    // atualizar o status do bot para mostrar quantos players estão ativos
                    _discord.SetActivityAsync(playerCount > 0
                        ? new Game($"em {playerCount} players", ActivityType.Watching)
                        : new Game("em nenhum player", ActivityType.Watching));
                    
                });

                if (!result.IsSuccess)
                    await context.Channel.SendMessageAsync(result.ToString());
            }
            catch
            {
                if (interaction.Type == InteractionType.ApplicationCommand)
                {
                    await interaction.GetOriginalResponseAsync()
                        .ContinueWith(msg => msg.Result.DeleteAsync());
                }
            }
        }

    }
}
