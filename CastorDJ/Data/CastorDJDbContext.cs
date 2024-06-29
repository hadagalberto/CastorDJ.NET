using Microsoft.EntityFrameworkCore;

namespace CastorDJ.Data
{
    public class CastorDJDbContext : DbContext
    {

        public CastorDJDbContext(DbContextOptions<CastorDJDbContext> options) : base(options)
        {
        }

        public DbSet<Models.DiscordServer> DiscordServers { get; set; }
        public DbSet<Models.Playlist> Playlists { get; set; }
        public DbSet<Models.PlaylistItem> PlaylistItems { get; set; }
        public DbSet<Models.PlayedTrack> PlayedTracks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Models.DiscordServer>().ToTable("DiscordServers");
            modelBuilder.Entity<Models.Playlist>().ToTable("Playlists");
            modelBuilder.Entity<Models.PlaylistItem>().ToTable("PlaylistItems");
            modelBuilder.Entity<Models.PlayedTrack>().ToTable("PlayedTracks");
        }

    }
}
