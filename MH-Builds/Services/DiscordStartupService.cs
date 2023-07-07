﻿using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.Options;
using Serilog.Context;
using Serilog;
using System.Reflection;
using System.Text.Json;
using MHBuilds.Extensions;
using MHBuilds.Options;
using MHBuilds.Settings;
using MHBuilds.Util;

namespace MHBuilds.Services;

public class DiscordStartupService : BackgroundService
{
    private readonly LogAdapter<BaseSocketClient> _adapter;
    private readonly CommandService _commandService;
    private readonly IOptions<DiscordBotOptions> _discordBotOptions;
    private readonly DiscordShardedClient _discordShardedClient;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _serviceProvider;

    private int _shardsReady;
    private TaskCompletionSource<bool>? _taskCompletionSource;

    public DiscordStartupService(
        DiscordShardedClient discordShardedClient,
        IOptions<DiscordBotOptions> discordBotOptions,
        InteractionService interactionService,
        CommandService commandService,
        IServiceProvider serviceProvider,
        LogAdapter<BaseSocketClient> adapter)
    {
        _discordShardedClient = discordShardedClient;
        _discordBotOptions = discordBotOptions;
        _interactionService = interactionService;
        _commandService = commandService;
        _serviceProvider = serviceProvider;
        _adapter = adapter;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _discordShardedClient.Log += async logMessage => { await _adapter.Log(logMessage); };
            _discordShardedClient.ShardDisconnected += OnShardDisconnected;

            _discordShardedClient.MessageReceived += OnMessageReceived;

            _discordShardedClient.JoinedGuild += OnJoinedGuild;
            _discordShardedClient.LeftGuild += OnLeftGuild;

            _discordShardedClient.InteractionCreated += OnInteractionCreated;
            _interactionService.SlashCommandExecuted += OnSlashCommandExecuted;

            _discordShardedClient.UserJoined += HandleJoin;
            _discordShardedClient.UserLeft += HandleLeft;

            PrepareClientAwaiter();
            await _discordShardedClient.LoginAsync(TokenType.Bot, _discordBotOptions.Value.Token);
            await _discordShardedClient.StartAsync();

            await WaitForReadyAsync(stoppingToken);

            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

            if (BotVariables.IsDebug)
            {
                await _discordShardedClient.Rest.DeleteAllGlobalCommandsAsync();
                await _interactionService.RegisterCommandsToGuildAsync(684161801469952023); // disciples du noot
            }
            else
            {
                await _interactionService.RegisterCommandsGloballyAsync();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception in DiscordStartupService\n{e.GetType()}: {e.Message}");
        }
    }

    private static async Task OnJoinedGuild(SocketGuild arg)
    {
        // ignored
    }

    private static async Task OnLeftGuild(SocketGuild arg)
    {
        // ignored
    }

    private static async Task OnSlashCommandExecuted(SlashCommandInfo arg1, IInteractionContext arg2, Discord.Interactions.IResult result)
    {
        if (result.IsSuccess || !result.Error.HasValue)
            return;

        if (!arg2.Interaction.HasResponded)
            await arg2.Interaction.DeferAsync();

        var errorEmbed = Embeds.MakeErrorEmbed();
        errorEmbed.Title = "Failed to execute command.";

        errorEmbed.Description = BotVariables.ErrorMessage;

        var debugOptions = new List<string>();
        var options = ((SocketSlashCommand)arg2.Interaction).Data;
        if (options != null && options.Options.Count > 0)
        {
            var opt = options.Options.First();
            debugOptions.Add($"SubCommand = {opt.Name}");
            debugOptions.AddRange(opt.Options.Select(socketSlashCommandDataOption =>
                $"{socketSlashCommandDataOption.Name} = {socketSlashCommandDataOption.Value}"));
        }

        var errorMessage = $"{result.Error}: {result.ErrorReason}";

        errorEmbed.AddField("Command", $"```{options!.Name}```");
        errorEmbed.AddField("Parameters", $"```{JsonSerializer.Serialize(debugOptions)}```");
        errorEmbed.AddField("Error", $"```{errorMessage}```");

        using (LogContext.PushProperty("context", new
               {
                   Sender = arg2.User.ToString(),
                   CommandName = options.Name,
                   CommandParameters = JsonSerializer.Serialize(debugOptions),
                   ServerId = arg2.Interaction.GuildId ?? 0
               }))
        {
            Log.Error(errorMessage);
        }

        await arg2.Interaction.FollowupAsync(embed: errorEmbed.Build());
    }

    private async Task OnMessageReceived(SocketMessage socketMessage)
    {
        if (socketMessage is not SocketUserMessage socketUserMessage)
            return;

        var argPos = 0;
        if (!socketUserMessage.HasStringPrefix("mh!", ref argPos))
            return;

        if (_discordBotOptions.Value.BotStaff != null &&
            !_discordBotOptions.Value.BotStaff.Contains(socketMessage.Author.Id))
            return;

        var context = new ShardedCommandContext(_discordShardedClient, socketUserMessage);
        var command = await _commandService.ExecuteAsync(context, argPos, _serviceProvider);

        if (command.Error != null)
            Log.Error($"{command.Error}: {command.ErrorReason}");
    }

    private async Task OnInteractionCreated(SocketInteraction socketInteraction)
    {
        var shardedInteractionContext = new ShardedInteractionContext(_discordShardedClient, socketInteraction);
        await _interactionService.ExecuteCommandAsync(shardedInteractionContext, _serviceProvider);
    }

    private async Task HandleJoin(SocketGuildUser arg)
    {
        // ignored  
    }

    private async Task HandleLeft(SocketGuild arg1, SocketUser arg2)
    {
        // ignored
    }

    private void PrepareClientAwaiter()
    {
        _taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _shardsReady = 0;

        _discordShardedClient.ShardReady += OnShardReady;
    }

    private Task OnShardReady(DiscordSocketClient discordClient)
    {
        Log.Information(
            $"Connected as {discordClient.CurrentUser.Username}#{discordClient.CurrentUser.DiscriminatorValue}");
        BotVariables.DiscordLogChannel ??=
            (SocketTextChannel)discordClient.GetChannel(1126478491165134878); // noot => test

        _shardsReady++;

        if (_shardsReady != _discordShardedClient.Shards.Count)
            return Task.CompletedTask;

        _taskCompletionSource!.TrySetResult(true);
        _discordShardedClient.ShardReady -= OnShardReady;

        return Task.CompletedTask;
    }

    private static async Task OnShardDisconnected(Exception arg1, DiscordSocketClient arg2)
    {
        Log.Error(arg1, "Disconnected from gateway.");

        if (arg1.InnerException is GatewayReconnectException &&
            arg1.InnerException.Message == "Server missed last heartbeat")
        {
            await arg2.StopAsync();
            await Task.Delay(10000);
            await arg2.StartAsync();
        }
    }

    private Task WaitForReadyAsync(CancellationToken cancellationToken)
    {
        if (_taskCompletionSource is null)
            throw new InvalidOperationException(
                "The sharded client has not been registered correctly. Did you use ConfigureDiscordShardedHost on your HostBuilder?");

        if (_taskCompletionSource.Task.IsCompleted)
            return _taskCompletionSource.Task;

        var registration = cancellationToken.Register(
            state => { ((TaskCompletionSource<bool>)state!).TrySetResult(true); },
            _taskCompletionSource);

        return _taskCompletionSource.Task.ContinueWith(_ => registration.DisposeAsync(), cancellationToken);
    }
}