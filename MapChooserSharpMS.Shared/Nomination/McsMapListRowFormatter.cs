using System;
using System.Linq;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Cooldown;
using MapChooserSharpMS.Shared.MapCycle.Services;

namespace MapChooserSharpMS.Shared.Nomination;

/// <summary>
/// Builds map list rows that show the value the list is sorted by. The value goes
/// in front of the map name so long names cannot push it out of view.
/// </summary>
public static class McsMapListRowFormatter
{
    /// <summary>
    /// Returns the sort-key prefix for a row, e.g. <c>[Unplayed: 10]</c>, <c>[CD: 3]</c>,
    /// <c>[Hard] [Insane]</c> or <c>[Last: 2026-01-31 18:05:00]</c> (local time).
    /// Empty for the alphabetical orders and for untagged maps under the tag orders.
    /// </summary>
    public static string GetSortPrefix(
        IMapConfig mapConfig,
        NominationSortOrder order,
        IMapCooldownQueryService cooldownQueryService,
        IMcsCooldownStore cooldownStore)
    {
        switch (order)
        {
            case NominationSortOrder.CooldownAscending:
            case NominationSortOrder.CooldownDescending:
            {
                int cooldown = cooldownQueryService.GetCurrentCooldowns(mapConfig).HighestCooldownCount;
                // int.MaxValue marks a map an admin excluded until further notice.
                return cooldown == int.MaxValue ? "[CD: Inf]" : $"[CD: {cooldown}]";
            }

            case NominationSortOrder.TimedCooldownAscending:
            case NominationSortOrder.TimedCooldownDescending:
            {
                var until = cooldownQueryService.GetCurrentCooldowns(mapConfig).LongestTimedCooldown;
                return until > DateTime.UtcNow ? $"[Until: {until.ToLocalTime():yyyy-MM-dd HH:mm:ss}]" : string.Empty;
            }

            case NominationSortOrder.TagAscending:
            case NominationSortOrder.TagDescending:
                return string.Join(" ", mapConfig.SearchTags.Select(tag => $"[{tag}]"));

            case NominationSortOrder.UnplayedCountAscending:
            case NominationSortOrder.UnplayedCountDescending:
            {
                var state = cooldownStore.GetEffectiveMapState(mapConfig.MapName);
                return state.LastPlayedAt == DateTime.MinValue
                    ? "[Unplayed: -]"
                    : $"[Unplayed: {state.UnplayedCount}]";
            }

            case NominationSortOrder.LastPlayedAscending:
            case NominationSortOrder.LastPlayedDescending:
            {
                var lastPlayed = cooldownStore.GetEffectiveMapState(mapConfig.MapName).LastPlayedAt;
                return lastPlayed == DateTime.MinValue
                    ? "[Last: Never]"
                    : $"[Last: {lastPlayed.ToLocalTime():yyyy-MM-dd HH:mm:ss}]";
            }

            default:
                return string.Empty;
        }
    }

    /// <summary>
    /// Returns <paramref name="displayText"/> with the sort-key prefix for
    /// <paramref name="order"/> in front, or unchanged when there is no prefix.
    /// </summary>
    public static string FormatRow(
        string displayText,
        IMapConfig mapConfig,
        NominationSortOrder order,
        IMapCooldownQueryService cooldownQueryService,
        IMcsCooldownStore cooldownStore)
    {
        string prefix = GetSortPrefix(mapConfig, order, cooldownQueryService, cooldownStore);
        return prefix.Length == 0 ? displayText : $"{prefix} {displayText}";
    }
}
