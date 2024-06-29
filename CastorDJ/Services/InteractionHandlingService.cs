using CastorDJ.Player;
using CastorDJ.Utils;
using Discord;
using Discord.Interactions;
using Discord.Rest;
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

        private async Task<IReadOnlyCollection<RestGlobalCommand>> OnReady()
        {
            //await _discord.SetActivityAsync(new Game("Nenhum player ativo no momento", ActivityType.CustomStatus));

            return await _interactions.RegisterCommandsGloballyAsync(true);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _interactions.Dispose();
            return Task.CompletedTask;
        }

        private async Task OnInteractionAsync(SocketInteraction interaction)
        {
            var transaction = SentrySdk.StartTransaction("interaction-transaction", "interaction-transaction-received");
            try
            {
                var context = new SocketInteractionContext(_discord, interaction);
                var result = await _interactions.ExecuteCommandAsync(context, _services);

                if (!result.IsSuccess)
                    await context.Channel.SendMessageAsync(result.ToString());
            }
            catch(Exception ex)
            {
                if (interaction.Type == InteractionType.ApplicationCommand)
                {
                    await interaction.GetOriginalResponseAsync()
                        .ContinueWith(msg => msg.Result.DeleteAsync());
                }

                SentrySdk.CaptureException(ex);
            }
            finally
            {
                transaction.Finish();
            }
        }

    }
}
