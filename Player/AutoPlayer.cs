﻿using Discord;
using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Protocol.Payloads.Events;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;
using Microsoft.Extensions.DependencyInjection;
using YoutubeExplode;
using YoutubeExplode.Common;
using Lavalink4NET.InactivityTracking.Players;
using Lavalink4NET.InactivityTracking.Trackers;
using System.Text;
using Discord.WebSocket;

namespace CastorDJ.Player
{
    public sealed class AutoPlayer : LavalinkPlayer, IInactivityPlayerListener
    {
        private readonly IAudioService _audioService;
        public static readonly YoutubeClient YoutubeClient = new();
        public IUserMessage ControlMessage { get; set; }
        public IUserMessage FilaMessage { get; set; }
        public int FilaSkip { get; set; } = 0;
        public List<QueueItem> Queue { get; set; } = [];
        public int QueueIndex { get; set; } = 0;
        private YoutubeClient _youtubeClient;
        public List<LavalinkTrack> SimilarTracks = new();
        public bool Pausado = false;

        private readonly SocketSelfUser BotUser;

        public AutoPlayer(IPlayerProperties<AutoPlayer, AutoPlayerOptions> properties, SocketSelfUser botUser)
            : base(properties)
        {
            _audioService = properties.ServiceProvider.GetRequiredService<IAudioService>();
            _youtubeClient = new YoutubeClient();
            BotUser = botUser;
        }

        public async ValueTask<int> PlayAsync(QueueItem track)
        {
            Queue.Add(track);

            _ = Task.Run(() => FindSimilarTracks(track.Track));

            if (QueueIndex == 0 && (Queue.Count() == 1 || IsPaused))
            {
                await StartPlay();
            }
            if (QueueIndex > 0 && IsPaused)
            {
                await PlayPauseAsync();
            }
            return Queue.Count;
        }

        public async ValueTask<int> AddNextAsync(QueueItem track)
        {
            if (!Queue.Any())
            {
                return await PlayAsync(track);
            }

            Queue.Insert(QueueIndex + 1, track);

            return Queue.Count;
        }

        public async ValueTask<YoutubeExplode.Playlists.Playlist> PlaylistAsync(string url, ulong requester)
        {
            var playlist = await YoutubeClient.Playlists.GetAsync(url);

            _ = Task.Run(async () =>
            {
                var playlistFirstTrack = await YoutubeClient.Playlists.GetVideosAsync(playlist.Id).CollectAsync(1);

                var firstTrack = await _audioService.Tracks.LoadTrackAsync(playlistFirstTrack.First().Id, TrackSearchMode.YouTube);
                Queue.Add(new QueueItem
                {
                    Track = firstTrack,
                    Requester = requester,
                    RequestedAt = DateTime.Now,
                });

                if (QueueIndex == 0 && (Queue.Count() == 1 || IsPaused))
                {
                    await StartPlay();
                }
                if (QueueIndex > 0 && IsPaused)
                {
                    await PlayPauseAsync();
                }

                var playlistTracks = await YoutubeClient.Playlists.GetVideosAsync(playlist.Id);

                playlistTracks = playlistTracks.Where(x => x.Id != playlistFirstTrack.First().Id).ToList();

                foreach (var playlistTrack in playlistTracks)
                {
                    var track = await _audioService.Tracks.LoadTrackAsync(playlistTrack.Id, TrackSearchMode.YouTube);

                    if (track == null)
                    {
                        continue;
                    }

                    Queue.Add(new QueueItem
                    {
                        Track = track,
                        Requester = requester,
                        RequestedAt = DateTime.Now,
                    });
                }
            });

            return playlist;
        }

        // play immediately
        private async ValueTask PlayNowAsync(LavalinkTrack track)
        {
            Queue.Add(new QueueItem
            {
                Track = track,
            });
            await NextTrackAsync();

            _ = Task.Run(UpdateFila);

            if (SimilarTracks.Count <= 3)
            {
                _ = Task.Run(() => FindSimilarTracks(track));
            }
        }

        public async ValueTask NextTrackAsync()
        {
            if (QueueIndex >= Queue.Count - 1)
            {
                if (SimilarTracks.Count > 0)
                {
                    var similarTrack = SimilarTracks.ElementAt(new Random().Next(0, SimilarTracks.Count));
                    await PlayNowAsync(similarTrack);
                    SimilarTracks.Remove(similarTrack);
                    return;
                }

                throw new InvalidOperationException("Não há mais músicas na fila.");
            }
            QueueIndex++;
            var track = Queue[QueueIndex];

            _ = Task.Run(UpdateFila);

            await base.PlayAsync(track.Track);
        }

