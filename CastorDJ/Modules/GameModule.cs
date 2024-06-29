using Discord.Interactions;
using Discord.WebSocket;
using System.ComponentModel;
using System.Text;

namespace CastorDJ.Modules
{
    [Description("Comandos de games 🎮")]
    public class GameModule : InteractionModuleBase<SocketInteractionContext>
    {

        [SlashCommand("sorteio", "Sorteia um número entre 1 e 100!")]
        public async Task SorteioAsync()
        {
            var random = new Random();
            var number = random.Next(1, 101);
            await ReplyAsync($"O número sorteado foi: {number}");
        }

        [SlashCommand("times", "Forma times aleatórios com os usuários na chamada de voz de acordo com o tamanho desejado!")]
        public async Task FormaTimesAsync([Summary("Times")] int numeroDeTimes)
        {
            //var userVocieChannel = Context.Guild.Channels.Where(x => x is SocketVoiceChannel && x.Users.Where(x => x.VoiceChannel != null).Select(x => x.Id).Contains(Context.User.Id)).FirstOrDefault();
            var userVoiceChannel = Context.Guild.Users.Where(x => x.VoiceChannel != null).FirstOrDefault(x => x.Id == Context.User.Id)?.VoiceChannel;

            if (userVoiceChannel == null)
            {
                await RespondAsync("Você precisa estar em um canal de voz para usar este comando.");
                return;
            }

            var users = userVoiceChannel.Users.ToList();

            // remove bots
            users = users.Where(x => !x.IsBot).ToList();

            // only users at the same voice channel
            users = users.Where(x => userVoiceChannel.Id == x.VoiceChannel?.Id).ToList();

            if (users.Count < numeroDeTimes)
            {
                await RespondAsync("Não há usuários suficientes na chamada de voz.");
                return;
            }

            var random = new Random();
            var teams = new List<List<SocketGuildUser>>();
            for (int i = 0; i < numeroDeTimes; i++)
            {
                teams.Add(new List<SocketGuildUser>());
            }

            var teamIndex = 0;

            // shuffle users
            users = users.OrderBy(x => random.Next()).ToList();

            while (users.Count > 0)
            {
                var userIndex = random.Next(0, users.Count);
                teams[teamIndex].Add(users[userIndex]);
                users.RemoveAt(userIndex);
                teamIndex++;
                if (teamIndex >= numeroDeTimes)
                {
                    teamIndex = 0;
                }
            }
            
            var nomesTimes = ObterNomeAnimaisParaTimes(numeroDeTimes);

            var sb = new StringBuilder();
            for (int i = 0; i < teams.Count; i++)
            {
                sb.AppendLine($"Time {nomesTimes[i]}:");
                foreach (var user in teams[i])
                {
                    sb.AppendLine($"- {user.Mention}");
                }
                sb.AppendLine();
            }

            await RespondAsync(sb.ToString());
        }

        private string[] ObterNomeAnimaisParaTimes(int quantidadeTimes)
        {
            var animais = new List<string>
            {
                "Águia",
                "Porco",
                "Cachorro",
                "Gato",
                "Leão",
                "Tigre",
                "Urso",
                "Elefante",
                "Girafa",
                "Cavalo",
                "Vaca",
                "Pato",
                "Galinha",
                "Pombo",
                "Papagaio",
                "Pinguim",
                "Tartaruga",
                "Cobra",
                "Sapo",
                "Peixe",
                "Tubarão",
                "Polvo",
                "Lagosta",
                "Caranguejo",
                "Baleia",
                "Golfinho",
                "Orca",
                "Tubarão",
                "Tartaruga",
                "Cavalo-marinho",
                "Estrela-do-mar",
                "Água-viva",
                "Medusa",
                "Polvo",
                "Lula",
                "Caracol",
                "Lesma",
                "Minhoca",
                "Formiga",
                "Abelha",
                "Vespa",
                "Joaninha",
                "Borboleta",
                "Libélula",
                "Besouro",
                "Barata",
                "Mosquito",
                "Mosca",
                "Aracnídeo",
                "Aranha",
            };

            var random = new Random();
            var animaisSorteados = new List<string>();
            for (int i = 0; i < quantidadeTimes; i++)
            {
                var animalIndex = random.Next(0, animais.Count);
                animaisSorteados.Add(animais[animalIndex]);
                animais.RemoveAt(animalIndex);
            }

            return animaisSorteados.ToArray();

        }
    }
}
