using CastorDJ.AutoCompleteHandlers;
using CastorDJ.Player;
using Discord;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Players;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;
using System.Text;

namespace CastorDJ.Modules
{
    [RequireContext(ContextType.Guild)]
    public class MusicModule : InteractionModuleBase<SocketInteractionContext>
    {

        private readonly IAudioService _audioService;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MusicModule"/> class.
        /// </summary>
        /// <param name="audioService">the audio service</param>
        /// <exception cref="ArgumentNullException">
        ///     thrown if the specified <paramref name="audioService"/> is <see langword="null"/>.
        /// </exception>
        public MusicModule(IAudioService audioService)
        {
            ArgumentNullException.ThrowIfNull(audioService);

            _audioService = audioService;
        }

        /// <summary>
        ///     Disconnects from the current voice channel connected to asynchronously.
        /// </summary>
        /// <returns>a task that represents the asynchronous operation</returns>
        [SlashCommand("desconecta", "Desconecta o player do canal de voz", runMode: RunMode.Async)]
        public async Task Disconnect()
        {
            var player = await GetPlayerAsync().ConfigureAwait(false);

            if (player is null)
            {
                return;
            }

            await player.DisconnectAsync().ConfigureAwait(false);
            await RespondAsync("Desconectado.").ConfigureAwait(false);
        }

        /// <summary>
        ///     Plays music asynchronously.
        /// </summary>
        /// <param name="query">the search query</param>
        /// <returns>a task that represents the asynchronous operation</returns>
        [SlashCommand("play", description: "Plays music", runMode: RunMode.Async)]
        public async Task Play([Summary("música"), Autocomplete(typeof(MusicAutoCompleteHandler))] string query)
        {
            var player = await GetPlayerAsync(connectToVoiceChannel: true).ConfigureAwait(false);

            if (player is null)
            {
                return;
            }

            var simpleTrack = await _audioService.Tracks
                .LoadTrackAsync(query, TrackSearchMode.YouTube)
                .ConfigureAwait(false);

            if (simpleTrack is null)
            {
                await FollowupAsync("😖 No results.").ConfigureAwait(false);
                return;
            }

            QueueItem track = new QueueItem
            {
                Track = simpleTrack,
                Requester = Context.User.Id,
                RequestedAt = DateTime.Now
            };

            var count = await player.PlayAsync(track);
            var position = player.QueueIndex;

            var component = GetControlsComponent(player);

            if (player.Queue.Count == 1)
            {
                await DeferAsync().ConfigureAwait(false);

                var description = new StringBuilder();
                description.AppendLine($"{track.Track.Title} - {track.Track.Duration}");
                
                description.AppendLine($"Adicionado por: {MentionUtils.MentionUser(track.Requester)}");

                description.AppendLine($"[Link]({track.Track.Uri})");

                var embed = new EmbedBuilder()
                    .WithTitle("🔈 Tocando")
                    .WithDescription(description.ToString())
                    .WithUrl(track.Track.Uri.ToString())
                    .WithImageUrl(track.Track.ArtworkUri.ToString())
                    .WithFooter($"Posição: {position + 1}")
                    .Build();

                var sendMessage = await FollowupAsync(embed: embed, components: component).ConfigureAwait(false);

                player.ControlMessage = sendMessage;
                
            }
            else
            {
                await RespondAsync($"{track.Track.Title} adicionado à fila.", ephemeral: true).ConfigureAwait(false);
            }
        }

