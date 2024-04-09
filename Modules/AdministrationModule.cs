using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CastorDJ.Modules
{
    public class AdministrationModule : InteractionModuleBase<SocketInteractionContext>
    {

        public AdministrationModule() { }

        [RequireRole("Admin")]
        [SlashCommand("limpar", "Limpa as mensagens do canal de texto!")]
        public async Task LimparAsync(uint amount = 100)
        {
            var messages = await Context.Channel.GetMessagesAsync((int)amount + 1).FlattenAsync();

            foreach (var message in messages)
            {
                await message.DeleteAsync();
            }
            const int delay = 5000;
            var m = await ReplyAsync($"Purge completed. _This message will be deleted in {delay / 1000} seconds._");
            await Task.Delay(delay);
            await m.DeleteAsync();
        }

    }
}
