using System;
using System.Collections.Generic;
using MapChooserSharpMS.Modules.MapCycle.Cooldown;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;
using Xunit;

namespace MapChooserSharpMS.Tests.MapCycle.Cooldown;

public class CooldownStoreTests
{
    private const string OwnServer = "server-1";
    private const string OtherServer = "server-2";
    private const string ThirdServer = "server-3";

    private static readonly DateTime Past = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static DateTime Future(int hours = 1) => DateTime.UtcNow.AddHours(hours);

    private static CooldownRecord Record(
        int cooldown = 0,
        DateTime timedEnd = default,
        DateTime lastPlayed = default,
        int unplayed = 0,
        int nomCooldown = 0,
        DateTime nomTimedEnd = default)
    {
        return new CooldownRecord(
            Cooldown: cooldown,
            TimedCooldownEnd: timedEnd == default ? DateTime.MinValue : timedEnd,
            LastPlayedAt: lastPlayed == default ? DateTime.MinValue : lastPlayed,
            UnplayedCount: unplayed,
            NomCooldown: nomCooldown,
            NomTimedCooldownEnd: nomTimedEnd == default ? DateTime.MinValue : nomTimedEnd);
    }

    private static ScopedCooldownRecord Scoped(string serverKey, string name, CooldownRecord record)
        => new(serverKey, name, record);

    private static void ApplyMaps(McsCooldownStore store, params ScopedCooldownRecord[] maps)
        => store.ApplyLoadedRecords(maps, Array.Empty<ScopedCooldownRecord>(), OwnServer);

    #region Zero state

    [Fact]
    public void GetEffectiveMapState_UnknownMap_ReturnsZeroState()
    {
        var store = new McsCooldownStore();

        var state = store.GetEffectiveMapState("de_unknown");

        Assert.Equal(0, state.CurrentCooldown);
        Assert.Equal(DateTime.MinValue, state.TimedCooldownEndUtc);
        Assert.Equal(DateTime.MinValue, state.LastPlayedAt);
        Assert.Equal(0, state.UnplayedCount);
        Assert.False(state.IsCooldownActive);
        Assert.False(state.IsNominationCooldownActive);
    }

    [Fact]
    public void GetOwnMapState_UnknownMap_ReturnsZeroState()
    {
        var store = new McsCooldownStore();

        Assert.False(store.GetOwnMapState("de_unknown").IsCooldownActive);
        Assert.False(store.GetOwnGroupState("unknown_group").IsCooldownActive);
    }

    #endregion

    #region Raw layer

    [Fact]
    public void GetOrCreateRawMapEntry_SameName_ReturnsSameInstance()
    {
        var store = new McsCooldownStore();

        var first = store.GetOrCreateRawMapEntry("de_dust2");
        var second = store.GetOrCreateRawMapEntry("DE_DUST2");

        Assert.Same(first, second);
    }

    [Fact]
    public void GetEffectiveMapState_RawOnly_ReturnsRawValues()
    {
        var store = new McsCooldownStore();
        var entry = store.GetOrCreateRawMapEntry("de_dust2");
        entry.CurrentCooldown = 3;
        entry.UnplayedCount = 7;
        entry.LastPlayedAt = Past;

        var effective = store.GetEffectiveMapState("de_dust2");

        Assert.Equal(3, effective.CurrentCooldown);
        Assert.Equal(7, effective.UnplayedCount);
        Assert.Equal(Past, effective.LastPlayedAt);
        Assert.True(effective.IsCooldownActive);
    }

    #endregion

    #region ApplyLoadedRecords — raw seeding

    [Fact]
    public void ApplyLoadedRecords_OwnServerRecord_SeedsRawLayer()
    {
        var store = new McsCooldownStore();

        ApplyMaps(store, Scoped(OwnServer, "de_dust2", Record(cooldown: 5, lastPlayed: Past, unplayed: 2)));

        var own = store.GetOwnMapState("de_dust2");
        Assert.Equal(5, own.CurrentCooldown);
        Assert.Equal(Past, own.LastPlayedAt);
        Assert.Equal(2, own.UnplayedCount);
    }

