using System;
using System.Linq;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Shared.MapConfig.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;

namespace MapChooserSharpMS.Modules.Nomination.Commands;

internal sealed class MapListCommand(IServiceProvider provider) : NominationCommandBase(provider)
{
    public override string CommandName => "maplist";
    public override string CommandDescription => "Browse the map list with sortable map information";
    public override TnmsCommandRegistrationType CommandRegistrationType => TnmsCommandRegistrationType.Client;

    private IMcsInternalNominationController _controller = null!;
    private IMcsMapSearchService _mapSearchService = null!;

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        if (client is null) return;

        _controller ??= ServiceProvider.GetRequiredService<IMcsInternalNominationController>();
        _mapSearchService ??= ServiceProvider.GetRequiredService<IMcsMapSearchService>();

        if (commandInfo.ArgCount < 1)
        {
            _controller.NominationMenuManagementService.ShowMapListMenu(client);
            return;
        }

        string query = commandInfo[1];
        var searchResult = _mapSearchService.SearchMaps(query);

        if (searchResult.Status == McsMapSearchStatus.NotFound)
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "General.Notification.MapNotFound", query));
            return;
        }

        _controller.NominationMenuManagementService.ShowMapListMenu(client, searchResult.Maps.ToList());
    }
}
