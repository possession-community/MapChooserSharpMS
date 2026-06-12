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

internal sealed class NominateCommand(IServiceProvider provider) : TnmsAbstractCommandBase(provider)
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
                ? _mapConfigProvider.ToolingService.ResolveMapDisplayName(nextMap)
                : LocalizeString(client, "Word.VotePending");
            client.GetPlayerController()?.PrintToChat(
                LocalizeWithPluginPrefix(client, "MapCycle.Command.Notification.NextMap", nextMapDisplay));
            return;
        }

        if (commandInfo.ArgCount < 2)
        {
            client.GetPlayerController()?.PrintToChat(
                LocalizeWithPluginPrefix(client, "Nomination.Command.Notification.Usage"));
            _controller.NominationMenuManagementService.ShowNominationMenu(client);
            return;
        }

        string mapName = commandInfo[1];

        if (_mapConfigProvider.TryGetMapConfig(mapName, out var exactMatch))
        {
            var result = _controller.NominationService.TryNominateMap(client, exactMatch);
            if (result.Count > 0)
                NotifyNominationFailure(client, exactMatch, result);
            return;
        }

        var allMaps = _mapConfigProvider.GetMapConfigs();
        var matched = allMaps
            .Where(kv => kv.Key.Contains(mapName, StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Value.First().MapConfig)
            .Where(m => !m.IsDisabled)
            .ToList();

        if (matched.Count == 0)
        {
            client.GetPlayerController()?.PrintToChat(
                LocalizeWithPluginPrefix(client, "Nomination.Command.Notification.NotMapsFound", mapName));
            return;
        }

        if (matched.Count > 1)
        {
            client.GetPlayerController()?.PrintToChat(
                LocalizeWithPluginPrefix(client, "Nomination.Command.Notification.MultipleResult", matched.Count, mapName));
            _controller.NominationMenuManagementService.ShowNominationMenu(client, matched);
            return;
        }

        var nominateResult = _controller.NominationService.TryNominateMap(client, matched[0]);
        if (nominateResult.Count > 0)
            NotifyNominationFailure(client, matched[0], nominateResult);
    }

    private void NotifyNominationFailure(IGameClient client, IMapConfig mapConfig, IReadOnlyList<NominationCheckResult> results)
    {
        // Delegate to the controller's existing ProcessNominationCheckResult logic
        // by calling NominationService — the controller already has chat notification
        // wired in ProcessNominationCheckResult. The service returned failures,
        // which the command layer should display. For now, show the first failure.
        string mapDisplay = _mapConfigProvider.ToolingService.ResolveMapDisplayName(mapConfig);
        foreach (var r in results)
        {
            client.GetPlayerController()?.PrintToChat(
                LocalizeWithPluginPrefix(client, $"Nomination.Notification.Failure.{r}", mapDisplay));
        }
    }

}
