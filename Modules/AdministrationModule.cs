using System.ComponentModel;
using System.Text;
using CastorDJ.Utils;
using Discord;
using Discord.Interactions;

namespace CastorDJ.Modules
{
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
            // read version.txt file in the app root, commit is the first line and build time is the second line
            string commit;
            string buildTime;
            try
            {
                commit = (await File.ReadAllTextAsync("version.txt")).Split('\n')[0];
                var buildTimeTxt = (await File.ReadAllTextAsync("version.txt")).Split('\n')[1];
                DateTime.TryParse(buildTimeTxt, out var buildTimeDt);
                buildTime = buildTimeDt.ToString("dd/MM/yyyy HH:mm:ss");
            }
            catch (Exception)
            {
                commit = "Desconhecido";
                buildTime = "Desconhecido";
            }

            var texto = "Esse bot é open source, o que significa que você pode ter o seu próprio exatamente igual a esse. O link do código fonte [está aqui](https://github.com/hadagalberto/CastorDJ.NET)\n\n";
            texto += $"Commit:           _*{commit}*_\n";
            texto += $"Data de build:    _*{buildTime}*_\n";
            texto += $"Tempo ativo:      _*{RuntimeTracker.Instance.GetElapsedTimeInPortuguese()}*_\n\n";
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
