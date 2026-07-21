using System;
using System.Linq;
using MapChooserSharpMS.Modules.Commands;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.MapCycle;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle;
using MapChooserSharpMS.Shared.MapCycle.Services;
using MapChooserSharpMS.Shared.Nomination.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Command;

namespace MapChooserSharpMS.Modules.MapCycle.Commands;

internal sealed class MapInfoCommand(IServiceProvider provider) : McsCommandBase(provider)
{
    public override string CommandName => "mapinfo";
    public override string CommandDescription => "Show map information";
    public override TnmsCommandRegistrationType CommandRegistrationType => TnmsCommandRegistrationType.Client;

    private IMapCycleController _controller = null!;
    private IMcsMapConfigProvider _mapConfigProvider = null!;
    private INominationValidateService _nominationValidateService = null!;
    private IInternalEventManager _eventManager = null!;
    private IMapCooldownQueryService _cooldownQueryService = null!;

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        if (client is null)
            return;

        _controller ??= ServiceProvider.GetRequiredService<IMapCycleController>();
        _mapConfigProvider ??= ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();
        _nominationValidateService ??= ServiceProvider.GetRequiredService<INominationValidateService>();
        _eventManager ??= ServiceProvider.GetRequiredService<IInternalEventManager>();
        _cooldownQueryService ??= ServiceProvider.GetRequiredService<IMapCooldownQueryService>();

        if (commandInfo.ArgCount < 1)
        {
            var currentMap = _controller.MapTransitionManager.CurrentMap?.MapConfig;
            if (currentMap is null)
            {
                Print(client, "MapCycle.Command.Notification.MapInfo.NotAvailable");
                return;
            }

            PrintMapInfo(client, currentMap, commandInfo);
            return;
        }

        ResolveMapAndExecute(client, commandInfo[1], (c, map) =>
        {
            if (c is not null)
                PrintMapInfo(c, map, commandInfo);
        });
    }

    private void PrintMapInfo(IGameClient client, IMapConfig mapConfig, StringCommand commandInfo)
    {
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

        if (mapConfig.SearchTags.Count > 0)
            Print(client, "MapCycle.Command.Notification.MapInfo.SearchTags",
                string.Join(", ", mapConfig.SearchTags));

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

        var cooldownDetails = _cooldownQueryService.GetCurrentCooldowns(mapConfig);
        bool hasCount = cooldownDetails.HighestCooldownCount > 0;
        bool hasTimed = cooldownDetails.LongestTimedCooldown > DateTime.UtcNow;

        if (hasCount && hasTimed)
        {
            string timedStr = cooldownDetails.LongestTimedCooldown.ToLocalTime().ToString("yyyy/MM/dd HH:mm");
            Print(client, "MapCycle.Command.Notification.MapInfo.CooldownWithTimed",
                cooldownDetails.HighestCooldownCount, timedStr);
        }
        else if (hasCount)
        {
            Print(client, "MapCycle.Command.Notification.MapInfo.Cooldown", cooldownDetails.HighestCooldownCount);
        }
        else if (hasTimed)
        {
            string timedStr = cooldownDetails.LongestTimedCooldown.ToLocalTime().ToString("yyyy/MM/dd HH:mm");
            Print(client, "MapCycle.Command.Notification.MapInfo.TimedCooldown", timedStr);
        }

        var cooldownState = _controller.CooldownStore.GetEffectiveMapState(mapConfig.MapName);

        bool hasNomCount = cooldownState.CurrentNominationCooldown > 0;
        bool hasNomTimed = cooldownState.NominationTimedCooldownEndUtc > DateTime.UtcNow;

        if (hasNomCount && hasNomTimed)
        {
            string nomTimedStr = cooldownState.NominationTimedCooldownEndUtc.ToLocalTime().ToString("yyyy/MM/dd HH:mm");
            Print(client, "MapCycle.Command.Notification.MapInfo.NomCooldownWithTimed",
                cooldownState.CurrentNominationCooldown, nomTimedStr);
        }
        else if (hasNomCount)
        {
            Print(client, "MapCycle.Command.Notification.MapInfo.NomCooldown", cooldownState.CurrentNominationCooldown);
        }
        else if (hasNomTimed)
        {
            string nomTimedStr = cooldownState.NominationTimedCooldownEndUtc.ToLocalTime().ToString("yyyy/MM/dd HH:mm");
            Print(client, "MapCycle.Command.Notification.MapInfo.NomTimedCooldown", nomTimedStr);
        }

        if (cooldownState.UnplayedCount > 0)
            Print(client, "MapCycle.Command.Notification.MapInfo.UnplayedCount", cooldownState.UnplayedCount);

        var playerCdState = _nominationValidateService.GetPlayerCooldownState(client.SteamId);

        if (playerCdState is not null)
        {
            bool hasPlayerCount = playerCdState.RemainingCount > 0;
            bool hasPlayerTimed = playerCdState.CooldownUntil > DateTime.UtcNow;

            if (hasPlayerCount && hasPlayerTimed)
            {
                string timedStr = playerCdState.CooldownUntil.ToLocalTime().ToString("yyyy/MM/dd HH:mm");
                Print(client, "MapCycle.Command.Notification.MapInfo.PlayerNomCooldownWithTimed",
                    playerCdState.RemainingCount, timedStr);
            }
            else if (hasPlayerCount)
            {
                Print(client, "MapCycle.Command.Notification.MapInfo.PlayerNomCooldown",
                    playerCdState.RemainingCount);
            }
            else if (hasPlayerTimed)
            {
                string timedStr = playerCdState.CooldownUntil.ToLocalTime().ToString("yyyy/MM/dd HH:mm");
                Print(client, "MapCycle.Command.Notification.MapInfo.PlayerNomTimedCooldown", timedStr);
            }
        }

        var checkResults = _nominationValidateService.PlayerCanNominateMap(client, mapConfig);
        string canNominate = checkResults.Count == 0
            ? $"{LocalizeString(client, "Word.Yes")} {LocalizeString(client, "Word.MapInfo.NominationCheck.Success")}"
            : $"{LocalizeString(client, "Word.No")} {LocalizeString(client, $"Word.MapInfo.NominationCheck.{checkResults[0]}")}";
        Print(client, "MapCycle.Command.Notification.MapInfo.YouCanNominate", canNominate);

        if (_controller is TnmsPluginFoundation.Models.Plugin.PluginModuleBase moduleBase)
        {
            var eventParams = new MapInfoCommandExecutedParams(Plugin, moduleBase, client, commandInfo, mapConfig);
            _eventManager.Fire<IMapCycleEventListener>(e => e.OnMapInfoCommandExecuted(eventParams));
        }
    }

    private void Print(IGameClient client, string key, params object[] args)
    {
        client.GetPlayerController()?.PrintToChat(LocalizeWithPluginPrefix(client, key, args));
    }
}