        [SlashCommand("proxima", "Adiciona uma música para ser a próxima a ser tocada", runMode: RunMode.Async)]
        public async Task Next([Summary("música"), Autocomplete(typeof(MusicAutoCompleteHandler))] string query)
        {
            var player = await GetPlayerAsync(connectToVoiceChannel: true).ConfigureAwait(false);

            if (player is null)
            {
                return;
            }

            var simpleTrack = await _audioService.Tracks
                .LoadTrackAsync(query, TrackSearchMode.YouTube)
                .ConfigureAwait(false);

            if (simpleTrack is null)
            {
                await FollowupAsync("😖 No results.").ConfigureAwait(false);
                return;
            }

            QueueItem track = new QueueItem
            {
                Track = simpleTrack,
                Requester = Context.User.Id,
                RequestedAt = DateTime.Now
            };

            var count = await player.AddNextAsync(track);
            var position = player.QueueIndex;

            var component = GetControlsComponent(player);

            if (player.Queue.Count == 1)
            {
                await DeferAsync().ConfigureAwait(false);

                var description = new StringBuilder();
                description.AppendLine($"{track.Track.Title} - {track.Track.Duration}");
                
                description.AppendLine($"Adicionado por: {MentionUtils.MentionUser(track.Requester)}");

                description.AppendLine($"[Link]({track.Track.Uri})");

                var embed = new EmbedBuilder()
                    .WithTitle("🔈 Tocando")
                    .WithDescription(description.ToString())
                    .WithUrl(track.Track.Uri.ToString())
                    .WithImageUrl(track.Track.ArtworkUri.ToString())
                    .WithFooter($"Posição: {position + 1}")
                    .Build();

                var sendMessage = await FollowupAsync(embed: embed, components: component).ConfigureAwait(false);

                player.ControlMessage = sendMessage;
                
            }
            else
            {
                await RespondAsync($"{track.Track.Title} adicionado à fila.", ephemeral: true).ConfigureAwait(false);
            }
        }


        [SlashCommand("playlist", "Adiciona uma playlist à fila", runMode: RunMode.Async)]
        public async Task Playlist([Summary("link")] string query)
        {
            var player = await GetPlayerAsync(connectToVoiceChannel: true).ConfigureAwait(false);

            if (player is null)
            {
                await DeferAsync().ConfigureAwait(false);
                return;
            }

            var playlist = await player.PlaylistAsync(query, Context.User.Id);

            var component = GetControlsComponent(player);

            await DeferAsync().ConfigureAwait(false);

            var sendMessage = await FollowupAsync($"{playlist.Title} sendo adicionada à fila.", ephemeral: true).ConfigureAwait(false);

            do { } while (player.Queue.Count == 0);

            var position = player.QueueIndex;

            var track = player.Queue.ElementAt(player.QueueIndex);

            if (player.Queue.Count == 1 && player.ControlMessage == null)
            {
                var description = new StringBuilder();
                description.AppendLine($"{track.Track.Title} - {track.Track.Duration}");
                
                description.AppendLine($"Adicionado por: {Context.User.Mention}");

                description.AppendLine($"[Link]({track.Track.Uri})");

                var embed = new EmbedBuilder()
                    .WithTitle("🔈 Tocando")
                    .WithDescription(description.ToString())
                    .WithUrl(track.Track.Uri.ToString())
                    .WithImageUrl(track.Track.ArtworkUri.ToString())
                    .WithFooter($"Posição: {position + 1}")
                    .Build();

                await sendMessage.ModifyAsync(x => { x.Embed = embed; x.Components = component; x.Content = ""; }).ConfigureAwait(false);

                player.ControlMessage = sendMessage;
            }
            else
            {
                await RespondAsync($"{playlist.Title} adicionado à fila.", ephemeral: true).ConfigureAwait(false);
            }
        }

        [SlashCommand("aleatorizar", "Aleatoriza a fila", runMode: RunMode.Async)]
        public async Task Shuffle()
        {
            var player = await GetPlayerAsync(connectToVoiceChannel: false).ConfigureAwait(false);

            if (player is null)
            {
                await RespondAsync("Erro ao aleatorizar fila!", ephemeral: true).ConfigureAwait(false);
                return;
            }

            await player.Shuffle();

            await RespondAsync("Fila aleatorizada!", ephemeral: true).ConfigureAwait(false);
        }

