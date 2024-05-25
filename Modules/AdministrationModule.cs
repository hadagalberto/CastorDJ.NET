using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CastorDJ.Utils;

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

        [SlashCommand("info", "Exibe as informações atuais do bot")]
        public async ValueTask VersaoAsync()
        {
            var commit = Environment.GetEnvironmentVariable("COMMIT");
            var buildTime = Environment.GetEnvironmentVariable("BUILD_DATE");

            var texto = "Esse bot é open source, o que significa que você pode ter o seu próprio exatamente igual a esse. O link do código fonte [está aqui](https://github.com/hadagalberto/CastorDJ.NET)";
            texto += $"Commit: {commit}\n";
            texto += $"Build: {buildTime}\n";
            texto += $"Tempo ativo: {RuntimeTracker.Instance.GetElapsedTimeInPortuguese()}\n\n";
            texto += "Desenvolvido por: @hadagalberto\n";

            var embed = new EmbedBuilder()
                .WithTitle("Informações do Bot")
                .WithDescription(texto)
                .WithColor(Color.Blue)
                .Build();

            await RespondAsync("Castor DJ",embed: embed);
        }

    }
}
