using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Managers.MapTransition;
using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.Nomination;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Extensions.Client;

namespace MapChooserSharpMS.Modules.Nomination.Commands;

internal sealed class NominateCommand(IServiceProvider provider) : NominationCommandBase(provider)
{
    public override string CommandName => "nominate";
    public override List<string> CommandAliases => ["nom"];
    public override string CommandDescription => "Nominate a map for next vote";
    public override TnmsCommandRegistrationType CommandRegistrationType => TnmsCommandRegistrationType.Client;

    private IMcsInternalNominationController _controller = null!;
    private IMcsMapConfigProvider _mapConfigProvider = null!;
    private IMcsReadOnlyVoteState _voteState = null!;
    private IMapTransitionManager _transitionManager = null!;

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        if (client is null) return;

        _controller ??= ServiceProvider.GetRequiredService<IMcsInternalNominationController>();
        _mapConfigProvider ??= ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();
        _voteState ??= ServiceProvider.GetRequiredService<IMcsReadOnlyVoteState>();
        _transitionManager ??= ServiceProvider.GetRequiredService<IMapTransitionManager>();

        if (_voteState.CurrentVoteState == McsMapVoteState.NextMapConfirmed)
        {
            string nextMapDisplay = _transitionManager.NextMap is { } nextMap
                ? _mapConfigProvider.ToolingService.ResolveMapDisplayName(nextMap.MapConfig)
                : LocalizeString(client, "Word.VotePending");
            client.GetPlayerController()?.PrintToChat(
                LocalizeWithNominationPrefix(client, "MapCycle.Command.Notification.NextMap", nextMapDisplay));
            return;
        }

        if (commandInfo.ArgCount < 1)
        {
            _controller.NominationMenuManagementService.ShowNominationMenu(client);
            return;
        }

        string query = commandInfo[1];

        var allMaps = _mapConfigProvider.GetMapConfigs();
        var matched = allMaps
            .Where(kv => kv.Key.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Value.First().MapConfig)
            .Where(m => !m.IsDisabled)
            .ToList();

        if (matched.Count > 1)
        {
            var exact = matched.FirstOrDefault(m =>
                string.Equals(m.MapName, query, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
                matched = [exact];
        }

        if (matched.Count == 0)
        {
            var allMapConfigs = allMaps
                .Select(kv => kv.Value.First().MapConfig)
                .Where(m => !m.IsDisabled);
            matched = _mapConfigProvider.ToolingService.FindMapsBySearchTag(query, allMapConfigs);
        }

        if (matched.Count == 0)
        {
            client.GetPlayerController()?.PrintToChat(
                LocalizeWithNominationPrefix(client, "Nomination.Command.Notification.NotMapsFound", query));
            return;
        }

        if (matched.Count > 1)
        {
            client.GetPlayerController()?.PrintToChat(
                LocalizeWithNominationPrefix(client, "Nomination.Command.Notification.MultipleResult", matched.Count, query));
            _controller.NominationMenuManagementService.ShowNominationMenu(client, matched);
            return;
        }

        _controller.NominationMenuManagementService.NominateOrConfirm(client, matched[0], false);
    }

}
