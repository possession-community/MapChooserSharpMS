using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.MapConfig.Models;
using MapChooserSharpMS.Modules.MapCycle.Cooldown;
using MapChooserSharpMS.Modules.MapCycle.Services;
using MapChooserSharpMS.Shared.Events;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapConfig.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MapChooserSharpMS.Tests.MapCycle.Cooldown;

public class CooldownLifecycleTests
{
    private readonly McsCooldownStore _store = new();
    private readonly FakeMapConfigProvider _provider = new();
    private readonly FakeCooldownPersistence _persistence = new();
    private readonly McsMapCooldownLifecycleService _service;

    public CooldownLifecycleTests()
    {
        // plugin / moduleBase are only dereferenced by the event params'
        // ModulePrefix, which nothing here calls.
        _service = new McsMapCooldownLifecycleService(
            NullLogger.Instance, null!, null!, _provider, new NoopEventManager(), _store);
        _service.SetPersistence(_persistence);
    }

    private IMapConfig AddMap(string name, int cooldown = 3, TimeSpan? timedCooldown = null)
    {
        var map = CooldownTestHelper.CreateMapConfig(name, configCooldown: cooldown, timedCooldown: timedCooldown);
        _provider.Add(map);
        return map;
    }

    [Fact]
    public void MapStart_AppliesFullCooldownToStartedMap()
    {
        var map = AddMap("de_a", cooldown: 3);

        _service.AdvanceCooldownsForMapStart(map);

        // Applied after the tick: the fresh cooldown must not lose a count
        // on the very start it was applied at.
        Assert.Equal(3, _store.GetOwnMapState("de_a").CurrentCooldown);
    }

    [Fact]
    public void MapStart_StampsLastPlayedAndResetsUnplayedCount()
    {
        var map = AddMap("de_a", cooldown: 1);
        var entry = _store.GetOrCreateRawMapEntry("de_a");
        entry.LastPlayedAt = DateTime.UtcNow.AddDays(-1);
        entry.UnplayedCount = 7;

        var before = DateTime.UtcNow;
        _service.AdvanceCooldownsForMapStart(map);

        var state = _store.GetOwnMapState("de_a");
        Assert.Equal(0, state.UnplayedCount);
        Assert.True(state.LastPlayedAt >= before);
    }

    [Fact]
    public void MapStart_TimedCooldownCountsFromStart()
    {
        var map = AddMap("de_a", cooldown: 0, timedCooldown: TimeSpan.FromHours(2));

        var before = DateTime.UtcNow;
        _service.AdvanceCooldownsForMapStart(map);
        var after = DateTime.UtcNow;

        var end = _store.GetOwnMapState("de_a").TimedCooldownEndUtc;
        Assert.InRange(end, before.AddHours(2), after.AddHours(2));
    }

    [Fact]
    public void SubsequentMapStarts_TickThePreviousMapDown()
    {
        var a = AddMap("de_a", cooldown: 2);
        var b = AddMap("de_b", cooldown: 2);
        var c = AddMap("de_c", cooldown: 2);

        _service.AdvanceCooldownsForMapStart(a);
        _service.AdvanceCooldownsForMapStart(b);

        Assert.Equal(1, _store.GetOwnMapState("de_a").CurrentCooldown);
        Assert.Equal(2, _store.GetOwnMapState("de_b").CurrentCooldown);

        _service.AdvanceCooldownsForMapStart(c);

        Assert.Equal(0, _store.GetOwnMapState("de_a").CurrentCooldown);
        Assert.Equal(1, _store.GetOwnMapState("de_b").CurrentCooldown);
    }

    [Fact]
    public void MapStart_WithoutStartedMap_OnlyTicks()
    {
        AddMap("de_a", cooldown: 3);
        _store.GetOrCreateRawMapEntry("de_a").CurrentCooldown = 2;

        _service.AdvanceCooldownsForMapStart(null);

        Assert.Equal(1, _store.GetOwnMapState("de_a").CurrentCooldown);
        Assert.Empty(_persistence.FireAndForgetMaps);
    }

    [Fact]
    public void MapStart_PersistsTickSnapshotAndAppliedState()
    {
        var map = AddMap("de_a", cooldown: 3);
        _store.GetOrCreateRawMapEntry("de_a").CurrentCooldown = 1;

        _service.AdvanceCooldownsForMapStart(map);

        var tick = Assert.Single(_persistence.BulkSaves);
        Assert.Equal(0, Assert.Single(tick.Maps).Item2.Cooldown);

        var applied = Assert.Single(_persistence.FireAndForgetMaps);
        Assert.Equal("de_a", applied.Name);
        Assert.Equal(3, applied.Record.Cooldown);
        Assert.Equal(0, applied.Record.UnplayedCount);
    }

    private sealed class FakeMapConfigProvider : IMcsMapConfigProvider
    {
        private readonly Dictionary<string, IReadOnlyCollection<IMapConfigOverrides>> _maps
            = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, IReadOnlyCollection<IMapGroupConfigOverrides>> _groups
            = new(StringComparer.OrdinalIgnoreCase);

        internal void Add(IMapConfig map)
        {
            _maps[map.MapName] =
            [
                new MapConfigOverrides(map, "", true, false, 0, Array.Empty<DayOfWeek>(), Array.Empty<ITimeRange>()),
            ];
        }

        public IMapConfigToolingService ToolingService => throw new NotSupportedException();
        public IMcsMapSearchService MapSearchService => throw new NotSupportedException();

        public void ReloadConfigs() { }

        public IReadOnlyDictionary<string, IReadOnlyCollection<IMapGroupConfigOverrides>> GetGroupSettings() => _groups;

        public IReadOnlyDictionary<string, IReadOnlyCollection<IMapConfigOverrides>> GetMapConfigs() => _maps;

        public bool TryGetMapConfig(string mapName, [NotNullWhen(true)] out IMapConfig? found)
        {
            found = null;
            if (!_maps.TryGetValue(mapName, out var variants))
                return false;

            foreach (var variant in variants)
            {
                found = variant.MapConfig;
                return true;
            }

            return false;
        }

        public bool TryGetMapConfig(long workshopId, [NotNullWhen(true)] out IMapConfig? found)
        {
            found = null;
            return false;
        }
    }

    private sealed class NoopEventManager : IInternalEventManager
    {
        public void RegisterListener<TListener>(TListener listener) where TListener : IEventListenerBase { }

        public void RemoveListener<TListener>(TListener listener) where TListener : IEventListenerBase { }

        public void Fire<TListener>(Action<TListener> action) where TListener : IEventListenerBase { }

        public McsCancellableEvent FireCancellable<TListener>(Func<TListener, McsCancellableEvent> handler)
            where TListener : IEventListenerBase
            => McsCancellableEvent.Continue;

        public List<TResult> FireCollect<TListener, TResult>(Func<TListener, TResult> selector, bool skipNullResults = true)
            where TListener : IEventListenerBase
            => [];

        public List<TResult> FireCollectMany<TListener, TResult>(Func<TListener, IEnumerable<TResult>> selector)
            where TListener : IEventListenerBase
            => [];

        public TResult? FireWithResult<TListener, TResult>(
            Func<TListener, TResult> selector,
            Func<TResult, bool> shouldStop,
            TResult? defaultValue = default)
            where TListener : IEventListenerBase
            => defaultValue;
    }
}
