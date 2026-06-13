using System;
using System.Linq;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle;
using MapChooserSharpMS.Shared.Nomination.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Command;

namespace MapChooserSharpMS.Modules.MapCycle.Commands;

internal sealed class MapInfoCommand(IServiceProvider provider) : TnmsAbstractCommandBase(provider)
{
    public override string CommandName => "mapinfo";
    public override string CommandDescription => "Show map information";
    public override TnmsCommandRegistrationType CommandRegistrationType => TnmsCommandRegistrationType.Client;

    private IMapCycleController _controller = null!;
    private IMcsMapConfigProvider _mapConfigProvider = null!;
    private INominationValidateService _nominationValidateService = null!;

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        if (client is null)
            return;

        _controller ??= ServiceProvider.GetRequiredService<IMapCycleController>();
        _mapConfigProvider ??= ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();
        _nominationValidateService ??= ServiceProvider.GetRequiredService<INominationValidateService>();

        IMapConfig? mapConfig;
        if (commandInfo.ArgCount < 1)
        {
            mapConfig = _controller.MapTransitionManager.CurrentMap;
        }
        else
        {
            _mapConfigProvider.TryGetMapConfig(commandInfo[1], out mapConfig!);
        }

        if (mapConfig is null)
        {
            Print(client, "MapCycle.Command.Notification.MapInfo.NotAvailable");
            return;
        }

        Print(client, "MapCycle.Command.Notification.MapInfo", mapConfig.MapName);

        if (mapConfig.MapNameAlias != string.Empty)
            Print(client, "MapCycle.Command.Notification.MapInfo.AliasName", mapConfig.MapNameAlias);

        if (mapConfig.MapDescription != string.Empty)
            Print(client, "MapCycle.Command.Notification.MapInfo.Description", mapConfig.MapDescription);

        if (mapConfig.MaxExtends > 0)
            Print(client, "MapCycle.Command.Notification.MapInfo.MaxExtends", mapConfig.MaxExtends);

        if (mapConfig.WorkshopId > 0)
            Print(client, "MapCycle.Command.Notification.MapInfo.WorkshopId", mapConfig.WorkshopId);

        if (mapConfig.GroupSettings.Count > 0)
            Print(client, "MapCycle.Command.Notification.MapInfo.Groups",
                string.Join(", ", mapConfig.GroupSettings.Select(g => g.GroupName)));

        if (mapConfig.NominationConfig.DaysAllowed.Count > 0)
            Print(client, "MapCycle.Command.Notification.MapInfo.DaysAllowed",
                string.Join(", ", mapConfig.NominationConfig.DaysAllowed));

        if (mapConfig.NominationConfig.AllowedTimeRanges.Count > 0)
            Print(client, "MapCycle.Command.Notification.MapInfo.AllowedTimeRanges",
                string.Join(", ", mapConfig.NominationConfig.AllowedTimeRanges));

        if (mapConfig.NominationConfig.MaxPlayers > 0)
            Print(client, "MapCycle.Command.Notification.MapInfo.MaxPlayers", mapConfig.NominationConfig.MaxPlayers);

        if (mapConfig.NominationConfig.MinPlayers > 0)
            Print(client, "MapCycle.Command.Notification.MapInfo.MinPlayers", mapConfig.NominationConfig.MinPlayers);

        int highestCooldown = _mapConfigProvider.ToolingService.GetHighestCooldown(mapConfig);
        if (highestCooldown > 0)
            Print(client, "MapCycle.Command.Notification.MapInfo.Cooldown", highestCooldown);

        var checkResults = _nominationValidateService.PlayerCanNominateMap(client, mapConfig);
        string canNominate = checkResults.Count == 0
            ? $"{LocalizeString(client, "Word.Yes")} {LocalizeString(client, "Word.MapInfo.NominationCheck.Success")}"
            : $"{LocalizeString(client, "Word.No")} {LocalizeString(client, $"Word.MapInfo.NominationCheck.{checkResults[0]}")}";
        Print(client, "MapCycle.Command.Notification.MapInfo.YouCanNominate", canNominate);
    }

    private void Print(IGameClient client, string key, params object[] args)
    {
        client.GetPlayerController()?.PrintToChat(LocalizeWithPluginPrefix(client, key, args));
    }
}
