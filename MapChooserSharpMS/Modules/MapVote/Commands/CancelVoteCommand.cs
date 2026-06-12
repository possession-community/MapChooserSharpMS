using System;
using MapChooserSharpMS.Modules.Commands;
using MapChooserSharpMS.Modules.MapVote.Interfaces;
using MapChooserSharpMS.Shared.MapVote;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;

namespace MapChooserSharpMS.Modules.MapVote.Commands;

internal sealed class CancelVoteCommand(IServiceProvider provider) : McsCommandBase(provider)
{
    public override string CommandName => "cancelvote";
    public override string CommandDescription => "Admin: cancel the ongoing map vote";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMcsInternalVoteController _voteController = null!;
    private IMcsReadOnlyVoteState _voteState = null!;

    protected override ICommandValidator? GetValidator()
        => new PermissionValidator("mcs.admin.command.mapvote.cancelvote");

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _voteController ??= ServiceProvider.GetRequiredService<IMcsInternalVoteController>();
        _voteState ??= ServiceProvider.GetRequiredService<IMcsReadOnlyVoteState>();

        var state = _voteState.CurrentVoteState;

        if (state == McsMapVoteState.NextMapConfirmed)
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "MapVote.Command.Admin.CancelVote.NextMapAlreadyConfirmed"));
            return;
        }

        if (state is not (McsMapVoteState.Initializing
            or McsMapVoteState.InitializeAccepted
            or McsMapVoteState.Voting
            or McsMapVoteState.RunoffVoting
            or McsMapVoteState.Finalizing))
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "MapVote.Command.Notification.NoActiveVote"));
            return;
        }

        _voteController.MapVoteControllingService.CancelVote(client);

        string executorName = client?.Name ?? "Console";
        PrintLocalizedChatToAll("MapVote.Broadcast.Admin.CancelVote", executorName);
        Logger.LogInformation("Admin {Executor} cancelled the map vote", executorName);
    }
}
