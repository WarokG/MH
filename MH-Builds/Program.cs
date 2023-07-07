using Discord;
using Discord.WebSocket;
using Serilog;
using MHBuilds.Extensions;
using MHBuilds.Settings;

namespace MHBuilds;

public class Program
{
    public static async Task Main()
    {
        try
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("Logs/latest-.log", rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14)
                .CreateLogger();

            await BotVariables.Initialize();

            var builder = WebApplication.CreateBuilder();

            Console.Title = $"MH-Builds v.{BotVariables.Version}";

            builder.Host.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.File("Logs/latest-.log", rollingInterval: RollingInterval.Day);
            });

            builder.Host.UseDefaultServiceProvider(o => o.ValidateScopes = false);

            builder.Services
                .AddDiscord(
                    discordClient =>
                    {
                        discordClient.GatewayIntents = GatewayIntents.AllUnprivileged & ~GatewayIntents.GuildInvites &
                                                       ~GatewayIntents.GuildScheduledEvents;
                        discordClient.AlwaysDownloadUsers = false;
                    },
                    _ => { },
                    textCommandsService => { textCommandsService.CaseSensitiveCommands = false; },
                    builder.Configuration)
                .AddLogging(options => options.AddSerilog(dispose: true))
                .AddSingleton<LogAdapter<BaseSocketClient>>();

            await builder.Build().RunAsync();
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "Host terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}