using System.ComponentModel.DataAnnotations;

namespace CastorDJ.Models;

public class PlayedTrack
{
    
    [Key]
    public Guid IdPlayedTrack { get; set; }
    [Required]
    public Guid IdDiscordServer { get; set; }
    [Required]
    public string Name { get; set; }
    [Required]
    public string YoutubeUrl { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime DatePlayed { get; set; }

    // Navigation properties
    public DiscordServer DiscordServer { get; set; }

}