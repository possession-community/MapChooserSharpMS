using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapConfig.Services;

namespace MapChooserSharpMS.Modules.MapConfig.Services;

internal sealed class McsMapSearchService(IMcsMapConfigProvider mapConfigProvider) : IMcsMapSearchService
{
    public McsMapSearchResult SearchMaps(string query, bool includeDisabledMaps = false, bool useSearchTagFallback = true)
    {
        var matched = new List<IMapConfig>();
        foreach (string mapName in mapConfigProvider.GetMapConfigs().Keys)
        {
            if (!mapName.Contains(query, StringComparison.OrdinalIgnoreCase))
                continue;

            if (TryResolve(mapName, includeDisabledMaps, out var config))
                matched.Add(config);
        }

        if (matched.Count > 1)
        {
            var exact = matched.FirstOrDefault(m =>
                string.Equals(m.MapName, query, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
                matched = [exact];
        }

        if (matched.Count == 0 && useSearchTagFallback)
        {
            matched = mapConfigProvider.ToolingService
                .FindMapsBySearchTag(query, AllResolvedConfigs(includeDisabledMaps));
        }

        return new McsMapSearchResult
        {
            Status = matched.Count switch
            {
                0 => McsMapSearchStatus.NotFound,
                1 => McsMapSearchStatus.Found,
                _ => McsMapSearchStatus.MultipleFound,
            },
            Maps = matched,
        };
    }

    public List<KeyValuePair<string, T>> SearchByName<T>(string query, IEnumerable<KeyValuePair<string, T>> source)
    {
        var matched = source
            .Where(kv => kv.Key.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matched.Count > 1)
        {
            int exactIndex = matched.FindIndex(kv =>
                string.Equals(kv.Key, query, StringComparison.OrdinalIgnoreCase));
            if (exactIndex >= 0)
                matched = [matched[exactIndex]];
        }

        return matched;
    }

    private IEnumerable<IMapConfig> AllResolvedConfigs(bool includeDisabledMaps)
    {
        foreach (string mapName in mapConfigProvider.GetMapConfigs().Keys)
        {
            if (TryResolve(mapName, includeDisabledMaps, out var config))
                yield return config;
        }
    }

    private bool TryResolve(string mapName, bool includeDisabledMaps, out IMapConfig config)
    {
        if (mapConfigProvider.TryGetMapConfig(mapName, out var resolved)
            && resolved is not null
            && (includeDisabledMaps || !resolved.IsDisabled))
        {
            config = resolved;
            return true;
        }

        config = null!;
        return false;
    }
}
