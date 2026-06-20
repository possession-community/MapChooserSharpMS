using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapConfig.Services;

namespace MapChooserSharpMS.Modules.MapConfig.Services;

internal sealed class MapConfigToolingService : IMapConfigToolingService
{
    private readonly Func<bool> _shouldUseAlias;

    public MapConfigToolingService(Func<bool> shouldUseAlias)
    {
        _shouldUseAlias = shouldUseAlias;
    }

    public string ResolveMapDisplayName(IMapConfig mapConfig)
    {
        string baseName = _shouldUseAlias() && !string.IsNullOrWhiteSpace(mapConfig.MapNameAlias)
            ? mapConfig.MapNameAlias
            : mapConfig.MapName;

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

    public List<IMapConfig> FindMapsBySearchTag(string tag, IEnumerable<IMapConfig> allMaps)
    {
        return allMaps
            .Where(m => m.SearchTags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }
}
