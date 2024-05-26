using System.Reflection;
using Discord.Interactions;

namespace CastorDJ.Utils
{
    public static class ReflectionHelper
    {

        public static List<CommandInfo> GetCommands()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var classes = assembly.GetTypes().Where(x => x.Namespace == "CastorDJ.Modules" && x.IsSubclassOf(typeof(InteractionModuleBase<SocketInteractionContext>)));
            var commands = new List<CommandInfo>();

            foreach (var c in classes)
            {
                var methods = c.GetMethods().Where(x => x.GetCustomAttribute<SlashCommandAttribute>() != null);

                foreach (var m in methods)
                {
                    var attr = m.GetCustomAttribute<SlashCommandAttribute>();
                    commands.Add(new CommandInfo
                    {
                        Module = c.Name,
                        Name = attr.Name,
                        Description = attr.Description
                    });
                }
            }

            return commands;
        }

    }

    public class CommandInfo
    {
        public string Module { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}