    [Fact]
    public void ApplyLoadedRecords_LoadedExpiredState_MarksAuditRecorded()
    {
        var store = new McsCooldownStore();
        var entry = store.GetOrCreateRawMapEntry("de_dust2");
        entry.CooldownAuditRecorded = false;

        ApplyMaps(store, Scoped(OwnServer, "de_dust2", Record(cooldown: 0, lastPlayed: Past)));

        Assert.True(entry.CooldownAuditRecorded);
    }

    [Fact]
    public void ApplyLoadedRecords_LoadedActiveState_KeepsAuditFlagUntouched()
    {
        var store = new McsCooldownStore();
        var entry = store.GetOrCreateRawMapEntry("de_dust2");
        entry.CooldownAuditRecorded = false;

        ApplyMaps(store, Scoped(OwnServer, "de_dust2", Record(cooldown: 3, lastPlayed: Past)));

        Assert.False(entry.CooldownAuditRecorded);
    }

    [Fact]
    public void ApplyLoadedRecords_IsIdempotent()
    {
        var store = new McsCooldownStore();
        var records = new[]
        {
            Scoped(OwnServer, "de_dust2", Record(cooldown: 5, lastPlayed: Past, unplayed: 2)),
            Scoped(OtherServer, "de_dust2", Record(cooldown: 8, lastPlayed: Past)),
        };

        ApplyMaps(store, records);
        var first = Snapshot(store.GetEffectiveMapState("de_dust2"));

        ApplyMaps(store, records);
        var second = Snapshot(store.GetEffectiveMapState("de_dust2"));

        Assert.Equal(first, second);
    }

    private static (int, DateTime, DateTime, int, int, DateTime) Snapshot(Shared.MapCycle.Cooldown.IMcsCooldownState s)
        => (s.CurrentCooldown, s.TimedCooldownEndUtc, s.LastPlayedAt, s.UnplayedCount,
            s.CurrentNominationCooldown, s.NominationTimedCooldownEndUtc);

    #endregion

    #region Aggregation rules

    [Fact]
    public void GetEffectiveMapState_ForeignHigherCooldown_TakesMax()
    {
        var store = new McsCooldownStore();

        ApplyMaps(store,
            Scoped(OwnServer, "de_dust2", Record(cooldown: 2, lastPlayed: Past, unplayed: 5)),
            Scoped(OtherServer, "de_dust2", Record(cooldown: 9, lastPlayed: Past.AddDays(1), unplayed: 1)));

        var effective = store.GetEffectiveMapState("de_dust2");

        Assert.Equal(9, effective.CurrentCooldown);
        Assert.Equal(Past.AddDays(1), effective.LastPlayedAt);
        Assert.Equal(1, effective.UnplayedCount);
    }

    [Fact]
    public void GetEffectiveMapState_ForeignLaterTimedEnd_TakesLatest()
    {
        var store = new McsCooldownStore();
        var laterEnd = Future(4);

        ApplyMaps(store,
            Scoped(OwnServer, "de_dust2", Record(timedEnd: Future(1), lastPlayed: Past)),
            Scoped(OtherServer, "de_dust2", Record(timedEnd: laterEnd, lastPlayed: Past)));

        Assert.Equal(laterEnd, store.GetEffectiveMapState("de_dust2").TimedCooldownEndUtc);
        Assert.True(store.GetEffectiveMapState("de_dust2").IsCooldownActive);
    }

    [Fact]
    public void GetEffectiveMapState_NominationAxes_TakeMaxAndLatest()
    {
        var store = new McsCooldownStore();
        var laterNomEnd = Future(3);

        ApplyMaps(store,
            Scoped(OwnServer, "de_dust2", Record(nomCooldown: 1, nomTimedEnd: Future(1), lastPlayed: Past)),
            Scoped(OtherServer, "de_dust2", Record(nomCooldown: 4, nomTimedEnd: laterNomEnd, lastPlayed: Past)));

        var effective = store.GetEffectiveMapState("de_dust2");

        Assert.Equal(4, effective.CurrentNominationCooldown);
        Assert.Equal(laterNomEnd, effective.NominationTimedCooldownEndUtc);
        Assert.True(effective.IsNominationCooldownActive);
    }

