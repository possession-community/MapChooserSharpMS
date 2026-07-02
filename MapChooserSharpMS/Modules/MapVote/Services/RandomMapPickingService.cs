using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.Nomination.Services;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapVote.Services;

/// <summary>
/// Random map pick, split into the phases the deferred vote-candidate pipeline
/// needs: <see cref="PreparePick"/> and <see cref="FilterPickable"/> (pure) run on
/// whichever thread calls them, while <see cref="FinishPick"/> fires events and
/// must be called on the game thread.
/// </summary>
internal sealed class RandomMapPickingService(
    NominationValidateService nominationValidateService,
    IMcsPluginConfigProvider pluginConfigProvider,
    IMcsMapConfigProvider mapConfigProvider)
{
    /// <summary>
    /// Game-thread step: resolves the candidate pool and takes a snapshot of the
    /// live state the pure filter needs. No blocking, no thread hop.
    /// </summary>
    public RandomPickPlan PreparePick(int amount = -1, ISet<string>? excludeMapNames = null)
    {
        if (amount == -1)
            amount = pluginConfigProvider.PluginConfig.VoteConfig.MaxMenuElements;

        var allMaps = mapConfigProvider.GetMapConfigs()
            .Where(kv => excludeMapNames is null || !excludeMapNames.Contains(kv.Key))
            .Select(kv => kv.Value.First().MapConfig)
            .ToList();

        var snapshot = nominationValidateService.CreatePickupSnapshot();

        return new RandomPickPlan(allMaps, snapshot, amount);
    }

    /// <summary>
    /// Pure filter, safe to run on the thread pool — no event firing, no game API access.
    /// </summary>
    public List<IMapConfig> FilterPickable(RandomPickPlan plan)
        => nominationValidateService.FilterPickableMapsPure(plan.AllMaps, plan.Snapshot);

    /// <summary>
    /// Game-thread step: fires the nomination-check-passed event per surviving
    /// map, then shuffles. Kept together so the shuffle runs after the event filter.
    /// </summary>
    public List<IMapConfig> FinishPick(List<IMapConfig> filteredMaps, int amount)
    {
        var candidates = nominationValidateService.FilterByNominationCheckEvent(filteredMaps);
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

/// <summary>
/// Inputs for a deferred random pick: the already-filtered-by-name candidate
/// pool and the game-thread snapshot the pure filter consumes.
/// </summary>
internal readonly record struct RandomPickPlan(
    List<IMapConfig> AllMaps,
    NominationValidateService.PickupSnapshot Snapshot,
    int Amount);