        public async ValueTask PreviousTrackAsync()
        {
            if (QueueIndex <= 0)
            {
                throw new InvalidOperationException("Não há mais músicas para voltar.");
            }
            QueueIndex--;
            var track = Queue[QueueIndex];

            _ = Task.Run(UpdateFila);

            await base.PlayAsync(track.Track);
        }

        public async ValueTask StartPlay()
        {
            if (QueueIndex >= Queue.Count)
            {
                throw new InvalidOperationException("Não há mais músicas na fila.");
            }
            var track = Queue[QueueIndex];

            _ = Task.Run(UpdateFila);

            await base.PlayAsync(track.Track);
        }

        public async ValueTask PlayPauseAsync()
        {
            if (QueueIndex == 0 && IsPaused)
            {
                await StartPlay();
            }
            if (IsPaused)
            {
                await base.ResumeAsync();
            }
            else
            {
                await base.PauseAsync();
            }
        }

        public async ValueTask StopAsync()
        {
            await base.StopAsync();
            Queue.Clear();
            QueueIndex = 0;
        }

        public ValueTask Shuffle()
        {
            _ = Task.Run(async () =>
            {
                var currentTrack = Queue[QueueIndex];
                Queue.RemoveAt(QueueIndex);

                var random = new Random();
                var shuffledQueue = Queue.OrderBy(x => random.Next()).ToList();

                Queue.Clear();
                Queue.Add(currentTrack);
                Queue.AddRange(shuffledQueue);

                QueueIndex = 0;

                await UpdateFila();
            });

            return default;
        }

        public ValueTask ClearQueue()
        {
            _ = Task.Run(async () =>
            {
                var currentTrack = Queue[QueueIndex];
                Queue.Clear();

                Queue.Add(currentTrack);
                QueueIndex = 0;

                await UpdateFila();
            });
            return default;
        }

        public async ValueTask SkipToAsync(int index)
        {
            if (index < 0 || index >= Queue.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "O índice está fora do intervalo.");
            }
            QueueIndex = index;
            await NextTrackAsync();
        }

