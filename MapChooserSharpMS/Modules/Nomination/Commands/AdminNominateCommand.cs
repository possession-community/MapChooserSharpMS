using System;
using System.Linq;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapConfig.Services;
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

internal sealed class AdminNominateCommand(IServiceProvider provider) : NominationCommandBase(provider)
{
    public override string CommandName => "nominate_addmap";
    public override string CommandDescription => "Admin: force-add a map to nomination";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMcsInternalNominationController _controller = null!;
    private IMcsMapConfigProvider _mapConfigProvider = null!;
    private IMcsMapSearchService _mapSearchService = null!;
    private IMcsReadOnlyVoteState _voteState = null!;
    private IMapTransitionManager _transitionManager = null!;

    protected override ICommandValidator? GetValidator()
        => new PermissionValidator("mcs.admin.command.nomination.addmap");

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
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
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithNominationPrefix(client, "MapCycle.Command.Notification.NextMap", nextMapDisplay));
            return;
        }

        if (commandInfo.ArgCount < 1)
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithNominationPrefix(client, "NominationAddMap.Command.Notification.Usage"));
            if (client is not null)
                _controller.NominationMenuManagementService.ShowAdminNominationMenu(client);
            return;
        }

        string mapName = commandInfo[1];

        // Admins may force-nominate disabled maps, so the disabled filter is off.
        var searchResult = _mapSearchService.SearchMaps(mapName, includeDisabledMaps: true);

        if (searchResult.Status == McsMapSearchStatus.NotFound)
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithNominationPrefix(client, "Nomination.Command.Notification.NotMapsFound", mapName));
            return;
        }

        if (searchResult.Status == McsMapSearchStatus.MultipleFound)
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithNominationPrefix(client, "Nomination.Command.Notification.MultipleResult", searchResult.Maps.Count, mapName));
            if (client is not null)
                _controller.NominationMenuManagementService.ShowAdminNominationMenu(client, searchResult.Maps.ToList());
            return;
        }

        var nominateResult = _controller.NominationService.TryAdminNominateMap(client, searchResult.Maps[0]);
        if (nominateResult.Count > 0)
            _controller.NotifyNominationFailure(client, searchResult.Maps[0], nominateResult);
    }

}
