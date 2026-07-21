using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.Audit.Services;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.MapCycle;
using MapChooserSharpMS.Modules.MapCycle.Cooldown;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.MapConfig;
using Microsoft.Extensions.Logging;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.MapCycle.Services;

internal sealed class McsMapCooldownLifecycleService
{
    private const string CooldownTypeMap = "map";
    private const string CooldownTypeGroup = "group";

    private readonly ILogger _logger;
    private readonly TnmsPlugin _plugin;
    private readonly PluginModuleBase _moduleBase;
    private readonly IMcsMapConfigProvider _mapConfigProvider;
    private readonly IInternalEventManager _eventManager;
    private readonly IMcsInternalCooldownStore _store;
    private ICooldownPersistence _persistence = NullCooldownPersistence.Instance;
    private volatile IAuditPersistence _auditPersistence = NullAuditPersistence.Instance;
    private volatile string _serverId = "";
    private bool _persistenceEnabled;

    internal McsMapCooldownLifecycleService(
        ILogger logger,
        TnmsPlugin plugin,
        PluginModuleBase moduleBase,
        IMcsMapConfigProvider mapConfigProvider,
        IInternalEventManager eventManager,
        IMcsInternalCooldownStore store)
    {
        _logger = logger;
        _plugin = plugin;
        _moduleBase = moduleBase;
        _mapConfigProvider = mapConfigProvider;
        _eventManager = eventManager;
        _store = store;
    }

    internal void SetPersistence(ICooldownPersistence persistence)
    {
        _persistence = persistence;
        _persistenceEnabled = true;
    }

    internal void SetAuditPersistence(IAuditPersistence auditPersistence, string serverId)
    {
        _serverId = serverId;
        _auditPersistence = auditPersistence;
    }

    internal void ApplyPlayedMapCooldown(IMapConfig playedMap)
    {
        var now = DateTime.UtcNow;
        var settings = playedMap.CooldownSettings;

        var eventParams = new MapCooldownApplyEventParams(
            _plugin, _moduleBase, playedMap,
            settings.ConfigCooldown,
            settings.TimedCooldown);

        _eventManager.Fire<IMapCycleEventListener>(e => e.OnMapCooldownApply(eventParams));

        var entry = _store.GetOrCreateRawMapEntry(playedMap.MapName);

        if (eventParams.IsCancelled)
        {
            _logger.LogInformation("[Cooldown] Cooldown apply cancelled by listener for: {Map}", playedMap.MapName);
        }
        else
        {
            entry.CurrentCooldown = eventParams.Cooldown;

            if (HasAnyCooldownConfigured(settings))
                entry.CooldownAuditRecorded = false;

            if (eventParams.TimedCooldownDuration > TimeSpan.Zero)
                entry.TimedCooldownEndUtc = now + eventParams.TimedCooldownDuration;
        }

        entry.LastPlayedAt = now;
        entry.UnplayedCount = 0;

        if (!IsProvisionalMap(playedMap))
            _persistence.SaveMapCooldownFireAndForget(playedMap.MapName, BuildRecord(entry));

        var appliedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in playedMap.GroupSettings)
        {
            if (!appliedGroups.Add(group.GroupName))
                continue;

            var groupSettings = group.CooldownSettings;
            var groupEntry = _store.GetOrCreateRawGroupEntry(group.GroupName);

            groupEntry.CurrentCooldown = groupSettings.ConfigCooldown;
            groupEntry.LastPlayedAt = now;
            groupEntry.UnplayedCount = 0;

            if (HasAnyCooldownConfigured(groupSettings))
                groupEntry.CooldownAuditRecorded = false;

            if (groupSettings.TimedCooldown > TimeSpan.Zero)
                groupEntry.TimedCooldownEndUtc = now + groupSettings.TimedCooldown;

            _persistence.SaveGroupCooldownFireAndForget(group.GroupName, BuildRecord(groupEntry));
        }

