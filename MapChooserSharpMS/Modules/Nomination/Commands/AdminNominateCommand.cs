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

internal sealed class AdminNominateCommand(IServiceProvider provider) : McsCommandBase(provider)
{
    public override string CommandName => "nominate_addmap";
    public override string CommandDescription => "Admin: force-add a map to nomination";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMcsInternalNominationController _controller = null!;
    private IMcsMapConfigProvider _mapConfigProvider = null!;
    private IMcsReadOnlyVoteState _voteState = null!;
    private IMapTransitionManager _transitionManager = null!;

    protected override ICommandValidator? GetValidator()
        => new PermissionValidator("mcs.admin.command.nomination.addmap");

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _controller ??= ServiceProvider.GetRequiredService<IMcsInternalNominationController>();
        _mapConfigProvider ??= ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();
        _voteState ??= ServiceProvider.GetRequiredService<IMcsReadOnlyVoteState>();
        _transitionManager ??= ServiceProvider.GetRequiredService<IMapTransitionManager>();

        if (_voteState.CurrentVoteState == McsMapVoteState.NextMapConfirmed)
        {
            string nextMapDisplay = _transitionManager.NextMap is { } nextMap
                ? _mapConfigProvider.ToolingService.ResolveMapDisplayName(nextMap.MapConfig)
                : LocalizeString(client, "Word.VotePending");
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "MapCycle.Command.Notification.NextMap", nextMapDisplay));
            return;
        }

        if (commandInfo.ArgCount < 1)
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "NominationAddMap.Command.Notification.Usage"));
            if (client is not null)
                _controller.NominationMenuManagementService.ShowAdminNominationMenu(client);
            return;
        }

        string mapName = commandInfo[1];

        var allMaps = _mapConfigProvider.GetMapConfigs();
        var matched = allMaps
            .Where(kv => kv.Key.Contains(mapName, StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Value.First().MapConfig)
            .ToList();

        if (matched.Count > 1)
        {
            var exact = matched.FirstOrDefault(m =>
                string.Equals(m.MapName, mapName, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
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
                _controller.NominationMenuManagementService.ShowAdminNominationMenu(client, matched);
            return;
        }

        var nominateResult = _controller.NominationService.TryAdminNominateMap(client, matched[0]);
        if (nominateResult.Count > 0)
            _controller.NotifyNominationFailure(client, matched[0], nominateResult);
    }

}
