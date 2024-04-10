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

        [SlashCommand("limpar", "Limpa as mensagens do canal de texto!")]
        public async Task LimparAsync()
        {
            var messages = await Context.Channel.GetMessagesAsync(100).FlattenAsync();

            messages = messages.Where(x => x.Author.Id == Context.User.Id).ToList();

            var m = await ReplyAsync($"Deletando {messages.Count()} mensagens. _Essa mensagem vai desaparecer em 5 segundos._");

            foreach (var message in messages)
            {
                await message.DeleteAsync();
            }

            const int delay = 5000;
            
            await Task.Delay(delay);
            await m.DeleteAsync();
        }

    }
}
