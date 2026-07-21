using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapConfig.Services;
using MapChooserSharpMS.Shared.MapCycle.Cooldown;

namespace MapChooserSharpMS.Modules.MapConfig.Services;

internal sealed class MapConfigToolingService : IMapConfigToolingService
{
    private readonly Func<bool> _shouldUseAlias;
    private readonly Func<IMcsCooldownStore?> _cooldownStore;

    public MapConfigToolingService(Func<bool> shouldUseAlias, Func<IMcsCooldownStore?> cooldownStore)
    {
        _shouldUseAlias = shouldUseAlias;
        _cooldownStore = cooldownStore;
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
        var store = _cooldownStore();
        if (store is null)
            return 0;

        int highest = store.GetEffectiveMapState(mapConfig.MapName).CurrentCooldown;

        foreach (IMapGroupConfig group in mapConfig.GroupSettings)
        {
            int groupCd = store.GetEffectiveGroupState(group.GroupName).CurrentCooldown;
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