        [SlashCommand("fila", "Mostra a fila atual", runMode: RunMode.Async)]
        public async Task Queue()
        {
            await DeferAsync().ConfigureAwait(false);

            var player = await GetPlayerAsync(connectToVoiceChannel: false).ConfigureAwait(false);

            if (player is null)
            {
                await RespondAsync("Fila vazia!", ephemeral: true).ConfigureAwait(false);
                return;
            }

            if (player.Queue.Count is 0)
            {
                await RespondAsync("Fila vazia!", ephemeral: true).ConfigureAwait(false);
                return;
            }

            var queue = player.Queue.Skip(player.FilaSkip * 10).Take(10).ToList();
            var position = player.QueueIndex;

            var textoFila = new StringBuilder();

            textoFila.AppendLine("Fila atual:");

            for (int i = 0; i < queue.Count; i++)
            {
                var item = queue[i];

                if (i == position)
                {
                    textoFila.AppendLine($"{i + 1} 🔊     **{item.Track.Title} - {item.Track.Duration.ToString(@"hh\:mm\:ss")}**");
                }
                else
                {
                    textoFila.AppendLine($"{i + 1} 🔈     {item.Track.Title} - {item.Track.Duration.ToString(@"hh\:mm\:ss")}");
                }
            }

            var component = GetFilaComponent();

            var filaMessage = await FollowupAsync(textoFila.ToString(), components: component).ConfigureAwait(false);
            
            player.FilaMessage = filaMessage;
        }

        [SlashCommand("similares", "Obtem a fila de músicas similares", runMode: RunMode.Async)]
        public async Task GetSimilar()
        {
            var player = await GetPlayerAsync(connectToVoiceChannel: false).ConfigureAwait(false);

            if (player is null)
            {
                await RespondAsync("Fila de similares vazia!", ephemeral: true).ConfigureAwait(false);
                return;
            }

            if (player.Queue.Count is 0)
            {
                await RespondAsync("Fila de similares vazia!", ephemeral: true).ConfigureAwait(false);
                return;
            }

            var queue = player.SimilarTracks;

            var textoFila = new StringBuilder();

            textoFila.AppendLine("Fila de músicas similares:");

            for (int i = 0; i < queue.Count; i++)
            {
                var item = queue[i];
                textoFila.AppendLine($"{i + 1} 🔈     {item.Title}");
            }

            await RespondAsync(textoFila.ToString(), ephemeral: true).ConfigureAwait(false);
        }

        [ComponentInteraction("play_pause")]
        public async Task PlayPauseAsync()
        {
            var player = await GetPlayerAsync(connectToVoiceChannel: false);

            if (player is null)
            {
                await RespondAsync("Erro ao pausar música!", ephemeral: true).ConfigureAwait(false);
                return;
            }

            if (player.IsPaused)
            {
                await player.ResumeAsync().ConfigureAwait(false);
            }
            else
            {
                await player.PauseAsync().ConfigureAwait(false);
            }

            var position = player.QueueIndex;

            var controlMessage = player.ControlMessage;

            var component = GetControlsComponent(player);

            if (controlMessage is not null)
            {
                await controlMessage.ModifyAsync(x => { x.Components = component; }).ConfigureAwait(false);
                await DeferAsync().ConfigureAwait(false);
            }
            else
            {
                await RespondAsync("Play/Pause", ephemeral: true).ConfigureAwait(false);
            }
        }

        [ComponentInteraction("next_track")]
        public async Task NextTrackAsync()
        {
            var player = await GetPlayerAsync(connectToVoiceChannel: false);

            if (player is null)
            {
                await RespondAsync("Erro ao pular música!", ephemeral: true).ConfigureAwait(false);
                return;
            }

            try
            {
                await player.NextTrackAsync().ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                await RespondAsync("Não há mais músicas na fila.", ephemeral: true).ConfigureAwait(false);
                return;
            }
            
            if(player.CurrentItem is null || player.CurrentItem.Track is null)
            {
                await RespondAsync("Fila vazia!", ephemeral: true).ConfigureAwait(false);
                return;
            }

            var track = player.Queue.ElementAt(player.QueueIndex);

            var position = player.QueueIndex;

            var description = new StringBuilder();
            description.AppendLine($"{track.Track.Title} - {track.Track.Duration}");
            
            description.AppendLine($"Adicionado por: {MentionUtils.MentionUser(track.Requester)}");

            description.AppendLine($"[Link]({track.Track.Uri})");

            var embed = new EmbedBuilder()
                .WithTitle("🔈 Tocando")
                .WithDescription(description.ToString())
                .WithUrl(track.Track.Uri.ToString())
                .WithImageUrl(track.Track.ArtworkUri.ToString())
                .WithFooter($"Posição: {position + 1}")
                .Build();

            var controlMessage = player.ControlMessage;

            if (controlMessage is not null)
            {
                await controlMessage.ModifyAsync(x => x.Embed = embed).ConfigureAwait(false);
                await DeferAsync().ConfigureAwait(false);
            }
            else
            {
                await RespondAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
            }
        }

