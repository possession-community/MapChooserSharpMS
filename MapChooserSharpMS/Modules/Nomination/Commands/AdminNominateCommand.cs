using System;
using System.Linq;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapVote;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;
using TnmsPluginFoundation.Extensions.Client;

namespace MapChooserSharpMS.Modules.Nomination.Commands;

internal sealed class AdminNominateCommand(IServiceProvider provider) : TnmsAbstractCommandBase(provider)
{
    public override string CommandName => "nominate_addmap";
    public override string CommandDescription => "Admin: force-add a map to nomination";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMcsInternalNominationController _controller = null!;
    private IMcsMapConfigProvider _mapConfigProvider = null!;
    private IMcsReadOnlyVoteState _voteState = null!;

    protected override ICommandValidator? GetValidator()
        => new CompositeValidator()
            .Add(new PermissionValidator("mcs.admin.nominate"))
            .Add(new ArgumentCountValidator(2));

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _controller ??= ServiceProvider.GetRequiredService<IMcsInternalNominationController>();
        _mapConfigProvider ??= ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();
        _voteState ??= ServiceProvider.GetRequiredService<IMcsReadOnlyVoteState>();

        if (_voteState.CurrentVoteState == McsMapVoteState.NextMapConfirmed)
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "MapCycle.Command.Notification.NextMap"));
            return;
        }

        string mapName = commandInfo[1];

        if (_mapConfigProvider.TryGetMapConfig(mapName, out var exactMatch))
        {
            _controller.NominationService.TryAdminNominateMap(client, exactMatch);
            return;
        }

        var allMaps = _mapConfigProvider.GetMapConfigs();
        var matched = allMaps
            .Where(kv => kv.Key.Contains(mapName, StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Value.First().MapConfig)
            .ToList();

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
            return;
        }

        _controller.NominationService.TryAdminNominateMap(client, matched[0]);
    }

    private T GetRequiredService<T>() where T : notnull
        => (T)ServiceProvider.GetService(typeof(T))!;
}
