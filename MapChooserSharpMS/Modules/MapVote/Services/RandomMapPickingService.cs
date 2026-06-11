using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.Nomination.Services;

namespace MapChooserSharpMS.Modules.MapVote.Services;

internal sealed class RandomMapPickingService(
    INominationValidateService nominationValidateService,
    IMcsPluginConfigProvider pluginConfigProvider,
    IMcsMapConfigProvider mapConfigProvider)
{
    public List<IMapConfig> PickRandomMaps(int amount = -1, ISet<string>? excludeMapNames = null)
    {
        if (amount == -1)
            amount = pluginConfigProvider.PluginConfig.VoteConfig.MaxMenuElements;

        var allMaps = mapConfigProvider.GetMapConfigs()
            .Where(kv => excludeMapNames is null || !excludeMapNames.Contains(kv.Key))
            .Select(kv => kv.Value.First().MapConfig)
            .OrderBy(_ => Random.Shared.Next())
            .ToList();

        var picked = new List<IMapConfig>();

        foreach (var mapConfig in allMaps)
        {
            if (picked.Count >= amount)
                break;

            if (nominationValidateService.CanPickupMap(mapConfig).Count == 0)
                picked.Add(mapConfig);
        }

        return picked;
    }
}
