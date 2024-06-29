using System.ComponentModel.DataAnnotations;

namespace CastorDJ.Models;

public class DiscordServer
{

    [Key]
    public Guid IdDiscordServer { get; set; }
    [Required]
    public string Name { get; set; }
    [Required]
    public ulong DiscordId { get; set; }
    [Required]
    public DateTime DateAdded { get; set; }

}