        [ComponentInteraction("previous_track")]
        public async Task PreviousTrackAsync()
        {
            var player = await GetPlayerAsync(connectToVoiceChannel: false);

            if (player is null)
            {
                await RespondAsync("Erro ao retroceder música!", ephemeral: true).ConfigureAwait(false);
                return;
            }

            try
            {
                await player.PreviousTrackAsync().ConfigureAwait(false);
            }             
            catch (InvalidOperationException)
            {
                await RespondAsync("Não há mais músicas para voltar.", ephemeral: true).ConfigureAwait(false);
                return;
            }

            var track = player.Queue.ElementAt(player.QueueIndex);
            var position = player.QueueIndex;

            var description = new StringBuilder();
            description.AppendLine($"{track.Track.Title} - {track.Track.Duration}");
            
            description.AppendLine($"Adicionado por: {MentionUtils.MentionUser(track.Requester)}");

            var embed = new EmbedBuilder()
                .WithTitle("🔈 Tocando")
                .WithDescription(description.ToString())
                .WithUrl(track.Track.Uri.ToString())
                .WithImageUrl(track.Track.ArtworkUri.ToString())
                .WithFooter($"Posição: {position + 1}")
                .Build();

            var controlMessage = player.ControlMessage;

            if (controlMessage is not null)
            {
                await controlMessage.ModifyAsync(x => x.Embed = embed).ConfigureAwait(false);
                await DeferAsync().ConfigureAwait(false);
            }
            else
            {
                await RespondAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
            }

        }

        [ComponentInteraction("previous_fila")]
        public async Task PreviousFilaAsync()
        {
            var player = await GetPlayerAsync(connectToVoiceChannel: false);

            if (player is null)
            {
                await RespondAsync("Erro ao retroceder fila!", ephemeral: true).ConfigureAwait(false);
                return;
            }

            if (player.FilaSkip <= 0)
            {
                await RespondAsync("Não há mais músicas para voltar.", ephemeral: true).ConfigureAwait(false);
                return;
            }

            player.FilaSkip -= 1;

            var queue = player.Queue.Skip(player.FilaSkip * 10).Take(10).ToList();
            var position = player.QueueIndex;

            var textoFila = new StringBuilder();

            textoFila.AppendLine("Fila atual:");

            for (int i = 0; i < queue.Count; i++)
            {
                bool current = false;

                if (i == position - player.FilaSkip * 10)
                {
                    current = true;
                }

                var queuePosition = i + player.FilaSkip * 10;

                var item = queue[i];

                if (current)
                {
                    textoFila.AppendLine($"{queuePosition + 1} 🔊     **{item.Track.Title} - {item.Track.Duration.ToString(@"hh\:mm\:ss")}**");
                }
                else
                {
                    textoFila.AppendLine($"{queuePosition + 1} 🔈     {item.Track.Title} - {item.Track.Duration.ToString(@"hh\:mm\:ss")}");
                }
            }

            var filaMessage = player.FilaMessage;

            if (filaMessage is not null)
            {
                await filaMessage.ModifyAsync(x => x.Content = textoFila.ToString()).ConfigureAwait(false);
                await DeferAsync().ConfigureAwait(false);
            }
            else
            {
                await RespondAsync("Fila", ephemeral: true).ConfigureAwait(false);
            }
        }

