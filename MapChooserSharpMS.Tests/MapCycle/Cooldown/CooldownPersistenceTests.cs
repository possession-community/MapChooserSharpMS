using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.MapCycle.Services;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;
using MapChooserSharpMS.Modules.PluginConfig.Enums;
using Xunit;

namespace MapChooserSharpMS.Tests.MapCycle.Cooldown;

public class CooldownPersistenceTests
{
    private static CooldownRecord Record(int cooldown = 0, int unplayed = 0)
        => new(
            Cooldown: cooldown,
            TimedCooldownEnd: DateTime.MinValue,
            LastPlayedAt: DateTime.MinValue,
            UnplayedCount: unplayed,
            NomCooldown: 0,
            NomTimedCooldownEnd: DateTime.MinValue);

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
    public void ConfigMap_NoWorkshopId_IsNotProvisional()
    {
        var map = CooldownTestHelper.CreateMapConfig("de_test", workshopId: 0);
        Assert.Equal(0, map.WorkshopId);
    }

    // ========================================================================
    // Scoped load query generation
    // ========================================================================

    [Fact]
    public void BuildLoadSurql_Exact_MatchesServerKeyEquality()
    {
        var surql = SurrealCooldownRepository.BuildLoadSurql("mcs_map_cooldown", McsCooldownScopeMatchMode.Exact);

        Assert.Equal("SELECT * FROM mcs_map_cooldown WHERE server_key = $pattern;", surql);
    }

    [Fact]
    public void BuildLoadSurql_StartsWith_UsesStringStartsWith()
    {
        var surql = SurrealCooldownRepository.BuildLoadSurql("mcs_group_cooldown", McsCooldownScopeMatchMode.StartsWith);

        Assert.Equal("SELECT * FROM mcs_group_cooldown WHERE string::starts_with(server_key, $pattern);", surql);
    }

    // ========================================================================
    // NullCooldownPersistence — all operations are no-ops
    // ========================================================================

    [Fact]
    public async Task NullPersistence_ReturnsEmpty()
    {
        var persistence = NullCooldownPersistence.Instance;
        var scope = new CooldownScopeQuery(McsCooldownScopeMatchMode.Exact, "server-1");

        var maps = await persistence.LoadMapCooldownsAsync(scope);
        var groups = await persistence.LoadGroupCooldownsAsync(scope);

        Assert.Empty(maps);
        Assert.Empty(groups);
    }

    [Fact]
    public async Task NullPersistence_SaveDoesNotThrow()
    {
        var persistence = NullCooldownPersistence.Instance;
        var record = Record();

        await persistence.SaveMapCooldownAsync("test", record);
        await persistence.SaveGroupCooldownAsync("test", record);
        persistence.SaveMapCooldownFireAndForget("test", record);
        persistence.SaveGroupCooldownFireAndForget("test", record);
        persistence.SaveAllCooldownsFireAndForget(
            Array.Empty<(string, CooldownRecord)>(),
            Array.Empty<(string, CooldownRecord)>());
    }

    // ========================================================================
    // FakePersistence — write-through / write-behind / scope tracking
    // ========================================================================

    [Fact]
    public async Task FakePersistence_SaveMapAsync_TracksWriteThrough()
    {
        var fake = new FakeCooldownPersistence();

        await fake.SaveMapCooldownAsync("de_test", Record(cooldown: 3, unplayed: 6));

        Assert.Single(fake.SavedMaps);
        Assert.Equal("de_test", fake.SavedMaps[0].Name);
        Assert.Equal(3, fake.SavedMaps[0].Record.Cooldown);
        Assert.Equal(6, fake.SavedMaps[0].Record.UnplayedCount);
    }

    [Fact]
    public void FakePersistence_FireAndForget_TracksWriteBehind()
    {
        var fake = new FakeCooldownPersistence();

        fake.SaveMapCooldownFireAndForget("de_test", Record(cooldown: 2));
        fake.SaveGroupCooldownFireAndForget("group_a", Record(cooldown: 2));

        Assert.Single(fake.FireAndForgetMaps);
        Assert.Single(fake.FireAndForgetGroups);
        Assert.Equal("de_test", fake.FireAndForgetMaps[0].Name);
        Assert.Equal("group_a", fake.FireAndForgetGroups[0].Name);
    }

    [Fact]
    public void FakePersistence_BulkSave_TracksBulkWriteBehind()
    {
        var fake = new FakeCooldownPersistence();
        var record = Record(cooldown: 1);

        var maps = new List<(string, CooldownRecord)> { ("de_a", record), ("de_b", record) };
        var groups = new List<(string, CooldownRecord)> { ("grp_x", record) };

        fake.SaveAllCooldownsFireAndForget(maps, groups);

        Assert.Single(fake.BulkSaves);
        Assert.Equal(2, fake.BulkSaves[0].Maps.Count);
        Assert.Single(fake.BulkSaves[0].Groups);
    }

    [Fact]
    public async Task FakePersistence_Load_TracksRequestedScope()
    {
        var fake = new FakeCooldownPersistence
        {
            MapCooldownsToLoad = new[]
            {
                new ScopedCooldownRecord("server-1", "de_dust2", Record(cooldown: 5)),
            },
        };
        var scope = new CooldownScopeQuery(McsCooldownScopeMatchMode.StartsWith, "Tokyo");

        var records = await fake.LoadMapCooldownsAsync(scope);

        Assert.Single(records);
        Assert.Equal("server-1", records[0].ServerKey);
        Assert.Equal("de_dust2", records[0].Name);
        Assert.Single(fake.MapLoadScopes);
        Assert.Equal(scope, fake.MapLoadScopes[0]);
    }
}
