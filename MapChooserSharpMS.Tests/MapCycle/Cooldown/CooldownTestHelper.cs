using System;
using System.Collections.Generic;
using MapChooserSharpMS.Modules.MapConfig.Extra;
using MapChooserSharpMS.Modules.MapConfig.Models;
using MapConfigModel = MapChooserSharpMS.Modules.MapConfig.Models.MapConfig;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Tests.MapCycle.Cooldown;

internal static class CooldownTestHelper
{
    internal static MapConfigModel CreateMapConfig(
        string mapName,
        long workshopId = 0,
        int configCooldown = 3,
        TimeSpan? timedCooldown = null,
        int configNomCooldown = 0,
        TimeSpan? nomTimedCooldown = null,
        List<IMapGroupConfig>? groups = null)
    {
        var cc = new CooldownConfig(
            configCooldown,
            timedCooldown ?? TimeSpan.Zero,
            configNomCooldown,
            nomTimedCooldown ?? TimeSpan.Zero);

        return new MapConfigModel(
            MapName: mapName,
            MapNameAlias: mapName,
            MapDescription: "",
            WorkshopId: workshopId,
            GroupSettings: groups ?? new List<IMapGroupConfig>(),
            IsDisabled: false,
            MaxExtends: 3,
            MaxExtCommandUses: 1,
            MapTime: 0,
            ExtendTimePerExtends: 15,
            MapRounds: 0,
            ExtendRoundsPerExtends: 5,
            RandomPickConfig: new RandomPickConfig(1, true, false),
            NominationConfig: new NominationConfig(0, 0, false, Array.Empty<DayOfWeek>(), Array.Empty<ITimeRange>()),
            CooldownConfig: cc,
            ExtraConfiguration: ExtraConfigAccessor.Empty);
    }

    internal static CooldownConfig GetCooldownConfig(IMapConfig map)
        => (CooldownConfig)map.CooldownConfig;

    internal static MapConfigModel CreateProvisionalWorkshopMap(string mapName, long workshopId)
        => CreateMapConfig(mapName, workshopId: workshopId, groups: new List<IMapGroupConfig>());
}
