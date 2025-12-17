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
using System.Text;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace CastorDJ.Player
{
    public sealed class AutoPlayer : LavalinkPlayer, IInactivityPlayerListener
    {
        private readonly IAudioService _audioService;
        private readonly ILogger<AutoPlayer> _logger;
        public static readonly YoutubeClient YoutubeClient = new();
        public IUserMessage ControlMessage { get; set; }
        public IUserMessage FilaMessage { get; set; }
        public int FilaSkip { get; set; } = 0;
        public List<QueueItem> Queue { get; set; } = [];
        public int QueueIndex { get; set; } = 0;
        private YoutubeClient _youtubeClient;
        public List<LavalinkTrack> SimilarTracks = new();

        private readonly SocketSelfUser BotUser;

        public AutoPlayer(IPlayerProperties<AutoPlayer, AutoPlayerOptions> properties, SocketSelfUser botUser)
            : base(properties)
        {
            _audioService = properties.ServiceProvider.GetRequiredService<IAudioService>();
            _logger = properties.ServiceProvider.GetRequiredService<ILogger<AutoPlayer>>();
            _youtubeClient = new YoutubeClient();
            BotUser = botUser;
        }

        public async ValueTask<int> PlayAsync(QueueItem track)
        {
            Queue.Add(track);

            // if this is the only track, fetch and enqueue 3 similar right after it
            if (Queue.Count == 1)
            {
                _logger.LogInformation("Similar enqueue scheduled for base track '{Title}' ({Id})", track.Track?.Title, track.Track?.Identifier);
                _ = Task.Run(async () => await EnqueueSimilarAsync(track.Track, 3));
            }

            if (QueueIndex == 0 && Queue.Count == 1)
            {
                await StartPlay();
            }
            else if (IsPaused)
            {
                await ResumeAsync();
            }
            return Queue.Count;
        }

        private static string NormalizeTitleForEnqueue(string title)
        {
            var lower = title.ToLowerInvariant();
            var remove = new[] { "(official video)", "[official video]", "(lyrics)", "[lyrics]", "(audio)", "[audio]", "(hd)", "[hd]", "official", "video", "feat.", "ft.", "|", "/", "•" };
            foreach (var r in remove) lower = lower.Replace(r, string.Empty);
            lower = System.Text.RegularExpressions.Regex.Replace(lower, "\\s+", " ");
            return lower.Trim();
        }

        // Playlist-based similar finder: find a playlist containing the current track and enqueue next N tracks
        private async ValueTask EnqueueSimilarAsync(LavalinkTrack baseTrack, int count)
        {
            try
            {
                if (baseTrack is null)
                {
                    _logger.LogWarning("Similar enqueue aborted: base track is null");
                    return;
                }

                _logger.LogInformation("Playlist-based similar search started: base='{Title}' id={Id} count={Count}", baseTrack.Title, baseTrack.Identifier, count);

                var baseVideo = await _youtubeClient.Videos.GetAsync(baseTrack.Identifier);
                var baseNormalized = NormalizeTitleForEnqueue(baseVideo.Title);

                // Try to find a playlist that contains the base video
                var queries = new[]
                {
                    $"{baseVideo.Title} {baseVideo.Author.ChannelTitle}",
                    $"{baseVideo.Author.ChannelTitle} {baseVideo.Title}",
                    baseVideo.Title,
                    baseVideo.Author.ChannelTitle,
                };

                IReadOnlyList<YoutubeExplode.Playlists.PlaylistVideo> selectedPlaylistVideos = null;

                foreach (var q in queries)
                {
                    var playlists = await _youtubeClient.Search.GetPlaylistsAsync(q).CollectAsync(10);
                    foreach (var pl in playlists)
                    {
                        var videos = await _youtubeClient.Playlists.GetVideosAsync(pl.Id).CollectAsync(50);
                        if (videos.Any(v => v.Id == baseVideo.Id))
                        {
                            selectedPlaylistVideos = videos;
                            _logger.LogInformation("Matched playlist '{Title}' ({Id}) containing base track.", pl.Title, pl.Id);
                            break;
                        }
                    }

                    if (selectedPlaylistVideos != null)
                        break;
                }

                // Fallback: use author playlists if none matched containing base video
                if (selectedPlaylistVideos == null)
                {
                    var authorPlaylists = await _youtubeClient.Search.GetPlaylistsAsync(baseVideo.Author.ChannelTitle).CollectAsync(10);
                    foreach (var pl in authorPlaylists)
                    {
                        var videos = await _youtubeClient.Playlists.GetVideosAsync(pl.Id).CollectAsync(50);
                        if (videos.Any())
                        {
                            selectedPlaylistVideos = videos;
                            _logger.LogInformation("Fallback to author playlist '{Title}' ({Id}).", pl.Title, pl.Id);
                            break;
                        }
                    }
                }

                if (selectedPlaylistVideos == null)
                {
                    _logger.LogInformation("No suitable playlist found for base track. Aborting enqueue.");
                    return;
                }

                // Filter candidates from playlist
                var badTokens = new[] { "lyrics", "lyric", "audio", "official audio", "live", "nightcore", "slowed", "speed up", "sped up" };
                var candidates = selectedPlaylistVideos
                    .Where(v => v.Id != baseVideo.Id)
                    .Where(v => v.Duration == null || v.Duration >= TimeSpan.FromSeconds(60))
                    .Where(v =>
                    {
                        var norm = NormalizeTitleForEnqueue(v.Title);
                        if (norm == baseNormalized) return false;
                        var t = v.Title.ToLowerInvariant();
                        return !badTokens.Any(tok => t.Contains(tok));
                    })
                    .ToList();

                if (candidates.Count == 0)
                {
                    _logger.LogInformation("Playlist had no valid candidates after filtering.");
                    return;
                }

                var existingIds = new HashSet<string>(Queue.Select(q => q.Track.Identifier)) { baseTrack.Identifier };
                int insertIndex = Math.Min(QueueIndex + 1, Queue.Count);
                int inserted = 0;

                foreach (var video in candidates)
                {
                    if (inserted >= count) break;
                    var vid = video.Id.Value;
                    if (existingIds.Contains(vid)) continue;

                    var llTrack = await _audioService.Tracks.LoadTrackAsync(vid, TrackSearchMode.YouTube);
                    if (llTrack is null) continue;

                    Queue.Insert(insertIndex, new QueueItem
                    {
                        Track = llTrack,
                        Requester = BotUser.Id,
                        RequestedAt = DateTime.Now,
                    });
                    existingIds.Add(vid);
                    insertIndex++;
                    inserted++;
                }

                _logger.LogInformation("Playlist-based similar enqueue completed: inserted={Inserted}", inserted);
                _ = Task.Run(UpdateFila);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Playlist-based similar enqueue failed");
            }
        }

        private async Task UpdateNowPlayingAsync()
        {
            if (ControlMessage == null) return;
            if (QueueIndex < 0 || QueueIndex >= Queue.Count) return;

            var current = Queue[QueueIndex];
            var description = new StringBuilder();
            description.AppendLine($"[{current.Track.Title}]({current.Track.Uri}) - {current.Track.Duration}");
            description.AppendLine($"Adicionado por: {MentionUtils.MentionUser(current.Requester)}");

            var embed = new EmbedBuilder()
                .WithTitle("🔈 Tocando")
                .WithDescription(description.ToString())
                .WithUrl(current.Track.Uri.ToString())
                .WithImageUrl(current.Track.ArtworkUri?.ToString())
                .WithFooter($"Posição: {QueueIndex + 1} de {Queue.Count}")
                .Build();

            await ControlMessage.ModifyAsync(x => x.Embed = embed).ConfigureAwait(false);
        }

        // play immediately without advancing twice
        private async ValueTask PlayNowAsync(LavalinkTrack track)
        {
            Queue.Add(new QueueItem
            {
                Track = track,
                Requester = BotUser.Id,
                RequestedAt = DateTime.Now,
            });
            QueueIndex = Queue.Count - 1;

            await base.PlayAsync(track);
            await UpdateNowPlayingAsync();

            _ = Task.Run(UpdateFila);

            if (SimilarTracks.Count <= 3)
            {
                _ = Task.Run(() => FindSimilarTracks(track));
            }
        }

        public async ValueTask NextTrackAsync()
        {
            // proactively top up when 2 or fewer remaining including current
            var remaining = Queue.Count - QueueIndex;
            if (remaining <= 2)
            {
                var currentBase = Queue.ElementAtOrDefault(QueueIndex)?.Track;
                if (currentBase != null)
                {
                    await EnqueueSimilarAsync(currentBase, 3);
                }
            }

            if (QueueIndex >= Queue.Count - 1)
            {
                // after top-up, if still at end, stop advancing
                throw new InvalidOperationException("Não há mais músicas na fila.");
            }

            QueueIndex++;
            var track = Queue[QueueIndex];

            _ = Task.Run(UpdateFila);

            await base.PlayAsync(track.Track);
            await UpdateNowPlayingAsync();
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
            await UpdateNowPlayingAsync();
        }

        public async ValueTask StartPlay()
        {
            if (QueueIndex < 0 || QueueIndex >= Queue.Count)
            {
                throw new InvalidOperationException("Não há mais músicas na fila.");
            }
            var track = Queue[QueueIndex];

            _ = Task.Run(UpdateFila);

            await base.PlayAsync(track.Track);
            await UpdateNowPlayingAsync();
        }

        public async ValueTask PlayPauseAsync()
        {
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
            var currentTrack = Queue.ElementAtOrDefault(QueueIndex);
            if (currentTrack == null)
            {
                return default;
            }

            var random = new Random();
            var rest = Queue.Where((x, i) => i != QueueIndex).OrderBy(_ => random.Next()).ToList();

            Queue.Clear();
            Queue.Add(currentTrack);
            Queue.AddRange(rest);
            QueueIndex = 0;

            _ = Task.Run(UpdateFila);
            return default;
        }

        public ValueTask ClearQueue()
        {
            var currentTrack = Queue.ElementAtOrDefault(QueueIndex);
            if (currentTrack == null)
            {
                Queue.Clear();
                QueueIndex = 0;
            }
            else
            {
                Queue.Clear();
                Queue.Add(currentTrack);
                QueueIndex = 0;
            }

            _ = Task.Run(UpdateFila);
            return default;
        }

        public async ValueTask SkipToAsync(int index)
        {
            if (index < 0 || index >= Queue.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "O índice está fora do intervalo.");
            }
            QueueIndex = index;
            await StartPlay();
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

            if (endReason == TrackEndReason.Replaced)
            {
                return;
            }

            if (endReason == TrackEndReason.LoadFailed || endReason == TrackEndReason.Cleanup)
            {
                await NextTrackAsync();
                return;
            }

            // Finished normally
            if (QueueIndex < Queue.Count - 1)
            {
                await NextTrackAsync();
                return;
            }

            // End of queue: fetch and enqueue 3 similar, then continue if possible
            var baseTrack = Queue.ElementAtOrDefault(QueueIndex)?.Track;
            if (baseTrack != null)
            {
                await EnqueueSimilarAsync(baseTrack, 3);
            }

            if (QueueIndex < Queue.Count - 1)
            {
                await NextTrackAsync();
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("🔈 Fila vazia")
                .WithDescription("A fila de músicas acabou!")
                .Build();

            if (ControlMessage != null)
            {
                await ControlMessage.ModifyAsync(x => x.Embed = embed).ConfigureAwait(false);
            }

            await DisconnectAsync(cancellationToken);
        }

        private async ValueTask FindSimilarTracks(LavalinkTrack track)
        {
            try
            {
                _logger.LogInformation("Legacy similar search started for '{Title}' id={Id}", track?.Title, track?.Identifier);
                var yt = await _youtubeClient.Videos.GetAsync(track.Identifier);

                string Clean(string s)
                {
                    var lower = s.ToLowerInvariant();
                    var remove = new[] { "(official video)", "[official video]", "(lyrics)", "[lyrics]", "(audio)", "[audio]", "(hd)", "[hd]" };
                    foreach (var r in remove) lower = lower.Replace(r, string.Empty);
                    return lower.Trim();
                }

                var title = Clean(yt.Title);
                var artist = yt.Author.ChannelTitle;
                var duration = yt.Duration ?? TimeSpan.Zero;

                var queries = new List<string>
                {
                    $"{artist} {title}",
                    $"{artist} mix OR remix OR extended -cover -nightcore",
                    $"{artist} -cover -nightcore",
                };

                var candidates = new Dictionary<string, (LavalinkTrack track, double score)>();

                double Score(string candidateTitle, string candidateChannel, TimeSpan candidateDuration)
                {
                    double score = 0;
                    var ct = Clean(candidateTitle);
                    if (ct.Contains(title)) score += 0.5;
                    if (string.Equals(candidateChannel, artist, StringComparison.OrdinalIgnoreCase)) score += 0.3;
                    if (duration != TimeSpan.Zero && candidateDuration != TimeSpan.Zero)
                    {
                        var ratio = candidateDuration.TotalSeconds / duration.TotalSeconds;
                        var diff = Math.Abs(1.0 - ratio);
                        score += Math.Max(0, 0.2 - diff);
                    }
                    return score;
                }

                foreach (var q in queries)
                {
                    var found = await _audioService.Tracks.LoadTrackAsync(q, TrackSearchMode.YouTube);
                    _logger.LogDebug("Legacy similar query '{Query}' returned {HasResult}", q, found != null);
                    if (found != null)
                    {
                        var key = found.Identifier;
                        if (!candidates.ContainsKey(key))
                        {
                            var score = Score(found.Title, found.Author, found.Duration);
                            candidates[key] = (found, score);
                        }
                    }
                }

                var top = candidates.Values
                    .OrderByDescending(x => x.score)
                    .Take(10)
                    .Select(x => x.track)
                    .ToList();

                SimilarTracks = top;
                _logger.LogInformation("Legacy similar search completed: candidates={Candidates} selected={Selected}", candidates.Count, top.Count);
                _ = Task.Run(UpdateFila);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Legacy similar search failed");
            }
        }

        private async ValueTask UpdateFila()
        {
            if (FilaMessage != null)
            {
                var queue = Queue.Skip(FilaSkip * 10).Take(10).ToList();
                var position = QueueIndex;

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
                        textoFila.AppendLine($"{queuePosition + 1} 🔊     **[{item.Track.Title}]({item.Track.Uri}) - {item.Track.Duration.ToString(@"hh\\:mm\\:ss")}** - Pedida por " + MentionUtils.MentionUser(item.Requester));
                    }
                    else
                    {
                        textoFila.AppendLine($"{queuePosition + 1} 🔈     [{item.Track.Title}]({item.Track.Uri}) - {item.Track.Duration.ToString(@"hh\\:mm\\:ss")} - Pedida por " + MentionUtils.MentionUser(item.Requester));
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
            if (!IsPaused)
                await DisconnectAsync(cancellationToken: cancellationToken);
        }

        public async ValueTask NotifyPlayerActiveAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
        {
        }

        public async ValueTask NotifyPlayerTrackedAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
        {
        }

        public async ValueTask<int> AddNextAsync(QueueItem track)
        {
            if (Queue.Count == 0)
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

                var firstTrack = await _audioService.Tracks.LoadTrackAsync(playlistFirstTrack.First().Id.Value, TrackSearchMode.YouTube);
                if (firstTrack != null)
                {
                    Queue.Add(new QueueItem
                    {
                        Track = firstTrack,
                        Requester = requester,
                        RequestedAt = DateTime.Now,
                    });
                }

                if (QueueIndex == 0 && Queue.Count == 1)
                {
                    await StartPlay();
                }
                else if (IsPaused)
                {
                    await ResumeAsync();
                }

                var playlistTracks = await YoutubeClient.Playlists.GetVideosAsync(playlist.Id);

                var skipId = playlistFirstTrack.FirstOrDefault()?.Id;
                playlistTracks = playlistTracks.Where(x => x.Id != skipId).ToList();

                foreach (var playlistTrack in playlistTracks)
                {
                    var track = await _audioService.Tracks.LoadTrackAsync(playlistTrack.Id.Value, TrackSearchMode.YouTube);

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
    }
}
