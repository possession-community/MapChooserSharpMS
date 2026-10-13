using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Cooldown;
using MapChooserSharpMS.Shared.MapCycle.Services;
using MapChooserSharpMS.Shared.Nomination;
using Xunit;

namespace MapChooserSharpMS.Tests.Nomination;

public class McsMapListSorterTests
{
    #region Fakes

    internal sealed class FakeMapConfig : IMapConfig
    {
        public required string MapName { get; init; }
        public string MapNameAlias => MapName;
        public string MapDescription => string.Empty;
        public long WorkshopId => 0;
        public IReadOnlyList<string> SearchTags { get; init; } = [];
        public List<IMapGroupConfig> GroupSettings { get; } = [];
        public bool IsDisabled => false;
        public int MaxExtends => 0;
        public int MaxExtCommandUses => 0;
        public int MapTime => 0;
        public int ExtendTimePerExtends => 0;
        public int MapRounds => 0;
        public int ExtendRoundsPerExtends => 0;
        public IRandomPickConfig RandomPickConfig => null!;
        public INominationConfig NominationConfig => null!;
        public IMcsCooldownSettings CooldownSettings => null!;
        public IExtraConfigAccessor ExtraConfiguration => null!;
    }

    private sealed class FakeCooldownResult : IDetailedCooldownResult
    {
        public required IMapConfig MapConfig { get; init; }
        public int HighestCooldownCount { get; init; }
        public DateTime LongestTimedCooldown { get; init; }
        public bool HasCooldown => HighestCooldownCount > 0 || LongestTimedCooldown > DateTime.MinValue;
        public int CooldownCount => HighestCooldownCount;
        public DateTime TimedCooldown => LongestTimedCooldown;
        public IReadOnlyDictionary<string, int> GroupCooldowns { get; } = new Dictionary<string, int>();
        public IReadOnlyDictionary<string, DateTime> GroupTimedCooldowns { get; } = new Dictionary<string, DateTime>();
    }

