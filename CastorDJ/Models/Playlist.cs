using System.ComponentModel.DataAnnotations;

namespace CastorDJ.Models;

public class Playlist
{

    [Key]
    public Guid IdPlaylist { get; set; }
    [Required]
    public Guid IdDiscordServer { get; set; }
    [Required]
    public string Name { get; set; }
    public string Description { get; set; }
    [Required]
    public DateTime DateAdded { get; set; }

    // Navigation properties
    public DiscordServer DiscordServer { get; set; }
    public List<PlaylistItem> PlaylistItems { get; set; }

}