using Discord;

namespace MHBuilds.Options;

public class DiscordBotOptions
{
    public string? Token { get; set; }
    public ulong[]? BotStaff { get; set; }
    public Func<LogMessage, Exception?, string> LogFormat { get; set; } =
        (message, _) => $"{message.Source}: {message.Message}";
}
