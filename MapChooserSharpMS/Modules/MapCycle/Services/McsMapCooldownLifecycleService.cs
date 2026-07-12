using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.Audit.Services;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.MapCycle;
using MapChooserSharpMS.Modules.MapConfig.Models;
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
    private ICooldownPersistence _persistence = NullCooldownPersistence.Instance;
    private volatile IAuditPersistence _auditPersistence = NullAuditPersistence.Instance;
    private volatile string _serverId = "";
    private bool _persistenceEnabled;

    internal McsMapCooldownLifecycleService(
        ILogger logger,
        TnmsPlugin plugin,
        PluginModuleBase moduleBase,
        IMcsMapConfigProvider mapConfigProvider,
        IInternalEventManager eventManager)
    {
        _logger = logger;
        _plugin = plugin;
        _moduleBase = moduleBase;
        _mapConfigProvider = mapConfigProvider;
        _eventManager = eventManager;
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

        if (playedMap.CooldownConfig is CooldownConfig mapCc)
        {
            var eventParams = new MapCooldownApplyEventParams(
                _plugin, _moduleBase, playedMap,
                mapCc.ConfigCooldown,
                mapCc.TimedCooldown);

            _eventManager.Fire<IMapCycleEventListener>(e => e.OnMapCooldownApply(eventParams));

            if (eventParams.IsCancelled)
            {
                _logger.LogInformation("[Cooldown] Cooldown apply cancelled by listener for: {Map}", playedMap.MapName);
            }
            else
            {
                mapCc.CurrentCooldown = eventParams.Cooldown;

                if (mapCc.HasAnyCooldownConfigured)
                    mapCc.CooldownAuditRecorded = false;

                if (eventParams.TimedCooldownDuration > TimeSpan.Zero)
                    mapCc.TimedCooldownEndUtc = now + eventParams.TimedCooldownDuration;
            }

            mapCc.LastPlayedAt = now;
            mapCc.UnplayedCount = 0;

            if (!IsProvisionalMap(playedMap))
                _persistence.SaveMapCooldownFireAndForget(playedMap.MapName, BuildMapRecord(mapCc));
        }

        var savedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in playedMap.GroupSettings)
        {
            if (group.CooldownConfig is not CooldownConfig groupCc)
                continue;

            groupCc.CurrentCooldown = groupCc.ConfigCooldown;
            groupCc.LastPlayedAt = now;
            groupCc.UnplayedCount = 0;

            if (groupCc.HasAnyCooldownConfigured)
                groupCc.CooldownAuditRecorded = false;

            if (groupCc.TimedCooldown > TimeSpan.Zero)
                groupCc.TimedCooldownEndUtc = now + groupCc.TimedCooldown;

            if (savedGroups.Add(group.GroupName))
                _persistence.SaveGroupCooldownFireAndForget(group.GroupName, BuildGroupRecord(groupCc));
        }

        _logger.LogInformation("[Cooldown] Applied cooldown to played map: {Map}", playedMap.MapName);
    }

    internal void DecrementAllCooldowns()
    {
        var decremented = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var mapRecords = new List<(string name, CooldownRecord record)>();
        var groupRecords = new List<(string name, CooldownRecord record)>();
        var savedMaps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var savedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in _mapConfigProvider.GetMapConfigs())
        {
            foreach (var mapEntry in entry.Value)
            {
                if (decremented.Add(DedupKey(mapEntry.MapConfig.CooldownConfig)))
                {
                    DecrementCooldownConfig(mapEntry.MapConfig.CooldownConfig);

                    if (mapEntry.MapConfig.CooldownConfig is CooldownConfig mapCc)
                    {
                        TrackCooldownExpiry(mapCc, mapEntry.MapConfig.MapName, CooldownTypeMap);

                        if (!IsProvisionalMap(mapEntry.MapConfig) && savedMaps.Add(mapEntry.MapConfig.MapName))
                            mapRecords.Add((mapEntry.MapConfig.MapName, BuildMapRecord(mapCc)));
                    }
                }

                foreach (var group in mapEntry.MapConfig.GroupSettings)
                {
                    if (decremented.Add(DedupKey(group.CooldownConfig)))
                    {
                        DecrementCooldownConfig(group.CooldownConfig);

                        if (group.CooldownConfig is CooldownConfig groupCc)
                        {
                            TrackCooldownExpiry(groupCc, group.GroupName, CooldownTypeGroup);

                            if (savedGroups.Add(group.GroupName))
                                groupRecords.Add((group.GroupName, BuildGroupRecord(groupCc)));
                        }
                    }
                }
            }
        }

        if (_persistenceEnabled && (mapRecords.Count > 0 || groupRecords.Count > 0))
            _persistence.SaveAllCooldownsFireAndForget(mapRecords, groupRecords);
    }

    private static object DedupKey(ICooldownConfig config)
        => config is CooldownConfig cc ? cc.RuntimeState : config;

    private void TrackCooldownExpiry(CooldownConfig cc, string name, string cooldownType)
    {
        if (!cc.HasAnyCooldownConfigured || !cc.IsFullyAvailable || cc.LastPlayedAt == DateTime.MinValue)
            return;

        cc.UnplayedCount++;

        if (cc.CooldownAuditRecorded)
            return;

        cc.CooldownAuditRecorded = true;
        _auditPersistence.InsertCooldownExpiredFireAndForget(
            new AuditCooldownExpired(name, cooldownType, DateTime.UtcNow, _serverId));
    }

    internal void ApplyNominationCooldown(IMapConfig map)
    {
        if (map.CooldownConfig is not CooldownConfig cc)
            return;

        _logger.LogDebug("[NomCD] Applying to {Map}: ConfigNomCD={ConfigNomCD}, NomTimedCD={NomTimedCD}",
            map.MapName, cc.ConfigNominationCooldown, cc.NominationTimedCooldown);

        if (cc.ConfigNominationCooldown > 0)
            cc.CurrentNominationCooldown = cc.ConfigNominationCooldown;

        if (cc.NominationTimedCooldown > TimeSpan.Zero)
            cc.NominationTimedCooldownEndUtc = DateTime.UtcNow + cc.NominationTimedCooldown;

        if (!IsProvisionalMap(map))
            _persistence.SaveMapCooldownFireAndForget(map.MapName, BuildMapRecord(cc));
    }

    internal async Task<(IReadOnlyList<NamedCooldownRecord> Maps, IReadOnlyList<NamedCooldownRecord> Groups)?> FetchCooldownsFromDatabaseAsync()
    {
        try
        {
            var mapRecords = await _persistence.LoadAllMapCooldownsAsync();
            var groupRecords = await _persistence.LoadAllGroupCooldownsAsync();

            _logger.LogInformation(
                "[Cooldown] Fetched {MapCount} map and {GroupCount} group cooldowns from database",
                mapRecords.Count, groupRecords.Count);

            return (mapRecords, groupRecords);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cooldown] Failed to load cooldowns from database; continuing with defaults");
            return null;
        }
    }

    internal void ApplyLoadedCooldowns(IReadOnlyList<NamedCooldownRecord> mapRecords, IReadOnlyList<NamedCooldownRecord> groupRecords)
    {
        ApplyLoadedMapCooldowns(mapRecords);
        ApplyLoadedGroupCooldowns(groupRecords);
    }

    private void ApplyLoadedMapCooldowns(IReadOnlyList<NamedCooldownRecord> records)
    {
        foreach (var named in records)
        {
            if (!_mapConfigProvider.TryGetMapConfig(named.Name, out var mapConfig))
                continue;

            if (mapConfig.CooldownConfig is not CooldownConfig cc)
                continue;

            cc.CurrentCooldown = named.Record.Cooldown;
            cc.TimedCooldownEndUtc = named.Record.TimedCooldownEnd;
            cc.LastPlayedAt = named.Record.LastPlayedAt;
            cc.CurrentNominationCooldown = named.Record.NomCooldown;
            cc.NominationTimedCooldownEndUtc = named.Record.NomTimedCooldownEnd;

            if (cc.IsFullyAvailable)
                cc.CooldownAuditRecorded = true;
        }
    }

    private void ApplyLoadedGroupCooldowns(IReadOnlyList<NamedCooldownRecord> records)
    {
        var groupSettings = _mapConfigProvider.GetGroupSettings();
        var applied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var named in records)
        {
            if (!groupSettings.TryGetValue(named.Name, out var variants))
                continue;

            if (!applied.Add(named.Name))
                continue;

            foreach (var variant in variants)
            {
                if (variant.GroupConfig.CooldownConfig is not CooldownConfig cc)
                    continue;

                cc.CurrentCooldown = named.Record.Cooldown;
                cc.TimedCooldownEndUtc = named.Record.TimedCooldownEnd;
                cc.LastPlayedAt = named.Record.LastPlayedAt;
                cc.CurrentNominationCooldown = named.Record.NomCooldown;
                cc.NominationTimedCooldownEndUtc = named.Record.NomTimedCooldownEnd;

                if (cc.IsFullyAvailable)
                    cc.CooldownAuditRecorded = true;
            }
        }
    }

    private static void DecrementCooldownConfig(ICooldownConfig config)
    {
        if (config is not CooldownConfig cc)
            return;

        if (cc.CurrentCooldown > 0 && cc.CurrentCooldown < int.MaxValue)
            cc.CurrentCooldown--;

        if (cc.CurrentNominationCooldown > 0 && cc.CurrentNominationCooldown < int.MaxValue)
            cc.CurrentNominationCooldown--;
    }

    private static CooldownRecord BuildMapRecord(CooldownConfig cc)
    {
        return new CooldownRecord(
            Cooldown: cc.CurrentCooldown,
            TimedCooldownEnd: cc.TimedCooldownEndUtc,
            LastPlayedAt: cc.LastPlayedAt,
            NomCooldown: cc.CurrentNominationCooldown,
            NomTimedCooldownEnd: cc.NominationTimedCooldownEndUtc,
            LastNominatedAt: DateTime.MinValue);
    }

    private static CooldownRecord BuildGroupRecord(CooldownConfig cc)
    {
        return new CooldownRecord(
            Cooldown: cc.CurrentCooldown,
            TimedCooldownEnd: cc.TimedCooldownEndUtc,
            LastPlayedAt: cc.LastPlayedAt,
            NomCooldown: cc.CurrentNominationCooldown,
            NomTimedCooldownEnd: cc.NominationTimedCooldownEndUtc,
            LastNominatedAt: DateTime.MinValue);
    }

    private static bool IsProvisionalMap(IMapConfig map)
    {
        return map is MapChooserSharpMS.Modules.MapConfig.Models.MapConfig { IsProvisional: true };
    }
}