        _logger.LogInformation("[Cooldown] Applied cooldown to played map: {Map}", playedMap.MapName);
    }

    internal void DecrementAllCooldowns()
    {
        var mapRecords = new List<(string name, CooldownRecord record)>();
        var groupRecords = new List<(string name, CooldownRecord record)>();
        var processedMaps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var processedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapName in _mapConfigProvider.GetMapConfigs().Keys)
        {
            if (!processedMaps.Add(mapName))
                continue;

            if (!_mapConfigProvider.TryGetMapConfig(mapName, out var mapConfig))
                continue;

            bool hasAnyConfigured = HasAnyCooldownConfigured(mapConfig.CooldownSettings);

            if (!_store.TryGetRawMapEntry(mapName, out var entry))
            {
                if (!hasAnyConfigured)
                    continue;
                entry = _store.GetOrCreateRawMapEntry(mapName);
            }

            DecrementEntry(entry);
            TrackCooldownExpiry(entry, hasAnyConfigured, mapConfig.MapName, CooldownTypeMap);

            if (!IsProvisionalMap(mapConfig))
                mapRecords.Add((mapConfig.MapName, BuildRecord(entry)));
        }

        foreach (var (groupName, variants) in _mapConfigProvider.GetGroupSettings())
        {
            if (!processedGroups.Add(groupName))
                continue;

            bool hasAnyConfigured = false;
            foreach (var variant in variants)
            {
                if (HasAnyCooldownConfigured(variant.GroupConfig.CooldownSettings))
                {
                    hasAnyConfigured = true;
                    break;
                }
            }

            if (!_store.TryGetRawGroupEntry(groupName, out var entry))
            {
                if (!hasAnyConfigured)
                    continue;
                entry = _store.GetOrCreateRawGroupEntry(groupName);
            }

            DecrementEntry(entry);
            TrackCooldownExpiry(entry, hasAnyConfigured, groupName, CooldownTypeGroup);

            groupRecords.Add((groupName, BuildRecord(entry)));
        }

        // Entries for maps/groups no longer present in config keep ticking so a
        // config re-add doesn't resurrect a stale cooldown, but are not persisted.
        foreach (var (name, entry) in _store.RawMapEntries)
        {
            if (!processedMaps.Contains(name))
                DecrementEntry(entry);
        }

        foreach (var (name, entry) in _store.RawGroupEntries)
        {
            if (!processedGroups.Contains(name))
                DecrementEntry(entry);
        }

        if (_persistenceEnabled && (mapRecords.Count > 0 || groupRecords.Count > 0))
            _persistence.SaveAllCooldownsFireAndForget(mapRecords, groupRecords);
    }

    private void TrackCooldownExpiry(McsCooldownStateEntry entry, bool hasAnyConfigured, string name, string cooldownType)
    {
        if (!hasAnyConfigured || entry.IsCooldownActive || entry.LastPlayedAt == DateTime.MinValue)
            return;

        entry.UnplayedCount++;

        if (entry.CooldownAuditRecorded)
            return;

        entry.CooldownAuditRecorded = true;
        _auditPersistence.InsertCooldownExpiredFireAndForget(
            new AuditCooldownExpired(name, cooldownType, DateTime.UtcNow, _serverId));
    }

    internal void ApplyNominationCooldown(IMapConfig map)
    {
        var settings = map.CooldownSettings;

        _logger.LogDebug("[NomCD] Applying to {Map}: ConfigNomCD={ConfigNomCD}, NomTimedCD={NomTimedCD}",
            map.MapName, settings.ConfigNominationCooldown, settings.NominationTimedCooldown);

        var entry = _store.GetOrCreateRawMapEntry(map.MapName);

        if (settings.ConfigNominationCooldown > 0)
            entry.CurrentNominationCooldown = settings.ConfigNominationCooldown;

        if (settings.NominationTimedCooldown > TimeSpan.Zero)
            entry.NominationTimedCooldownEndUtc = DateTime.UtcNow + settings.NominationTimedCooldown;

        if (!IsProvisionalMap(map))
            _persistence.SaveMapCooldownFireAndForget(map.MapName, BuildRecord(entry));
    }

    internal async Task<(IReadOnlyList<ScopedCooldownRecord> Maps, IReadOnlyList<ScopedCooldownRecord> Groups)?> FetchCooldownsFromDatabaseAsync(CooldownScopeQuery scope)
    {
        try
        {
            var mapRecords = await _persistence.LoadMapCooldownsAsync(scope);
            var groupRecords = await _persistence.LoadGroupCooldownsAsync(scope);

            _logger.LogInformation(
                "[Cooldown] Fetched {MapCount} map and {GroupCount} group cooldowns from database (scope: {Mode} '{Pattern}')",
                mapRecords.Count, groupRecords.Count, scope.Mode, scope.Pattern);

            return (mapRecords, groupRecords);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cooldown] Failed to load cooldowns from database; continuing with defaults");
            return null;
        }
    }

    private static void DecrementEntry(McsCooldownStateEntry entry)
    {
        if (entry.CurrentCooldown > 0 && entry.CurrentCooldown < int.MaxValue)
            entry.CurrentCooldown--;

        if (entry.CurrentNominationCooldown > 0 && entry.CurrentNominationCooldown < int.MaxValue)
            entry.CurrentNominationCooldown--;
    }

    private static bool HasAnyCooldownConfigured(IMcsCooldownSettings settings)
        => settings.ConfigCooldown > 0 || settings.TimedCooldown > TimeSpan.Zero;

    private static CooldownRecord BuildRecord(McsCooldownStateEntry entry)
    {
        return new CooldownRecord(
            Cooldown: entry.CurrentCooldown,
            TimedCooldownEnd: entry.TimedCooldownEndUtc,
            LastPlayedAt: entry.LastPlayedAt,
            UnplayedCount: entry.UnplayedCount,
            NomCooldown: entry.CurrentNominationCooldown,
            NomTimedCooldownEnd: entry.NominationTimedCooldownEndUtc);
    }

    private static bool IsProvisionalMap(IMapConfig map)
    {
        return map is MapChooserSharpMS.Modules.MapConfig.Models.MapConfig { IsProvisional: true };
    }
}
