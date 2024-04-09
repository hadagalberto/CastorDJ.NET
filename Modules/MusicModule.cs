﻿using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.Players.Vote;
using Lavalink4NET.Players;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.Options;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Extensions;
using System;
using System.Threading.Tasks;
using Lavalink4NET.Players.Queued;
using CastorDJ.AutoCompleteHandlers;
using Discord;
using CastorDJ.Player;
using CastorDJ.Factories;
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
        [SlashCommand("disconecta", "Desconecta o player do canal de voz", runMode: RunMode.Async)]
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

            var track = await _audioService.Tracks
                .LoadTrackAsync(query, TrackSearchMode.YouTube)
                .ConfigureAwait(false);

            if (track is null)
            {
                await FollowupAsync("😖 No results.").ConfigureAwait(false);
                return;
            }

            var count = await player.PlayAsync(track);
            var position = player.QueueIndex;

            var bundle = new ComponentBuilder()
                .WithButton("⏮️", "previous_track", ButtonStyle.Primary)
                .WithButton(player.IsPaused ? "▶️" : "⏸️", "play_pause", player.IsPaused ? ButtonStyle.Success : ButtonStyle.Secondary)
                .WithButton("⏭️", "next_track", ButtonStyle.Primary)
                .Build();

            if (player.Queue.Count == 1)
            {
                await DeferAsync().ConfigureAwait(false);

                var embed = new EmbedBuilder()
                    .WithTitle("🔈 Tocando")
                    .WithDescription(track.Title)
                    .WithUrl(track.Uri.ToString())
                    .WithThumbnailUrl(track.ArtworkUri.ToString())
                    .WithFooter($"Posição: {position + 1}")
                    .Build();

                var sendMessage = await FollowupAsync(embed: embed, components: bundle).ConfigureAwait(false);

                player.ControlMessage = sendMessage;
                
            }
            else
            {
                await RespondAsync($"{track.Title} adicionado à fila.", ephemeral: true).ConfigureAwait(false);
            }
        }

        [SlashCommand("fila", "Mostra a fila atual", runMode: RunMode.Async)]
        public async Task Queue()
        {
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

            var queue = player.Queue;
            var position = player.QueueIndex;

            var textoFila = new StringBuilder();

            textoFila.AppendLine("Fila atual:");

            for (int i = 0; i < queue.Count; i++)
            {
                var item = queue[i];

                if (i == position)
                {
                    textoFila.AppendLine($"{i + 1} 🔊     **{item.Title}**");
                }
                else
                {
                    textoFila.AppendLine($"{i + 1} 🔈     {item.Title}");
                }
            }

            await RespondAsync(textoFila.ToString(), ephemeral: true).ConfigureAwait(false);
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

            var bundle = new ComponentBuilder()
                .WithButton("⏮️", "previous_track", ButtonStyle.Primary)
                .WithButton(player.IsPaused ? "▶️" : "⏸️", "play_pause", player.IsPaused ? ButtonStyle.Success : ButtonStyle.Secondary)
                .WithButton("⏭️", "next_track", ButtonStyle.Primary)
                .Build();

            if (controlMessage is not null)
            {
                await controlMessage.ModifyAsync(x => { x.Components = bundle; }).ConfigureAwait(false);
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

            var embed = new EmbedBuilder()
                .WithTitle("🔈 Tocando")
                .WithDescription(track.Title)
                .WithUrl(track.Uri.ToString())
                .WithThumbnailUrl(track.ArtworkUri.ToString())
                .WithFooter($"Posição: {position + 1}")
                .Build();

            var controlMessage = player.ControlMessage;

            if (controlMessage is not null)
            {
                await controlMessage.ModifyAsync(x => x.Embed = embed).ConfigureAwait(false);
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

            var track = player.CurrentItem.Track;
            var position = player.QueueIndex;

            var embed = new EmbedBuilder()
                .WithTitle("🔈 Tocando")
                .WithDescription(track.Title)
                .WithUrl(track.Uri.ToString())
                .WithThumbnailUrl(track.ArtworkUri.ToString())
                .WithFooter($"Posição: {position + 1}")
                .Build();

            var controlMessage = player.ControlMessage;

            if (controlMessage is not null)
            {
                await controlMessage.ModifyAsync(x => x.Embed = embed).ConfigureAwait(false);
            }
            else
            {
                await RespondAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
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

                await FollowupAsync(errorMessage).ConfigureAwait(false);
                return null;
            }

            return result.Player;
        }

        public static ValueTask<AutoPlayer> CreatePlayerAsync(IPlayerProperties<AutoPlayer, AutoPlayerOptions> properties, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(properties);

            return ValueTask.FromResult(new AutoPlayer(properties));
        }
    }
}
