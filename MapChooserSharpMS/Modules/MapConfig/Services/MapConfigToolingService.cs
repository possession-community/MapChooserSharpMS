using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapConfig.Services;

namespace MapChooserSharpMS.Modules.MapConfig.Services;

internal sealed class MapConfigToolingService : IMapConfigToolingService
{
    public string ResolveMapDisplayName(IMapConfig mapConfig)
    {
        return string.IsNullOrWhiteSpace(mapConfig.MapNameAlias)
            ? mapConfig.MapName
            : mapConfig.MapNameAlias;
    }

    public int GetHighestCooldown(IMapConfig mapConfig)
    {
        int highest = mapConfig.CooldownConfig.CurrentCooldown;

        foreach (IMapGroupConfig group in mapConfig.GroupSettings)
        {
            int groupCd = group.CooldownConfig.CurrentCooldown;
            if (groupCd > highest)
                highest = groupCd;
        }

        return highest;
    }
}
