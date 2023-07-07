using System.Diagnostics;
using Discord.WebSocket;

namespace MHBuilds.Settings
{
    public class BotVariables
    {
        internal static bool IsDebug;
        internal static string? Version;

        public const string DiscordInvite = "https://s.moons.bio/d-noot";
        
        internal const string ErrorMessage = "<:cheh:933084863194628128> <:NOOOOOOOOOOOOOT:855149582177533983>";
        public static SocketTextChannel? DiscordLogChannel { get; set; }

        public static Task Initialize()
        {
            if (Debugger.IsAttached)
            {
                IsDebug = true;
                Version = "dev";
            }
            else
            {
                Version = "prod";
            }

            return Task.CompletedTask;
        }

        public class Images
        {
            public const string Avatar = "https://cdn.tryfelicity.one/images/TwoMoons/avatar.png";
            public const string SadFace = "https://cdn.tryfelicity.one/images/peepoSad.png";
        }
    }
}
