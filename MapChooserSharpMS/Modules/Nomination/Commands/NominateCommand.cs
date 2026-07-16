using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapConfig.Services;
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
    private IMcsMapSearchService _mapSearchService = null!;
    private IMcsReadOnlyVoteState _voteState = null!;
    private IMapTransitionManager _transitionManager = null!;

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        if (client is null) return;

        _controller ??= ServiceProvider.GetRequiredService<IMcsInternalNominationController>();
        _mapConfigProvider ??= ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();
        _mapSearchService ??= ServiceProvider.GetRequiredService<IMcsMapSearchService>();
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

        var searchResult = _mapSearchService.SearchMaps(query);

        if (searchResult.Status == McsMapSearchStatus.NotFound)
        {
            client.GetPlayerController()?.PrintToChat(
                LocalizeWithNominationPrefix(client, "Nomination.Command.Notification.NotMapsFound", query));
            return;
        }

        if (searchResult.Status == McsMapSearchStatus.MultipleFound)
        {
            client.GetPlayerController()?.PrintToChat(
                LocalizeWithNominationPrefix(client, "Nomination.Command.Notification.MultipleResult", searchResult.Maps.Count, query));
            _controller.NominationMenuManagementService.ShowNominationMenu(client, searchResult.Maps.ToList());
            return;
        }

        _controller.NominationMenuManagementService.NominateOrConfirm(client, searchResult.Maps[0], false);
    }

}
