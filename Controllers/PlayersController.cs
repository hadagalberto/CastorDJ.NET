using CastorDJ.Player;
using Discord.WebSocket;
using Lavalink4NET;
using Microsoft.AspNetCore.Mvc;

namespace CastorDJ.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayersController(IAudioService audioService, DiscordSocketClient discord) : Controller
    {

        [HttpGet("players")]
        public IActionResult Players()
        {
            var guildsIds = audioService.Players.GetPlayers<AutoPlayer>().Select(x => x.GuildId);
            var guilds = discord.Guilds.Where(x => guildsIds.Contains(x.Id));

            return Ok(guilds.Select(x => x.Name));
        }
    }
}