    [Fact]
    public void GetEffectiveMapState_NeverPlayedRaw_DoesNotDragDownUnplayedCount()
    {
        var store = new McsCooldownStore();
        // Raw entry exists (e.g. created by decrement pass) but never played here.
        store.GetOrCreateRawMapEntry("de_dust2");

        ApplyMaps(store, Scoped(OtherServer, "de_dust2", Record(lastPlayed: Past, unplayed: 50)));

        Assert.Equal(50, store.GetEffectiveMapState("de_dust2").UnplayedCount);
    }

    [Fact]
    public void GetEffectiveMapState_ExcludedOnForeignServer_DominatesAggregate()
    {
        var store = new McsCooldownStore();

        ApplyMaps(store,
            Scoped(OwnServer, "de_dust2", Record(cooldown: 0, lastPlayed: Past)),
            Scoped(OtherServer, "de_dust2", Record(cooldown: int.MaxValue, lastPlayed: Past)));

        Assert.Equal(int.MaxValue, store.GetEffectiveMapState("de_dust2").CurrentCooldown);
        Assert.Equal(0, store.GetOwnMapState("de_dust2").CurrentCooldown);
    }

    [Fact]
    public void GetEffectiveMapState_MultipleForeignServers_AllAggregated()
    {
        var store = new McsCooldownStore();

        ApplyMaps(store,
            Scoped(OtherServer, "de_dust2", Record(cooldown: 3, lastPlayed: Past, unplayed: 10)),
            Scoped(ThirdServer, "de_dust2", Record(cooldown: 7, lastPlayed: Past.AddDays(2), unplayed: 4)));

        var effective = store.GetEffectiveMapState("de_dust2");

        Assert.Equal(7, effective.CurrentCooldown);
        Assert.Equal(Past.AddDays(2), effective.LastPlayedAt);
        Assert.Equal(4, effective.UnplayedCount);
    }

    [Fact]
    public void GetEffectiveMapState_RawMutatedAfterLoad_ReflectsForeignMax()
    {
        var store = new McsCooldownStore();

        ApplyMaps(store,
            Scoped(OwnServer, "de_dust2", Record(cooldown: 5, lastPlayed: Past)),
            Scoped(OtherServer, "de_dust2", Record(cooldown: 4, lastPlayed: Past)));

        // Simulate decrement passes on our raw layer only.
        Assert.True(store.TryGetRawMapEntry("de_dust2", out var entry));
        entry.CurrentCooldown = 1;

        // Foreign snapshot still holds 4 — effective takes the max.
        Assert.Equal(4, store.GetEffectiveMapState("de_dust2").CurrentCooldown);
        Assert.Equal(1, store.GetOwnMapState("de_dust2").CurrentCooldown);
    }

    [Fact]
    public void GetEffectiveMapState_DoesNotMutateRawOrForeign()
    {
        var store = new McsCooldownStore();

        ApplyMaps(store,
            Scoped(OwnServer, "de_dust2", Record(cooldown: 2, lastPlayed: Past, unplayed: 5)),
            Scoped(OtherServer, "de_dust2", Record(cooldown: 9, lastPlayed: Past, unplayed: 1)));

        _ = store.GetEffectiveMapState("de_dust2");
        _ = store.GetEffectiveMapState("de_dust2");

        Assert.Equal(2, store.GetOwnMapState("de_dust2").CurrentCooldown);
        Assert.Equal(5, store.GetOwnMapState("de_dust2").UnplayedCount);
    }

    #endregion

    #region Groups

    [Fact]
    public void ApplyLoadedRecords_GroupRecords_AggregateSeparatelyFromMaps()
    {
        var store = new McsCooldownStore();

        store.ApplyLoadedRecords(
            new List<ScopedCooldownRecord> { Scoped(OwnServer, "shared_name", Record(cooldown: 2, lastPlayed: Past)) },
            new List<ScopedCooldownRecord> { Scoped(OtherServer, "shared_name", Record(cooldown: 6, lastPlayed: Past)) },
            OwnServer);

        Assert.Equal(2, store.GetEffectiveMapState("shared_name").CurrentCooldown);
        Assert.Equal(6, store.GetEffectiveGroupState("shared_name").CurrentCooldown);
        Assert.Equal(0, store.GetOwnGroupState("shared_name").CurrentCooldown);
    }

    #endregion
}
