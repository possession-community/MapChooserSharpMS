using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.MapConfig.Models;
using MapChooserSharpMS.Modules.Nomination.Models;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Services;
using MapChooserSharpMS.Shared.Nomination;

namespace MapChooserSharpMS.Modules.MapCycle.Services;

internal sealed class McsMapCooldownQueryService : IMapCooldownQueryService
{
    public IDetailedCooldownResult GetCurrentCooldowns(IMapConfig mapConfig)
    {
        int mapCooldown = mapConfig.CooldownConfig.CurrentCooldown;
        var mapTimedEnd = GetTimedCooldownEnd(mapConfig.CooldownConfig);

        var groupCooldowns = new Dictionary<string, int>();
        var groupTimedCooldowns = new Dictionary<string, DateTime>();

        foreach (var group in mapConfig.GroupSettings)
        {
            groupCooldowns[group.GroupName] = group.CooldownConfig.CurrentCooldown;
            groupTimedCooldowns[group.GroupName] = GetTimedCooldownEnd(group.CooldownConfig);
        }

        return new DetailedCooldownResult(mapConfig, mapCooldown, groupCooldowns, mapTimedEnd, groupTimedCooldowns);
    }

    public Task<IDetailedCooldownResult?> QueryCurrentCooldowns(IMapConfig mapConfig)
    {
        // TODO: DB query when SurrealDB is wired
        return Task.FromResult<IDetailedCooldownResult?>(GetCurrentCooldowns(mapConfig));
    }

    internal static DateTime GetTimedCooldownEnd(ICooldownConfig config)
    {
        if (config is CooldownConfig cc && cc.TimedCooldownEndUtc > DateTime.MinValue)
            return cc.TimedCooldownEndUtc;

        if (config.LastPlayedAt > DateTime.MinValue && config.TimedCooldown > TimeSpan.Zero)
            return config.LastPlayedAt + config.TimedCooldown;

        return DateTime.MinValue;
    }
}
