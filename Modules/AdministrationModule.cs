using System.ComponentModel;
using System.Text;
using CastorDJ.Utils;
using Discord;
using Discord.Interactions;

namespace CastorDJ.Modules
{
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    [Description("Comandos de administração ")]
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
        public async Task VersaoAsync()
        {
            var commit = Environment.GetEnvironmentVariable("COMMIT");
            var buildTime = Environment.GetEnvironmentVariable("BUILD_DATE");

            var texto = "Esse bot é open source, o que significa que você pode ter o seu próprio exatamente igual a esse. O link do código fonte [está aqui](https://github.com/hadagalberto/CastorDJ.NET)\n\n";
            texto += $"Commit: {commit}\n";
            texto += $"Build: {buildTime}\n";
            texto += $"Tempo ativo: {RuntimeTracker.Instance.GetElapsedTimeInPortuguese()}\n\n";
            texto += $"Desenvolvido por: {MentionUtils.MentionUser(852673456351739935)}\n";

            var embed = new EmbedBuilder()
                .WithTitle("Informações do Bot")
                .WithDescription(texto)
                .WithColor(Color.Blue)
                .Build();

            await RespondAsync(embed: embed);
        }

        [SlashCommand("ajuda", "Exibe os comandos disponíveis")]
        public async Task HelpAsync()
        {
            var metodos = ReflectionHelper.GetCommands();

            var sb = new StringBuilder();

            sb.AppendLine("Comandos disponíveis:");

            sb.AppendLine();

            var modules = metodos.Select(x => new {x.Module, x.ModuleDescription}).Distinct();

            foreach (var module in modules)
            {
                sb.AppendLine($"**{module.ModuleDescription}**");
                foreach (var m in metodos.Where(x => x.Module == module.Module).OrderBy(x => x.Name))
                {
                    sb.AppendLine($"`/{m.Name}` - {m.Description}");
                }

                sb.AppendLine();
            }

            await RespondAsync(sb.ToString());
        }

    }
}