        protected override async ValueTask NotifyTrackEndedAsync(ITrackQueueItem track, TrackEndReason endReason, CancellationToken cancellationToken = default)
        {
            if (FilaMessage != null)
            {
                _ = Task.Run(async () =>
                {
                    await UpdateFila();
                });
            }

            if (endReason == TrackEndReason.LoadFailed || endReason == TrackEndReason.Cleanup)
            {
                await NextTrackAsync();
            }
            if (QueueIndex < Queue.Count - 1 && endReason != TrackEndReason.Replaced)
            {
                QueueIndex++;
                var currentTrack = Queue[QueueIndex];

                var description = new StringBuilder();
                description.AppendLine($"[{currentTrack.Track.Title}]({currentTrack.Track.Uri}) - {currentTrack.Track.Duration}");
                description.AppendLine($"Adicionado por: {MentionUtils.MentionUser(currentTrack.Requester)}");

                var embeds = new EmbedBuilder()
                    .WithTitle("🔈 Tocando")
                    .WithDescription(description.ToString())
                    .WithUrl(currentTrack.Track.Uri.ToString())
                    .WithImageUrl(currentTrack.Track.ArtworkUri.ToString())
                    .WithFooter($"Posição: {QueueIndex + 1} de {Queue.Count}")
                    .Build();

                await ControlMessage.ModifyAsync(x => x.Embed = embeds).ConfigureAwait(false);

                await PlayAsync(currentTrack.Track, cancellationToken: cancellationToken);
                return;
            }

            if (SimilarTracks.Count > 0 && endReason == TrackEndReason.Finished || IsPaused)
            {
                var similarTrack = SimilarTracks.ElementAt(new Random().Next(0, SimilarTracks.Count));
                await PlayNowAsync(similarTrack);
                SimilarTracks.Remove(similarTrack);

                var currentTrack = Queue[QueueIndex];

                var description = new StringBuilder();
                description.AppendLine($"[{currentTrack.Track.Title}]({currentTrack.Track.Uri}) - {currentTrack.Track.Duration}");
                description.AppendLine($"Adicionado por: {ControlMessage.Author.Mention}");

                var embeds = new EmbedBuilder()
                    .WithTitle("🔈 Tocando")
                    .WithDescription(description.ToString())
                    .WithUrl(currentTrack.Track.Uri.ToString())
                    .WithImageUrl(currentTrack.Track.ArtworkUri.ToString())
                    .WithFooter($"Posição: {QueueIndex + 1} de {Queue.Count}")
                    .Build();

                await ControlMessage.ModifyAsync(x => x.Embed = embeds).ConfigureAwait(false);

                return;
            }

            if (CurrentTrack != null)
            {
                var currentTrack = Queue.ElementAt(QueueIndex);

                var description = new StringBuilder();
                description.AppendLine($"{currentTrack.Track.Title} - {currentTrack.Track.Duration}");
                description.AppendLine($"Adicionado por: {MentionUtils.MentionUser(currentTrack.Requester)}");

                var embeds = new EmbedBuilder()
                    .WithTitle("🔈 Tocando")
                    .WithDescription(description.ToString())
                    .WithUrl(currentTrack.Track.Uri.ToString())
                    .WithImageUrl(currentTrack.Track.ArtworkUri.ToString())
                    .WithFooter($"Posição: {QueueIndex + 1} de {Queue.Count}")
                    .Build();

                await ControlMessage.ModifyAsync(x => x.Embed = embeds).ConfigureAwait(false);
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("🔈 Fila vazia")
                .WithDescription("A fila de músicas acabou!")
                .Build();

            await ControlMessage.ModifyAsync(x => x.Embed = embed).ConfigureAwait(false);

            await DisconnectAsync(cancellationToken);
        }

        private async ValueTask FindSimilarTracks(LavalinkTrack track)
        {
            var ytTrack = await _youtubeClient.Videos.GetAsync(track.Identifier);

            var playlists = await _youtubeClient.Search.GetPlaylistsAsync(ytTrack.Author.ChannelTitle).CollectAsync(15);
            // get random playlist
            var playlist = playlists.ElementAt(new Random().Next(0, playlists.Count));

            var playlistTracks = await _youtubeClient.Playlists.GetVideosAsync(playlist.Id).CollectAsync(10);

            // remove the current track from the playlist
            playlistTracks = playlistTracks.Where(x => x.Id != ytTrack.Id).ToList();
            // remove tracks that have similar title
            playlistTracks = playlistTracks.Where(x => !x.Title.Contains(ytTrack.Title)).ToList();

            var similarTracks = new List<LavalinkTrack>();

            foreach (var playlistTrack in playlistTracks)
            {
                var similarTrack = await _audioService.Tracks.LoadTrackAsync(playlistTrack.Id, TrackSearchMode.YouTube);
                similarTracks.Add(similarTrack);
            }

            SimilarTracks = similarTracks;
        }

        private async ValueTask UpdateFila()
        {
            if (FilaMessage != null)
            {
                var queue = Queue.Skip(FilaSkip * 10).Take(10).ToList();
                var position = QueueIndex;

                // get the current page of the queue
                var queuePage = QueueIndex / 10;
                var queuePages = (Queue.Count + 9) / 10;
                var queuePageText = $"Página {queuePage + 1} de {queuePages}";

                var textoFila = new StringBuilder();

                textoFila.AppendLine("Fila atual:");
                textoFila.AppendLine(queuePageText);

                for (int i = 0; i < queue.Count; i++)
                {
                    bool current = i == position - FilaSkip * 10;

                    var queuePosition = i + FilaSkip * 10;

                    var item = queue[i];

                    if (current)
                    {
                        textoFila.AppendLine($"{queuePosition + 1} 🔊     **[{item.Track.Title}]({item.Track.Uri}) - {item.Track.Duration.ToString(@"hh\:mm\:ss")}** - Pedida por " + MentionUtils.MentionUser(item.Requester));
                    }
                    else
                    {
                        textoFila.AppendLine($"{queuePosition + 1} 🔈     [{item.Track.Title}]({item.Track.Uri}) - {item.Track.Duration.ToString(@"hh\:mm\:ss")} - Pedida por " + MentionUtils.MentionUser(item.Requester));
                    }
                }

                await FilaMessage.ModifyAsync(x => {
                    x.Content = textoFila.ToString();
                }).ConfigureAwait(false);
            }
        }

        public async ValueTask NotifyPlayerInactiveAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ControlMessage != null)
            {
                var channel = ControlMessage.Channel as ITextChannel;
                await channel.SendMessageAsync("Desconectando por inatividade...").ConfigureAwait(false);
            }
            if (!Pausado)
                await DisconnectAsync(cancellationToken: cancellationToken);
        }

        public async ValueTask NotifyPlayerActiveAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
        {
        }

        public async ValueTask NotifyPlayerTrackedAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
        {
        }
    }
}
