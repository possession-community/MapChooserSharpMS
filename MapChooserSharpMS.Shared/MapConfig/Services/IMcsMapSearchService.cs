using System.Collections.Generic;

namespace MapChooserSharpMS.Shared.MapConfig.Services;

/// <summary>
/// Outcome of a map search.
/// </summary>
public enum McsMapSearchStatus
{
    /// <summary>No map matched the query.</summary>
    NotFound,

    /// <summary>Exactly one map matched; see <see cref="McsMapSearchResult.Maps"/>[0].</summary>
    Found,

    /// <summary>Several maps matched and the query was not an exact map name.</summary>
    MultipleFound,
}

/// <summary>
/// Result of <see cref="IMcsMapSearchService.SearchMaps"/>.
/// </summary>
public sealed class McsMapSearchResult
{
    public required McsMapSearchStatus Status { get; init; }

    /// <summary>Matched maps. Empty when <see cref="Status"/> is <see cref="McsMapSearchStatus.NotFound"/>.</summary>
    public required IReadOnlyList<IMapConfig> Maps { get; init; }
}

/// <summary>
/// Shared map-name search used by every command that takes a map name
/// (!nominate, !setnextmap, !map, !mapinfo, ...).
///
/// Matching flow:
/// 1. Case-insensitive substring match on the map name.
/// 2. When several maps match, an exact (case-insensitive) map-name match
///    collapses the result to that single map.
/// 3. When nothing matched, an optional SearchTag fallback
///    (case-insensitive exact tag equality) is tried.
/// </summary>
public interface IMcsMapSearchService
{
    /// <summary>
    /// Searches all configured maps. Configs are resolved through the provider's
    /// day/time override resolution, so the returned configs reflect the
    /// currently active DaySettings override.
    /// </summary>
    /// <param name="query">Map name (or fragment), or a search tag.</param>
    /// <param name="includeDisabledMaps">Include maps whose resolved config is disabled (admin flows).</param>
    /// <param name="useSearchTagFallback">Try SearchTag lookup when no name matched.</param>
    McsMapSearchResult SearchMaps(string query, bool includeDisabledMaps = false, bool useSearchTagFallback = true);

    /// <summary>
    /// Applies the same substring + exact-collapse matching to an arbitrary
    /// name-keyed collection (e.g. the currently nominated maps).
    /// </summary>
    List<KeyValuePair<string, T>> SearchByName<T>(string query, IEnumerable<KeyValuePair<string, T>> source);
}
