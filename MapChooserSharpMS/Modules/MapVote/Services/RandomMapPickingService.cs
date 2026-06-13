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

        var candidates = mapConfigProvider.GetMapConfigs()
            .Where(kv => excludeMapNames is null || !excludeMapNames.Contains(kv.Key))
            .Select(kv => kv.Value.First().MapConfig)
            .Where(m => nominationValidateService.CanPickupMap(m).Count == 0)
            .ToList();

        return WeightedShuffle(candidates, amount);
    }

    private static List<IMapConfig> WeightedShuffle(List<IMapConfig> candidates, int amount)
    {
        var pool = candidates
            .Select(m => (Config: m, Weight: Math.Max(m.RandomPickConfig.MapSelectionWeight, 1u)))
            .ToList();

        var picked = new List<IMapConfig>();

        while (picked.Count < amount && pool.Count > 0)
        {
            uint totalWeight = 0;
            foreach (var entry in pool)
                totalWeight += entry.Weight;

            uint roll = (uint)(Random.Shared.NextInt64(totalWeight));
            uint cumulative = 0;
            int selectedIndex = pool.Count - 1;

            for (int i = 0; i < pool.Count; i++)
            {
                cumulative += pool[i].Weight;
                if (roll < cumulative)
                {
                    selectedIndex = i;
                    break;
                }
            }

            picked.Add(pool[selectedIndex].Config);
            pool.RemoveAt(selectedIndex);
        }

        return picked;
    }
}