    internal sealed class FakeCooldownQueryService : IMapCooldownQueryService
    {
        public Dictionary<string, int> Cooldowns { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, DateTime> TimedCooldowns { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task<IDetailedCooldownResult?> QueryCurrentCooldowns(IMapConfig mapConfig)
            => Task.FromResult<IDetailedCooldownResult?>(GetCurrentCooldowns(mapConfig));

        public IDetailedCooldownResult GetCurrentCooldowns(IMapConfig mapConfig)
        {
            return new FakeCooldownResult
            {
                MapConfig = mapConfig,
                HighestCooldownCount = Cooldowns.GetValueOrDefault(mapConfig.MapName),
                LongestTimedCooldown = TimedCooldowns.GetValueOrDefault(mapConfig.MapName, DateTime.MinValue),
            };
        }
    }

    internal sealed class FakeCooldownState : IMcsCooldownState
    {
        public int CurrentCooldown => 0;
        public DateTime TimedCooldownEndUtc => DateTime.MinValue;
        public DateTime LastPlayedAt { get; init; } = DateTime.MinValue;
        public int UnplayedCount { get; init; }
        public int CurrentNominationCooldown => 0;
        public DateTime NominationTimedCooldownEndUtc => DateTime.MinValue;
        public bool IsCooldownActive => false;
        public bool IsNominationCooldownActive => false;
    }

    internal sealed class FakeCooldownStore : IMcsCooldownStore
    {
        private static readonly FakeCooldownState Empty = new();

        public Dictionary<string, FakeCooldownState> Maps { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Registers a map that has been played, with the given unplayed count.</summary>
        public void Played(string mapName, DateTime lastPlayedAt, int unplayedCount = 0)
            => Maps[mapName] = new FakeCooldownState { LastPlayedAt = lastPlayedAt, UnplayedCount = unplayedCount };

        public IMcsCooldownState GetEffectiveMapState(string mapName) => Maps.GetValueOrDefault(mapName, Empty);
        public IMcsCooldownState GetEffectiveGroupState(string groupName) => Empty;
        public IMcsCooldownState GetOwnMapState(string mapName) => Maps.GetValueOrDefault(mapName, Empty);
        public IMcsCooldownState GetOwnGroupState(string groupName) => Empty;
    }

    #endregion

    private static readonly DateTime Jan = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Jun = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static List<string> SortNames(IEnumerable<IMapConfig> maps, NominationSortOrder order, IMcsCooldownStore store)
    {
        return McsMapListSorter
            .Sort(maps, c => c, order, new FakeCooldownQueryService(), store)
            .Select(c => c.MapName)
            .ToList();
    }

    private static FakeMapConfig Map(string name, params string[] tags)
        => new() { MapName = name, SearchTags = tags };

    private static List<string> SortNames(
        IEnumerable<IMapConfig> maps,
        NominationSortOrder order,
        IMapCooldownQueryService? cooldownService = null)
    {
        return McsMapListSorter
            .Sort(maps, c => c, order, cooldownService ?? new FakeCooldownQueryService())
            .Select(c => c.MapName)
            .ToList();
    }

    [Fact]
    public void AlphabeticalAscending_SortsByMapName_CaseInsensitive()
    {
        var maps = new IMapConfig[] { Map("ze_C"), Map("ZE_a"), Map("ze_b") };

        var result = SortNames(maps, NominationSortOrder.AlphabeticalAscending);

        Assert.Equal(["ZE_a", "ze_b", "ze_C"], result);
    }

    [Fact]
    public void AlphabeticalDescending_SortsByMapNameReversed()
    {
        var maps = new IMapConfig[] { Map("ze_a"), Map("ze_c"), Map("ze_b") };

        var result = SortNames(maps, NominationSortOrder.AlphabeticalDescending);

        Assert.Equal(["ze_c", "ze_b", "ze_a"], result);
    }

    [Fact]
    public void CooldownAscending_SortsByCooldownCount_TieBrokenByName()
    {
        var maps = new IMapConfig[] { Map("ze_b"), Map("ze_a"), Map("ze_c") };
        var service = new FakeCooldownQueryService();
        service.Cooldowns["ze_a"] = 5;
        service.Cooldowns["ze_b"] = 0;
        service.Cooldowns["ze_c"] = 0;

        var result = SortNames(maps, NominationSortOrder.CooldownAscending, service);

        Assert.Equal(["ze_b", "ze_c", "ze_a"], result);
    }

    [Fact]
    public void CooldownDescending_SortsByCooldownCountReversed()
    {
        var maps = new IMapConfig[] { Map("ze_a"), Map("ze_b"), Map("ze_c") };
        var service = new FakeCooldownQueryService();
        service.Cooldowns["ze_a"] = 1;
        service.Cooldowns["ze_b"] = 3;
        service.Cooldowns["ze_c"] = 2;

        var result = SortNames(maps, NominationSortOrder.CooldownDescending, service);

        Assert.Equal(["ze_b", "ze_c", "ze_a"], result);
    }

    [Fact]
    public void TimedCooldownAscending_SortsByTimedCooldown_TieBrokenByName()
    {
        var maps = new IMapConfig[] { Map("ze_c"), Map("ze_b"), Map("ze_a") };
        var service = new FakeCooldownQueryService();
        service.TimedCooldowns["ze_b"] = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        service.TimedCooldowns["ze_c"] = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = SortNames(maps, NominationSortOrder.TimedCooldownAscending, service);

        Assert.Equal(["ze_a", "ze_b", "ze_c"], result);
    }

    [Fact]
    public void TimedCooldownDescending_SortsByTimedCooldownReversed()
    {
        var maps = new IMapConfig[] { Map("ze_a"), Map("ze_b") };
        var service = new FakeCooldownQueryService();
        service.TimedCooldowns["ze_a"] = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        service.TimedCooldowns["ze_b"] = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = SortNames(maps, NominationSortOrder.TimedCooldownDescending, service);

        Assert.Equal(["ze_b", "ze_a"], result);
    }

    [Fact]
    public void TagAscending_SortsBySmallestTag_UntaggedLast()
    {
        var maps = new IMapConfig[]
        {
            Map("ze_untagged"),
            Map("ze_hard", "hard", "long"),
            Map("ze_easy", "short", "easy"),
        };

        var result = SortNames(maps, NominationSortOrder.TagAscending);

        Assert.Equal(["ze_easy", "ze_hard", "ze_untagged"], result);
    }

    [Fact]
    public void TagDescending_UntaggedStillLast()
    {
        var maps = new IMapConfig[]
        {
            Map("ze_untagged"),
            Map("ze_easy", "easy"),
            Map("ze_hard", "hard"),
        };

        var result = SortNames(maps, NominationSortOrder.TagDescending);

        Assert.Equal(["ze_hard", "ze_easy", "ze_untagged"], result);
    }

    [Fact]
    public void TagAscending_SameTag_TieBrokenByName()
    {
        var maps = new IMapConfig[]
        {
            Map("ze_b", "easy"),
            Map("ze_a", "easy"),
        };

        var result = SortNames(maps, NominationSortOrder.TagAscending);

        Assert.Equal(["ze_a", "ze_b"], result);
    }

    [Fact]
    public void UnplayedCountAscending_SortsByCount_NeverPlayedLast()
    {
        var maps = new IMapConfig[] { Map("ze_never"), Map("ze_b"), Map("ze_a"), Map("ze_c") };
        var store = new FakeCooldownStore();
        store.Played("ze_a", Jan, unplayedCount: 5);
        store.Played("ze_b", Jan, unplayedCount: 0);
        store.Played("ze_c", Jan, unplayedCount: 5);

        var result = SortNames(maps, NominationSortOrder.UnplayedCountAscending, store);

        Assert.Equal(["ze_b", "ze_a", "ze_c", "ze_never"], result);
    }

    [Fact]
    public void UnplayedCountDescending_NeverPlayedFirst()
    {
        var maps = new IMapConfig[] { Map("ze_a"), Map("ze_never"), Map("ze_b") };
        var store = new FakeCooldownStore();
        store.Played("ze_a", Jan, unplayedCount: 2);
        store.Played("ze_b", Jan, unplayedCount: 9);

        var result = SortNames(maps, NominationSortOrder.UnplayedCountDescending, store);

        Assert.Equal(["ze_never", "ze_b", "ze_a"], result);
    }

    [Fact]
    public void LastPlayedAscending_OldestFirst_NeverPlayedBeforeEverything()
    {
        var maps = new IMapConfig[] { Map("ze_recent"), Map("ze_never"), Map("ze_old") };
        var store = new FakeCooldownStore();
        store.Played("ze_recent", Jun);
        store.Played("ze_old", Jan);

        var result = SortNames(maps, NominationSortOrder.LastPlayedAscending, store);

        Assert.Equal(["ze_never", "ze_old", "ze_recent"], result);
    }

    [Fact]
    public void LastPlayedDescending_NewestFirst_TieBrokenByName()
    {
        var maps = new IMapConfig[] { Map("ze_b"), Map("ze_old"), Map("ze_a") };
        var store = new FakeCooldownStore();
        store.Played("ze_a", Jun);
        store.Played("ze_b", Jun);
        store.Played("ze_old", Jan);

        var result = SortNames(maps, NominationSortOrder.LastPlayedDescending, store);

        Assert.Equal(["ze_a", "ze_b", "ze_old"], result);
    }

    [Theory]
    [InlineData(NominationSortOrder.UnplayedCountAscending)]
    [InlineData(NominationSortOrder.LastPlayedDescending)]
    public void StoreOrders_WithoutStoreOverload_Throw(NominationSortOrder order)
    {
        var maps = new IMapConfig[] { Map("ze_a") };

        Assert.Throws<ArgumentException>(() => SortNames(maps, order));
    }

    [Fact]
    public void Sort_WithSelector_SortsWrappedElements()
    {
        var items = new List<(string Label, IMapConfig Config)>
        {
            ("second", Map("ze_b")),
            ("first", Map("ze_a")),
        };

        var result = McsMapListSorter.Sort(
            items, i => i.Config, NominationSortOrder.AlphabeticalAscending, new FakeCooldownQueryService());

        Assert.Equal(["first", "second"], result.Select(i => i.Label).ToList());
    }

    [Fact]
    public void Sort_DoesNotMutateSource()
    {
        var maps = new List<IMapConfig> { Map("ze_b"), Map("ze_a") };

        McsMapListSorter.Sort(maps, c => c, NominationSortOrder.AlphabeticalAscending, new FakeCooldownQueryService());

        Assert.Equal("ze_b", maps[0].MapName);
    }
}
