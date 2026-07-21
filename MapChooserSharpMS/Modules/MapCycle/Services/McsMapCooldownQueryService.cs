using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.MapCycle.Cooldown;
using MapChooserSharpMS.Modules.Nomination.Models;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Services;
using MapChooserSharpMS.Shared.Nomination;

namespace MapChooserSharpMS.Modules.MapCycle.Services;

internal sealed class McsMapCooldownQueryService : IMapCooldownQueryService
{
    private readonly IMcsInternalCooldownStore _store;

    internal McsMapCooldownQueryService(IMcsInternalCooldownStore store)
    {
        _store = store;
    }

    public IDetailedCooldownResult GetCurrentCooldowns(IMapConfig mapConfig)
    {
        var mapState = _store.GetEffectiveMapState(mapConfig.MapName);

        var groupCooldowns = new Dictionary<string, int>();
        var groupTimedCooldowns = new Dictionary<string, DateTime>();

        foreach (var group in mapConfig.GroupSettings)
        {
            var groupState = _store.GetEffectiveGroupState(group.GroupName);
            groupCooldowns[group.GroupName] = groupState.CurrentCooldown;
            groupTimedCooldowns[group.GroupName] = groupState.TimedCooldownEndUtc;
        }

        return new DetailedCooldownResult(
            mapConfig, mapState.CurrentCooldown, groupCooldowns,
            mapState.TimedCooldownEndUtc, groupTimedCooldowns);
    }

    public Task<IDetailedCooldownResult?> QueryCurrentCooldowns(IMapConfig mapConfig)
    {
        // TODO: scoped DB query — the in-memory effective layer already reflects
        // the last map-start load, so this returns it directly for now.
        return Task.FromResult<IDetailedCooldownResult?>(GetCurrentCooldowns(mapConfig));
    }
}
