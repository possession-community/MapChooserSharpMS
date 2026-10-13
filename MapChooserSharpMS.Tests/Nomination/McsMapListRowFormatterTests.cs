using System;
using MapChooserSharpMS.Shared.Nomination;
using Xunit;
using FakeCooldownQueryService = MapChooserSharpMS.Tests.Nomination.McsMapListSorterTests.FakeCooldownQueryService;
using FakeCooldownStore = MapChooserSharpMS.Tests.Nomination.McsMapListSorterTests.FakeCooldownStore;
using FakeMapConfig = MapChooserSharpMS.Tests.Nomination.McsMapListSorterTests.FakeMapConfig;

namespace MapChooserSharpMS.Tests.Nomination;

public class McsMapListRowFormatterTests
{
    private readonly FakeCooldownQueryService _cooldowns = new();
    private readonly FakeCooldownStore _store = new();

    private static FakeMapConfig Map(string name, params string[] tags)
        => new() { MapName = name, SearchTags = tags };

    private string Prefix(FakeMapConfig map, NominationSortOrder order)
        => McsMapListRowFormatter.GetSortPrefix(map, order, _cooldowns, _store);

    [Theory]
    [InlineData(NominationSortOrder.AlphabeticalAscending)]
    [InlineData(NominationSortOrder.AlphabeticalDescending)]
    public void Alphabetical_HasNoPrefix(NominationSortOrder order)
    {
        Assert.Equal(string.Empty, Prefix(Map("ze_a", "hard"), order));
    }

    [Fact]
    public void Cooldown_ShowsCount_IncludingZero()
    {
        _cooldowns.Cooldowns["ze_a"] = 100;

        Assert.Equal("[CD: 100]", Prefix(Map("ze_a"), NominationSortOrder.CooldownAscending));
        Assert.Equal("[CD: 0]", Prefix(Map("ze_b"), NominationSortOrder.CooldownDescending));
    }

    [Fact]
    public void Cooldown_AdminExcludedMap_IsNotShownAsANumber()
    {
        _cooldowns.Cooldowns["ze_a"] = int.MaxValue;

        Assert.Equal("[CD: Inf]", Prefix(Map("ze_a"), NominationSortOrder.CooldownAscending));
    }

    [Fact]
    public void Tag_BracketsEveryTag_InConfigOrder()
    {
        Assert.Equal("[Hard] [Insane]", Prefix(Map("ze_a", "Hard", "Insane"), NominationSortOrder.TagAscending));
    }

    [Fact]
    public void Tag_UntaggedMap_HasNoPrefix()
    {
        Assert.Equal(string.Empty, Prefix(Map("ze_a"), NominationSortOrder.TagDescending));
    }

    [Fact]
    public void Unplayed_ShowsCount()
    {
        _store.Played("ze_a", DateTime.UtcNow, unplayedCount: 10);

        Assert.Equal("[Unplayed: 10]", Prefix(Map("ze_a"), NominationSortOrder.UnplayedCountDescending));
    }

    [Fact]
    public void Unplayed_NeverPlayedMap_ShowsDash()
    {
        Assert.Equal("[Unplayed: -]", Prefix(Map("ze_a"), NominationSortOrder.UnplayedCountAscending));
    }

    [Fact]
    public void LastPlayed_ShowsLocalTime_24Hour()
    {
        var local = new DateTime(2026, 3, 5, 18, 4, 9, DateTimeKind.Local);
        _store.Played("ze_a", local.ToUniversalTime());

        Assert.Equal("[Last: 2026-03-05 18:04:09]", Prefix(Map("ze_a"), NominationSortOrder.LastPlayedDescending));
    }

    [Fact]
    public void LastPlayed_NeverPlayedMap_ShowsNever()
    {
        Assert.Equal("[Last: Never]", Prefix(Map("ze_a"), NominationSortOrder.LastPlayedAscending));
    }

    [Fact]
    public void FormatRow_PutsPrefixBeforeTheName()
    {
        _store.Played("ze_a", DateTime.UtcNow, unplayedCount: 3);
        var map = Map("ze_a");

        string row = McsMapListRowFormatter.FormatRow(
            "ze_a (Alias)", map, NominationSortOrder.UnplayedCountAscending, _cooldowns, _store);

        Assert.Equal("[Unplayed: 3] ze_a (Alias)", row);
    }

    [Fact]
    public void FormatRow_WithoutPrefix_ReturnsTextUnchanged()
    {
        string row = McsMapListRowFormatter.FormatRow(
            "ze_a", Map("ze_a"), NominationSortOrder.AlphabeticalAscending, _cooldowns, _store);

        Assert.Equal("ze_a", row);
    }
}
