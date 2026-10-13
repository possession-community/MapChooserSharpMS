using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Cooldown;
using MapChooserSharpMS.Shared.MapCycle.Services;

namespace MapChooserSharpMS.Shared.Nomination;

/// <summary>
/// Sorts map lists for nomination menus. Shared between MCS core (default order)
/// and menu compat plugins (player-selected order).
/// </summary>
public static class McsMapListSorter
{
    /// <summary>
    /// Returns a new list sorted by <paramref name="order"/>.
    /// Non-alphabetical orders tie-break by map name ascending.
    /// Maps without search tags are always placed last for tag orders.
    /// </summary>
    /// <param name="source">Elements to sort.</param>
    /// <param name="configSelector">Extracts the <see cref="IMapConfig"/> from an element.</param>
    /// <param name="order">Sort order to apply.</param>
    /// <param name="cooldownQueryService">Used for cooldown-based orders; queries in-memory state only.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="order"/> is an unplayed-count or last-played order; those need the
    /// overload that takes an <see cref="IMcsCooldownStore"/>.
    /// </exception>
    public static List<T> Sort<T>(
        IEnumerable<T> source,
        Func<T, IMapConfig> configSelector,
        NominationSortOrder order,
        IMapCooldownQueryService cooldownQueryService)
    {
        return SortCore(source, configSelector, order, cooldownQueryService, null);
    }

    /// <summary>
    /// Returns a new list sorted by <paramref name="order"/>, including the
    /// unplayed-count and last-played orders.
    /// Unplayed-count orders treat never-played maps as the most unplayed;
    /// last-played orders treat them as the oldest.
    /// </summary>
    /// <param name="source">Elements to sort.</param>
    /// <param name="configSelector">Extracts the <see cref="IMapConfig"/> from an element.</param>
    /// <param name="order">Sort order to apply.</param>
    /// <param name="cooldownQueryService">Used for cooldown-based orders; queries in-memory state only.</param>
    /// <param name="cooldownStore">Used for unplayed-count and last-played orders (effective map state).</param>
    public static List<T> Sort<T>(
        IEnumerable<T> source,
        Func<T, IMapConfig> configSelector,
        NominationSortOrder order,
        IMapCooldownQueryService cooldownQueryService,
        IMcsCooldownStore cooldownStore)
    {
        return SortCore(source, configSelector, order, cooldownQueryService, cooldownStore);
    }

    /// <summary>
    /// Sort key for the unplayed-count orders: a never-played map has no meaningful
    /// counter, so it ranks above every map that has one.
    /// </summary>
    private static int UnplayedKeyOf(IMcsCooldownState state)
        => state.LastPlayedAt == DateTime.MinValue ? int.MaxValue : state.UnplayedCount;

    private static List<T> SortCore<T>(
        IEnumerable<T> source,
        Func<T, IMapConfig> configSelector,
        NominationSortOrder order,
        IMapCooldownQueryService cooldownQueryService,
        IMcsCooldownStore? cooldownStore)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        string NameOf(T element) => configSelector(element).MapName;

        switch (order)
        {
            case NominationSortOrder.AlphabeticalAscending:
                return source.OrderBy(NameOf, comparer).ToList();

            case NominationSortOrder.AlphabeticalDescending:
                return source.OrderByDescending(NameOf, comparer).ToList();

            case NominationSortOrder.CooldownAscending:
            case NominationSortOrder.CooldownDescending:
            {
                int CooldownOf(T element) =>
                    cooldownQueryService.GetCurrentCooldowns(configSelector(element)).HighestCooldownCount;

                var ordered = order == NominationSortOrder.CooldownAscending
                    ? source.OrderBy(CooldownOf)
                    : source.OrderByDescending(CooldownOf);
                return ordered.ThenBy(NameOf, comparer).ToList();
            }

            case NominationSortOrder.TimedCooldownAscending:
            case NominationSortOrder.TimedCooldownDescending:
            {
                DateTime TimedCooldownOf(T element) =>
                    cooldownQueryService.GetCurrentCooldowns(configSelector(element)).LongestTimedCooldown;

                var ordered = order == NominationSortOrder.TimedCooldownAscending
                    ? source.OrderBy(TimedCooldownOf)
                    : source.OrderByDescending(TimedCooldownOf);
                return ordered.ThenBy(NameOf, comparer).ToList();
            }

            case NominationSortOrder.TagAscending:
            case NominationSortOrder.TagDescending:
            {
                string? TagKeyOf(T element)
                {
                    var tags = configSelector(element).SearchTags;
                    return tags.Count > 0 ? tags.Min(comparer) : null;
                }

                var withTagFlag = source.OrderBy(e => TagKeyOf(e) is null);
                var ordered = order == NominationSortOrder.TagAscending
                    ? withTagFlag.ThenBy(TagKeyOf, comparer)
                    : withTagFlag.ThenByDescending(TagKeyOf, comparer);
                return ordered.ThenBy(NameOf, comparer).ToList();
            }

            case NominationSortOrder.UnplayedCountAscending:
            case NominationSortOrder.UnplayedCountDescending:
            {
                var store = RequireStore(cooldownStore, order);
                int UnplayedOf(T element) => UnplayedKeyOf(store.GetEffectiveMapState(NameOf(element)));

                var ordered = order == NominationSortOrder.UnplayedCountAscending
                    ? source.OrderBy(UnplayedOf)
                    : source.OrderByDescending(UnplayedOf);
                return ordered.ThenBy(NameOf, comparer).ToList();
            }

            case NominationSortOrder.LastPlayedAscending:
            case NominationSortOrder.LastPlayedDescending:
            {
                var store = RequireStore(cooldownStore, order);
                DateTime LastPlayedOf(T element) => store.GetEffectiveMapState(NameOf(element)).LastPlayedAt;

                var ordered = order == NominationSortOrder.LastPlayedAscending
                    ? source.OrderBy(LastPlayedOf)
                    : source.OrderByDescending(LastPlayedOf);
                return ordered.ThenBy(NameOf, comparer).ToList();
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(order), order, "Unknown sort order");
        }
    }

    private static IMcsCooldownStore RequireStore(IMcsCooldownStore? cooldownStore, NominationSortOrder order)
    {
        return cooldownStore
               ?? throw new ArgumentException(
                   $"Sort order {order} needs the overload that takes an IMcsCooldownStore", nameof(order));
    }
}
