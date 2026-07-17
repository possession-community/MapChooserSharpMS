using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.MapConfig.Extra;
using MapChooserSharpMS.Modules.MapConfig.Models;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;
using MapChooserSharpMS.Shared.MapConfig;
using Xunit;

namespace MapChooserSharpMS.Tests.MapCycle.Cooldown;

public class CooldownPersistenceTests
{
    // ========================================================================
    // IsProvisionalMap — workshop maps without groups are excluded from DB
    // ========================================================================

    [Fact]
    public void ProvisionalWorkshopMap_HasNoGroups_IsProvisional()
    {
        var map = CooldownTestHelper.CreateProvisionalWorkshopMap("ws_test", 12345);

        Assert.True(map.WorkshopId > 0);
        Assert.Empty(map.GroupSettings);
    }

    [Fact]
    public void ConfigMap_WithWorkshopId_IsNotProvisional()
    {
        var group = CreateFakeGroup("competitive");
        var map = CooldownTestHelper.CreateMapConfig("de_test", workshopId: 99999,
            groups: new List<IMapGroupConfig> { group });

        Assert.True(map.WorkshopId > 0);
        Assert.NotEmpty(map.GroupSettings);
    }

    [Fact]
    public void ConfigMap_NoWorkshopId_IsNotProvisional()
    {
        var map = CooldownTestHelper.CreateMapConfig("de_test", workshopId: 0);
        Assert.Equal(0, map.WorkshopId);
    }

    // ========================================================================
    // CooldownRecord — BuildMapRecord / BuildGroupRecord produce correct values
    // ========================================================================

