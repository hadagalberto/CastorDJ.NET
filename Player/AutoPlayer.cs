using Discord;
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

namespace CastorDJ.Player
{
    public sealed class AutoPlayer : LavalinkPlayer, IInactivityPlayerListener
    {
        private readonly IAudioService _audioService;
        public static readonly YoutubeClient YoutubeClient = new YoutubeClient();
        public IUserMessage ControlMessage { get; set; }
        public IUserMessage FilaMessage { get; set; }
        public int FilaSkip { get; set; } = 0;
        public List<LavalinkTrack> Queue { get; set; } = new List<LavalinkTrack>();
        public int QueueIndex { get; set; } = 0;
        private YoutubeClient _youtubeClient;
        public List<LavalinkTrack> SimilarTracks = new List<LavalinkTrack>();

        public AutoPlayer(IPlayerProperties<AutoPlayer, AutoPlayerOptions> properties)
            : base(properties)
        {
            _audioService = properties.ServiceProvider.GetRequiredService<IAudioService>();
            _youtubeClient = new YoutubeClient();
        }

        public async ValueTask<int> PlayAsync(LavalinkTrack track)
        {
            Queue.Add(track);

            _ = Task.Run(() => FindSimilarTracks(track));

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

        public async ValueTask<YoutubeExplode.Playlists.Playlist> PlaylistAsync(string url)
        {
            var playlist = await YoutubeClient.Playlists.GetAsync(url);

            _ = Task.Run(async () =>
            {
                var playlistFirstTrack = await YoutubeClient.Playlists.GetVideosAsync(playlist.Id).CollectAsync(1);

                var firstTrack = await _audioService.Tracks.LoadTrackAsync(playlistFirstTrack.First().Id, TrackSearchMode.YouTube);
                Queue.Add(firstTrack);

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

                    Queue.Add(track);
                }
            });

            return playlist;
        }

        // play immediately
        private async ValueTask PlayNowAsync(LavalinkTrack track)
        {
            Queue.Add(track);
            await NextTrackAsync();
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
            await base.PlayAsync(track);
        }

        public async ValueTask PreviousTrackAsync()
        {
            if (QueueIndex <= 0)
            {
                throw new InvalidOperationException("Não há mais músicas para voltar.");
            }
            QueueIndex--;
            var track = Queue[QueueIndex];
            await base.PlayAsync(track);
        }

        public async ValueTask StartPlay()
        {
            if (QueueIndex >= Queue.Count)
            {
                throw new InvalidOperationException("Não há mais músicas na fila.");
            }
            var track = Queue[QueueIndex];
            await base.PlayAsync(track);
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
            _ = Task.Run(() =>
            {
                var currentTrack = Queue[QueueIndex];
                Queue.RemoveAt(QueueIndex);

                var random = new Random();
                var shuffledQueue = Queue.OrderBy(x => random.Next()).ToList();

                Queue.Clear();
                Queue.Add(currentTrack);
                Queue.AddRange(shuffledQueue);

                QueueIndex = 0;
            });

            return default;
        }

        protected override async ValueTask NotifyTrackEndedAsync(ITrackQueueItem track, TrackEndReason endReason, CancellationToken cancellationToken = default)
        {
            if (endReason == TrackEndReason.LoadFailed || endReason == TrackEndReason.Cleanup)
            {
                await NextTrackAsync();
            }
            if (QueueIndex < Queue.Count - 1 && endReason != TrackEndReason.Replaced)
            {
                QueueIndex++;
                var nextTrack = Queue[QueueIndex];

                var embeds = new EmbedBuilder()
                    .WithTitle("🔈 Tocando")
                    .WithDescription(nextTrack.Title)
                    .WithUrl(nextTrack.Uri.ToString())
                    .WithImageUrl(nextTrack.ArtworkUri.ToString())
                    .WithFooter($"Posição: {QueueIndex + 1}")
                    .Build();

                await ControlMessage.ModifyAsync(x => x.Embed = embeds).ConfigureAwait(false);

                await base.PlayAsync(nextTrack);
                return;
            }

            if (SimilarTracks.Count > 0 && endReason == TrackEndReason.Finished || IsPaused)
            {
                var similarTrack = SimilarTracks.ElementAt(new Random().Next(0, SimilarTracks.Count));
                await PlayNowAsync(similarTrack);
                SimilarTracks.Remove(similarTrack);

                var nextTrack = Queue[QueueIndex];

                var embeds = new EmbedBuilder()
                    .WithTitle("🔈 Tocando")
                    .WithDescription(nextTrack.Title)
                    .WithUrl(nextTrack.Uri.ToString())
                    .WithImageUrl(nextTrack.ArtworkUri.ToString())
                    .WithFooter($"Posição: {QueueIndex + 1}")
                    .Build();

                await ControlMessage.ModifyAsync(x => x.Embed = embeds).ConfigureAwait(false);

                await DisconnectAsync(cancellationToken);

                return;
            }

            if (CurrentTrack != null)
            {
                var currentTrack = CurrentTrack;

                var embeds = new EmbedBuilder()
                    .WithTitle("🔈 Tocando")
                    .WithDescription(currentTrack.Title)
                    .WithUrl(currentTrack.Uri.ToString())
                    .WithImageUrl(currentTrack.ArtworkUri.ToString())
                    .WithFooter($"Posição: {QueueIndex + 1}")
                    .Build();

                await ControlMessage.ModifyAsync(x => x.Embed = embeds).ConfigureAwait(false);
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("🔈 Fila vazia")
                .WithDescription("A fila de músicas acabou!")
                .Build();

            await ControlMessage.ModifyAsync(x => x.Embed = embed).ConfigureAwait(false);

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

        public async ValueTask NotifyPlayerInactiveAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await DisconnectAsync();
        }

        public ValueTask NotifyPlayerActiveAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return default; // do nothing
        }

        public ValueTask NotifyPlayerTrackedAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return default; // do nothing
        }
    }
}
