using Discord.Interactions;
// ReSharper disable UnusedMember.Global

namespace MHBuilds.Commands.Interactions;

public class InfoCommands : InteractionModuleBase<ShardedInteractionContext>
{
    [RequireOwner]
    [SlashCommand("ping", "ta mere")]
    public async Task Ping()
    {
        await RespondAsync("pong");
    }
}