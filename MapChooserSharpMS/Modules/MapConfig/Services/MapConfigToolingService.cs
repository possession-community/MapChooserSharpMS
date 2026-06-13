using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapConfig.Services;

namespace MapChooserSharpMS.Modules.MapConfig.Services;

internal sealed class MapConfigToolingService : IMapConfigToolingService
{
    public string ResolveMapDisplayName(IMapConfig mapConfig)
    {
        string baseName = string.IsNullOrWhiteSpace(mapConfig.MapNameAlias)
            ? mapConfig.MapName
            : mapConfig.MapNameAlias;

        string tag = ResolveTag(mapConfig);
        return tag.Length > 0 ? $"[{tag}] {baseName}" : baseName;
    }

    private static string ResolveTag(IMapConfig mapConfig)
    {
        foreach (var group in mapConfig.GroupSettings)
        {
            if (!string.IsNullOrWhiteSpace(group.ShortGroupName))
                return group.ShortGroupName;
        }
        return "";
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
