using System.ComponentModel.DataAnnotations;

namespace CastorDJ.Models;

public class PlaylistItem
{

    [Key]
    public Guid IdPlaylistItem { get; set; }
    [Required]
    public Guid IdPlaylist { get; set; }
    [Required]
    public string Name { get; set; }
    [Required]
    public string YoutubeUrl { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime DateAdded { get; set; }

    // Navigation properties
    public Playlist Playlist { get; set; }

}