        [ComponentInteraction("next_fila")]
        public async Task NextFilaAsync()
        {
            var player = await GetPlayerAsync(connectToVoiceChannel: false);

            if (player is null)
            {
                await RespondAsync("Erro ao avançar fila!", ephemeral: true).ConfigureAwait(false);
                return;
            }

            if (player.FilaSkip >= player.Queue.Count / 10)
            {
                await RespondAsync("Não há mais músicas para avançar.", ephemeral: true).ConfigureAwait(false);
                return;
            }

            player.FilaSkip += 1;

            var queue = player.Queue.Skip(player.FilaSkip * 10).Take(10).ToList();
            var position = player.QueueIndex;

            var textoFila = new StringBuilder();

            textoFila.AppendLine("Fila atual:");

            for (int i = 0; i < queue.Count; i++)
            {
                bool current = false;

                // check if the current item is in the current page
                if (i == position - player.FilaSkip * 10)
                {
                    current = true;
                }

                var queuePosition = i + player.FilaSkip * 10;

                var item = queue[i];

                if (current)
                {
                    textoFila.AppendLine($"{queuePosition + 1} 🔊     **{item.Track.Title} - {item.Track.Duration.ToString(@"hh\:mm\:ss")}**");
                }
                else
                {
                    textoFila.AppendLine($"{queuePosition + 1} 🔈     {item.Track.Title} - {item.Track.Duration.ToString(@"hh\:mm\:ss")}");
                }
            }

            var filaMessage = player.FilaMessage;

            if (filaMessage is not null)
            {
                await filaMessage.ModifyAsync(x => x.Content = textoFila.ToString()).ConfigureAwait(false);
                await DeferAsync().ConfigureAwait(false);
            }
            else
            {
                await RespondAsync("Fila", ephemeral: true).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Gets the guild player asynchronously.
        /// </summary>
        /// <param name="connectToVoiceChannel">
        ///     a value indicating whether to connect to a voice channel
        /// </param>
        /// <returns>
        ///     a task that represents the asynchronous operation. The task result is the lavalink player.
        /// </returns>
        private async ValueTask<AutoPlayer?> GetPlayerAsync(bool connectToVoiceChannel = true)
        {
            var retrieveOptions = new PlayerRetrieveOptions(
                ChannelBehavior: connectToVoiceChannel ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None);

            var options = new AutoPlayerOptions();

            var result = await _audioService.Players
                .RetrieveAsync<AutoPlayer, AutoPlayerOptions>(Context, CreatePlayerAsync, retrieveOptions)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                var errorMessage = result.Status switch
                {
                    PlayerRetrieveStatus.UserNotInVoiceChannel => "You are not connected to a voice channel.",
                    PlayerRetrieveStatus.BotNotConnected => "The bot is currently not connected.",
                    _ => "Unknown error.",
                };

                return null;
            }

            if (result.Player.Queue.Count == 0)
            {
                _ = Task.Run(async () =>
                {
                    var messages = await Context.Channel.GetMessagesAsync(100).FlattenAsync();

                    messages = messages.Where(x => x.Author.Id == Context.Client.CurrentUser.Id && (x.Embeds.Any() || x.Components.Any())).ToList();

                    foreach (var message in messages)
                    {
                        await message.DeleteAsync();
                    }
                });
            }

            return result.Player;
        }

        public ValueTask<AutoPlayer> CreatePlayerAsync(IPlayerProperties<AutoPlayer, AutoPlayerOptions> properties, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(properties);

            return ValueTask.FromResult(new AutoPlayer(properties, Context.Client.CurrentUser));
        }

        private MessageComponent GetControlsComponent(AutoPlayer player)
        {
            var component = new ComponentBuilder()
                .WithButton("⏮️", "previous_track", ButtonStyle.Primary)
                .WithButton(player.IsPaused ? "▶️" : "⏸️", "play_pause", player.IsPaused ? ButtonStyle.Success : ButtonStyle.Primary)
                .WithButton("⏭️", "next_track", ButtonStyle.Primary)
                .Build();

            return component;
        }

        private MessageComponent GetFilaComponent()
        {
            var component = new ComponentBuilder()
                .WithButton("⏮️", "previous_fila", ButtonStyle.Primary)
                .WithButton("⏭️", "next_fila", ButtonStyle.Primary)
                .Build();

            return component;
        }
    }
}
