using System;
using MapChooserSharpMS.Shared.RockTheVote;
using MapChooserSharpMS.Shared.RockTheVote.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Command;

namespace MapChooserSharpMS.Modules.RockTheVote.Commands;

internal sealed class RtvCommand(IServiceProvider provider) : TnmsAbstractCommandBase(provider)
{
    public override string CommandName => "rtv";
    public override string CommandDescription => "Rock the vote — request a map change";
    public override TnmsCommandRegistrationType CommandRegistrationType => TnmsCommandRegistrationType.Client;

    private IRtvService _rtvService = null!;

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        if (client is null)
            return;

        _rtvService ??= ServiceProvider.GetRequiredService<IRtvService>();

        var result = _rtvService.AddClientToRtv(client);

        switch (result)
        {
            case RtvExecutionResult.Success:
                break;

            case RtvExecutionResult.AlreadyVoted:
                client.GetPlayerController()?.PrintToChat(
                    LocalizeWithPluginPrefix(client, "Rtv.Notification.AlreadyVoted"));
                break;

            case RtvExecutionResult.CommandInCooldown:
                client.GetPlayerController()?.PrintToChat(
                    LocalizeWithPluginPrefix(client, "Rtv.Notification.InCooldown"));
                break;

            case RtvExecutionResult.CommandDisabled:
                client.GetPlayerController()?.PrintToChat(
                    LocalizeWithPluginPrefix(client, "Rtv.Notification.Disabled"));
                break;

            case RtvExecutionResult.AnotherVoteOngoing:
                client.GetPlayerController()?.PrintToChat(
                    LocalizeWithPluginPrefix(client, "Rtv.Notification.AnotherVoteOngoing"));
                break;

            case RtvExecutionResult.TriggeredWaitingForVote:
            case RtvExecutionResult.TriggeredWaitingForMapTransition:
                client.GetPlayerController()?.PrintToChat(
                    LocalizeWithPluginPrefix(client, "Rtv.Notification.AlreadyTriggered"));
                break;

            case RtvExecutionResult.DisallowedByExternalConsumer:
                break;
        }
    }
}
