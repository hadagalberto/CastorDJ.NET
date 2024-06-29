namespace Lavalink4NET.Tracks
{
    public record class QueueItem
    {

        public LavalinkTrack Track { get; set; }
        public ulong Requester { get; set; }
        public DateTime RequestedAt { get; set; }

    }
}
