using System;
using System.Linq;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Managers.MapTransition;
using MapChooserSharpMS.Shared.MapVote;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using MapChooserSharpMS.Modules.Commands;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;
using TnmsPluginFoundation.Extensions.Client;

namespace MapChooserSharpMS.Modules.Nomination.Commands;

internal sealed class RemoveNominationCommand(IServiceProvider provider) : McsCommandBase(provider)
{
    public override string CommandName => "nominate_removemap";
    public override string CommandDescription => "Admin: remove a map from nomination";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMcsInternalNominationController _controller = null!;
    private IMcsReadOnlyVoteState _voteState = null!;
    private IMcsMapConfigProvider _mapConfigProvider = null!;
    private IMapTransitionManager _transitionManager = null!;

    protected override ICommandValidator? GetValidator()
        => new PermissionValidator("mcs.admin.command.nomination.removemap");

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _controller ??= ServiceProvider.GetRequiredService<IMcsInternalNominationController>();
        _voteState ??= ServiceProvider.GetRequiredService<IMcsReadOnlyVoteState>();
        _mapConfigProvider ??= ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();
        _transitionManager ??= ServiceProvider.GetRequiredService<IMapTransitionManager>();

        if (_voteState.CurrentVoteState == McsMapVoteState.NextMapConfirmed)
        {
            string nextMapDisplay = _transitionManager.NextMap is { } nextMap
                ? _mapConfigProvider.ToolingService.ResolveMapDisplayName(nextMap)
                : LocalizeString(client, "Word.VotePending");
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "MapCycle.Command.Notification.NextMap", nextMapDisplay));
            return;
        }

        if (commandInfo.ArgCount < 1)
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "NominationRemoveMap.Command.Notification.Usage"));
            if (client is not null)
                _controller.NominationMenuManagementService.ShowRemoveNominationMenu(client);
            return;
        }

        string mapName = commandInfo[1];
        var nominations = _controller.NominationManager.NominatedMaps;
        var matched = nominations
            .Where(kv => kv.Key.Contains(mapName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matched.Count > 1)
        {
            var exact = matched.FirstOrDefault(kv =>
                string.Equals(kv.Key, mapName, StringComparison.OrdinalIgnoreCase));
            if (exact.Value is not null)
                matched = [exact];
        }

        if (matched.Count == 0)
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "Nomination.Command.Notification.NotMapsFound", mapName));
            return;
        }

        if (matched.Count > 1)
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "Nomination.Command.Notification.MultipleResult", matched.Count, mapName));
            if (client is not null)
                _controller.NominationMenuManagementService.ShowRemoveNominationMenu(client,
                    matched.Select(kv => kv.Value).ToList());
            return;
        }

        _controller.NominationService.TryRemoveNomination(matched[0].Value.MapConfig, client, forceRemoval: true);
    }

}
