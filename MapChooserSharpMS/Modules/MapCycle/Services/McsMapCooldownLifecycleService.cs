using System;
using System.Collections.Generic;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.MapCycle;
using MapChooserSharpMS.Modules.MapConfig.Models;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.MapConfig;
using Microsoft.Extensions.Logging;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.MapCycle.Services;

internal sealed class McsMapCooldownLifecycleService
{
    private readonly ILogger _logger;
    private readonly TnmsPlugin _plugin;
    private readonly PluginModuleBase _moduleBase;
    private readonly IMcsMapConfigProvider _mapConfigProvider;
    private readonly IInternalEventManager _eventManager;

    internal McsMapCooldownLifecycleService(
        ILogger logger,
        TnmsPlugin plugin,
        PluginModuleBase moduleBase,
        IMcsMapConfigProvider mapConfigProvider,
        IInternalEventManager eventManager)
    {
        _logger = logger;
        _plugin = plugin;
        _moduleBase = moduleBase;
        _mapConfigProvider = mapConfigProvider;
        _eventManager = eventManager;
    }

    internal void ApplyPlayedMapCooldown(IMapConfig playedMap)
    {
        if (playedMap.CooldownConfig is CooldownConfig mapCc)
        {
            var eventParams = new MapCooldownApplyEventParams(
                _plugin, _moduleBase, playedMap,
                mapCc.ConfigCooldown,
                mapCc.TimedCooldown);

            _eventManager.Fire<IMapCycleEventListener>(e => e.OnMapCooldownApply(eventParams));

            if (eventParams.IsCancelled)
            {
                _logger.LogInformation("[Cooldown] Cooldown apply cancelled by listener for: {Map}", playedMap.MapName);
                return;
            }

            mapCc.CurrentCooldown = eventParams.Cooldown;
            mapCc.LastPlayedAt = DateTime.UtcNow;

            if (eventParams.TimedCooldownDuration > TimeSpan.Zero)
                mapCc.TimedCooldownEndUtc = DateTime.UtcNow + eventParams.TimedCooldownDuration;
        }

        foreach (var group in playedMap.GroupSettings)
        {
            if (group.CooldownConfig is not CooldownConfig groupCc)
                continue;

            groupCc.CurrentCooldown = groupCc.ConfigCooldown;
            groupCc.LastPlayedAt = DateTime.UtcNow;

            if (groupCc.TimedCooldown > TimeSpan.Zero)
                groupCc.TimedCooldownEndUtc = DateTime.UtcNow + groupCc.TimedCooldown;
        }

        _logger.LogInformation("[Cooldown] Applied cooldown to played map: {Map}", playedMap.MapName);
    }

    internal void DecrementAllCooldowns()
    {
        var decremented = new HashSet<ICooldownConfig>(ReferenceEqualityComparer.Instance);

        foreach (var entry in _mapConfigProvider.GetMapConfigs())
        {
            foreach (var mapEntry in entry.Value)
            {
                if (decremented.Add(mapEntry.MapConfig.CooldownConfig))
                    DecrementCooldownConfig(mapEntry.MapConfig.CooldownConfig);

                foreach (var group in mapEntry.MapConfig.GroupSettings)
                {
                    if (decremented.Add(group.CooldownConfig))
                        DecrementCooldownConfig(group.CooldownConfig);
                }
            }
        }
    }

    internal void ApplyNominationCooldown(IMapConfig map)
    {
        if (map.CooldownConfig is not CooldownConfig cc)
            return;

        if (cc.ConfigNominationCooldown > 0)
            cc.CurrentNominationCooldown = cc.ConfigNominationCooldown;

        if (cc.NominationTimedCooldown > TimeSpan.Zero)
            cc.NominationTimedCooldownEndUtc = DateTime.UtcNow + cc.NominationTimedCooldown;
    }

    private static void DecrementCooldownConfig(ICooldownConfig config)
    {
        if (config is not CooldownConfig cc)
            return;

        if (cc.CurrentCooldown > 0 && cc.CurrentCooldown < int.MaxValue)
            cc.CurrentCooldown--;

        if (cc.CurrentNominationCooldown > 0 && cc.CurrentNominationCooldown < int.MaxValue)
            cc.CurrentNominationCooldown--;
    }
}