    [Fact]
    public void CooldownRecord_FromCooldownConfig_HasCorrectValues()
    {
        var map = CooldownTestHelper.CreateMapConfig("de_dust2", configCooldown: 5);
        var cc = CooldownTestHelper.GetCooldownConfig(map);

        cc.CurrentCooldown = 3;
        cc.LastPlayedAt = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        cc.TimedCooldownEndUtc = new DateTime(2026, 6, 16, 12, 0, 0, DateTimeKind.Utc);
        cc.UnplayedCount = 7;
        cc.CurrentNominationCooldown = 2;
        cc.NominationTimedCooldownEndUtc = new DateTime(2026, 6, 15, 14, 0, 0, DateTimeKind.Utc);

        var record = new CooldownRecord(
            Cooldown: cc.CurrentCooldown,
            TimedCooldownEnd: cc.TimedCooldownEndUtc,
            LastPlayedAt: cc.LastPlayedAt,
            UnplayedCount: cc.UnplayedCount,
            NomCooldown: cc.CurrentNominationCooldown,
            NomTimedCooldownEnd: cc.NominationTimedCooldownEndUtc,
            LastNominatedAt: DateTime.MinValue);

        Assert.Equal(3, record.Cooldown);
        Assert.Equal(new DateTime(2026, 6, 16, 12, 0, 0, DateTimeKind.Utc), record.TimedCooldownEnd);
        Assert.Equal(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc), record.LastPlayedAt);
        Assert.Equal(7, record.UnplayedCount);
        Assert.Equal(2, record.NomCooldown);
        Assert.Equal(new DateTime(2026, 6, 15, 14, 0, 0, DateTimeKind.Utc), record.NomTimedCooldownEnd);
    }

    // ========================================================================
    // LoadFromDatabaseAsync — records are applied to in-memory CooldownConfig
    // ========================================================================

    [Fact]
    public async Task LoadFromDatabase_AppliesMapCooldowns()
    {
        var fake = new FakeCooldownPersistence();
        var map = CooldownTestHelper.CreateMapConfig("de_dust2", configCooldown: 5);

        var loadedRecord = new CooldownRecord(
            Cooldown: 2,
            TimedCooldownEnd: new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            LastPlayedAt: new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc),
            UnplayedCount: 4,
            NomCooldown: 1,
            NomTimedCooldownEnd: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            LastNominatedAt: DateTime.MinValue);

        fake.MapCooldownsToLoad = new[]
        {
            new NamedCooldownRecord("de_dust2", loadedRecord),
        };

        var cc = CooldownTestHelper.GetCooldownConfig(map);

        Assert.Equal(0, cc.CurrentCooldown);
        Assert.Equal(0, cc.UnplayedCount);

        ApplyLoadedMapCooldown(cc, loadedRecord);

        Assert.Equal(2, cc.CurrentCooldown);
        Assert.Equal(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), cc.TimedCooldownEndUtc);
        Assert.Equal(new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc), cc.LastPlayedAt);
        Assert.Equal(4, cc.UnplayedCount);
        Assert.Equal(1, cc.CurrentNominationCooldown);
        Assert.Equal(new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc), cc.NominationTimedCooldownEndUtc);
    }

    [Fact]
    public async Task LoadFromDatabase_UnknownMap_IsIgnored()
    {
        var fake = new FakeCooldownPersistence();

        var loadedRecord = new CooldownRecord(
            Cooldown: 5,
            TimedCooldownEnd: DateTime.MinValue,
            LastPlayedAt: DateTime.MinValue,
            UnplayedCount: 0,
            NomCooldown: 0,
            NomTimedCooldownEnd: DateTime.MinValue,
            LastNominatedAt: DateTime.MinValue);

        fake.MapCooldownsToLoad = new[]
        {
            new NamedCooldownRecord("de_nonexistent", loadedRecord),
        };

        // Should not throw — unknown maps are simply skipped
        var records = await fake.LoadAllMapCooldownsAsync();
        Assert.Single(records);
        Assert.Equal("de_nonexistent", records[0].Name);
    }

    // ========================================================================
    // NullCooldownPersistence — all operations are no-ops
    // ========================================================================

    [Fact]
    public async Task NullPersistence_ReturnsEmpty()
    {
        var persistence = MapChooserSharpMS.Modules.MapCycle.Services.NullCooldownPersistence.Instance;

        var maps = await persistence.LoadAllMapCooldownsAsync();
        var groups = await persistence.LoadAllGroupCooldownsAsync();

        Assert.Empty(maps);
        Assert.Empty(groups);
    }

    [Fact]
    public async Task NullPersistence_SaveDoesNotThrow()
    {
        var persistence = MapChooserSharpMS.Modules.MapCycle.Services.NullCooldownPersistence.Instance;
        var record = new CooldownRecord(0, DateTime.MinValue, DateTime.MinValue, 0, 0, DateTime.MinValue, DateTime.MinValue);

        await persistence.SaveMapCooldownAsync("test", record);
        await persistence.SaveGroupCooldownAsync("test", record);
        persistence.SaveMapCooldownFireAndForget("test", record);
        persistence.SaveGroupCooldownFireAndForget("test", record);
        persistence.SaveAllCooldownsFireAndForget(
            Array.Empty<(string, CooldownRecord)>(),
            Array.Empty<(string, CooldownRecord)>());
    }

    // ========================================================================
    // FakePersistence — write-through / write-behind tracking
    // ========================================================================

    [Fact]
    public async Task FakePersistence_SaveMapAsync_TracksWriteThrough()
    {
        var fake = new FakeCooldownPersistence();
        var record = new CooldownRecord(3, DateTime.UtcNow, DateTime.UtcNow, 6, 0, DateTime.MinValue, DateTime.MinValue);

        await fake.SaveMapCooldownAsync("de_test", record);

        Assert.Single(fake.SavedMaps);
        Assert.Equal("de_test", fake.SavedMaps[0].Name);
        Assert.Equal(3, fake.SavedMaps[0].Record.Cooldown);
        Assert.Equal(6, fake.SavedMaps[0].Record.UnplayedCount);
    }

    [Fact]
    public void FakePersistence_FireAndForget_TracksWriteBehind()
    {
        var fake = new FakeCooldownPersistence();
        var record = new CooldownRecord(2, DateTime.UtcNow, DateTime.UtcNow, 0, 0, DateTime.MinValue, DateTime.MinValue);

        fake.SaveMapCooldownFireAndForget("de_test", record);
        fake.SaveGroupCooldownFireAndForget("group_a", record);

        Assert.Single(fake.FireAndForgetMaps);
        Assert.Single(fake.FireAndForgetGroups);
        Assert.Equal("de_test", fake.FireAndForgetMaps[0].Name);
        Assert.Equal("group_a", fake.FireAndForgetGroups[0].Name);
    }

    [Fact]
    public void FakePersistence_BulkSave_TracksBulkWriteBehind()
    {
        var fake = new FakeCooldownPersistence();
        var record = new CooldownRecord(1, DateTime.UtcNow, DateTime.UtcNow, 0, 0, DateTime.MinValue, DateTime.MinValue);

        var maps = new List<(string, CooldownRecord)> { ("de_a", record), ("de_b", record) };
        var groups = new List<(string, CooldownRecord)> { ("grp_x", record) };

        fake.SaveAllCooldownsFireAndForget(maps, groups);

        Assert.Single(fake.BulkSaves);
        Assert.Equal(2, fake.BulkSaves[0].Maps.Count);
        Assert.Single(fake.BulkSaves[0].Groups);
    }

    // ========================================================================
    // NamedCooldownRecord — equality and data access
    // ========================================================================

    [Fact]
    public void NamedCooldownRecord_StoresNameAndRecord()
    {
        var record = new CooldownRecord(5, DateTime.MinValue, DateTime.MinValue, 0, 0, DateTime.MinValue, DateTime.MinValue);
        var named = new NamedCooldownRecord("de_dust2", record);

        Assert.Equal("de_dust2", named.Name);
        Assert.Equal(5, named.Record.Cooldown);
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private static void ApplyLoadedMapCooldown(CooldownConfig cc, CooldownRecord record)
    {
        cc.CurrentCooldown = record.Cooldown;
        cc.TimedCooldownEndUtc = record.TimedCooldownEnd;
        cc.LastPlayedAt = record.LastPlayedAt;
        cc.UnplayedCount = record.UnplayedCount;
        cc.CurrentNominationCooldown = record.NomCooldown;
        cc.NominationTimedCooldownEndUtc = record.NomTimedCooldownEnd;
    }

    private static MapGroupConfig CreateFakeGroup(string groupName, int configCooldown = 2)
    {
        var cc = new CooldownConfig(configCooldown, TimeSpan.Zero);
        return new MapGroupConfig(
            GroupName: groupName,
            ShortGroupName: "",
            MapCooldownOverride: 0,
            NominationLimit: 0,
            IsDisabled: false,
            MaxExtends: 3,
            MaxExtCommandUses: 1,
            MapTime: 0,
            ExtendTimePerExtends: 15,
            MapRounds: 0,
            ExtendRoundsPerExtends: 5,
            RandomPickConfig: new RandomPickConfig(1, true, false),
            NominationConfig: new NominationConfig(0, 0, false, Array.Empty<DayOfWeek>(), Array.Empty<ITimeRange>()),
            CooldownConfig: cc,
            ExtraConfiguration: ExtraConfigAccessor.Empty,
            SearchTags: []);
    }
}
