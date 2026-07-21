using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;
using MapChooserSharpMS.Shared.MapCycle.Cooldown;

namespace MapChooserSharpMS.Modules.MapCycle.Cooldown;

/// <summary>
/// Mutable runtime cooldown state for a single map or group.
/// </summary>
internal sealed class McsCooldownStateEntry : IMcsCooldownState
{
    public int CurrentCooldown { get; set; }
    public DateTime TimedCooldownEndUtc { get; set; } = DateTime.MinValue;
    public DateTime LastPlayedAt { get; set; }
    public int UnplayedCount { get; set; }
    public bool CooldownAuditRecorded { get; set; } = true;
    public int CurrentNominationCooldown { get; set; }
    public DateTime NominationTimedCooldownEndUtc { get; set; } = DateTime.MinValue;

    public bool IsCooldownActive => CurrentCooldown > 0 || TimedCooldownEndUtc > DateTime.UtcNow;

    public bool IsNominationCooldownActive => CurrentNominationCooldown > 0 || NominationTimedCooldownEndUtc > DateTime.UtcNow;
}

internal sealed class McsCooldownStore : IMcsInternalCooldownStore
{
    private static readonly ZeroCooldownState Zero = new();

    private readonly Dictionary<string, McsCooldownStateEntry> _rawMaps = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, McsCooldownStateEntry> _rawGroups = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, McsCooldownStateEntry> _foreignMaps = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, McsCooldownStateEntry> _foreignGroups = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, McsCooldownStateEntry> RawMapEntries => _rawMaps;
    public IReadOnlyDictionary<string, McsCooldownStateEntry> RawGroupEntries => _rawGroups;

    public IMcsCooldownState GetEffectiveMapState(string mapName)
        => GetEffective(_rawMaps, _foreignMaps, mapName);

    public IMcsCooldownState GetEffectiveGroupState(string groupName)
        => GetEffective(_rawGroups, _foreignGroups, groupName);

    public IMcsCooldownState GetOwnMapState(string mapName)
        => _rawMaps.TryGetValue(mapName, out var entry) ? entry : Zero;

    public IMcsCooldownState GetOwnGroupState(string groupName)
        => _rawGroups.TryGetValue(groupName, out var entry) ? entry : Zero;

    public McsCooldownStateEntry GetOrCreateRawMapEntry(string mapName)
        => GetOrCreate(_rawMaps, mapName);

    public McsCooldownStateEntry GetOrCreateRawGroupEntry(string groupName)
        => GetOrCreate(_rawGroups, groupName);

    public bool TryGetRawMapEntry(string mapName, [NotNullWhen(true)] out McsCooldownStateEntry? entry)
        => _rawMaps.TryGetValue(mapName, out entry);

    public bool TryGetRawGroupEntry(string groupName, [NotNullWhen(true)] out McsCooldownStateEntry? entry)
        => _rawGroups.TryGetValue(groupName, out entry);

    public void ApplyLoadedRecords(
        IReadOnlyList<ScopedCooldownRecord> maps,
        IReadOnlyList<ScopedCooldownRecord> groups,
        string ownServerKey)
    {
        _foreignMaps.Clear();
        _foreignGroups.Clear();
        ApplyLoaded(maps, _rawMaps, _foreignMaps, ownServerKey);
        ApplyLoaded(groups, _rawGroups, _foreignGroups, ownServerKey);
    }

    private static void ApplyLoaded(
        IReadOnlyList<ScopedCooldownRecord> records,
        Dictionary<string, McsCooldownStateEntry> raw,
        Dictionary<string, McsCooldownStateEntry> foreign,
        string ownServerKey)
    {
        foreach (var scoped in records)
        {
            if (string.Equals(scoped.ServerKey, ownServerKey, StringComparison.Ordinal))
            {
                var entry = GetOrCreate(raw, scoped.Name);
                entry.CurrentCooldown = scoped.Record.Cooldown;
                entry.TimedCooldownEndUtc = scoped.Record.TimedCooldownEnd;
                entry.LastPlayedAt = scoped.Record.LastPlayedAt;
                entry.UnplayedCount = scoped.Record.UnplayedCount;
                entry.CurrentNominationCooldown = scoped.Record.NomCooldown;
                entry.NominationTimedCooldownEndUtc = scoped.Record.NomTimedCooldownEnd;

                // Loaded state that is already fully expired must not fire a
                // cooldown-expired audit on the next decrement pass.
                if (!entry.IsCooldownActive)
                    entry.CooldownAuditRecorded = true;
            }
            else if (foreign.TryGetValue(scoped.Name, out var aggregate))
            {
                McsCooldownAggregator.CombineInto(aggregate, McsCooldownAggregator.FromRecord(scoped.Record));
            }
            else
            {
                foreign[scoped.Name] = McsCooldownAggregator.FromRecord(scoped.Record);
            }
        }
    }

    private static IMcsCooldownState GetEffective(
        Dictionary<string, McsCooldownStateEntry> raw,
        Dictionary<string, McsCooldownStateEntry> foreign,
        string name)
    {
        bool hasRaw = raw.TryGetValue(name, out var rawEntry);
        bool hasForeign = foreign.TryGetValue(name, out var foreignEntry);

        if (hasRaw && hasForeign)
            return McsCooldownAggregator.Combine(rawEntry!, foreignEntry!);
        if (hasRaw)
            return rawEntry!;
        if (hasForeign)
            return foreignEntry!;
        return Zero;
    }

    private static McsCooldownStateEntry GetOrCreate(Dictionary<string, McsCooldownStateEntry> dict, string name)
    {
        if (!dict.TryGetValue(name, out var entry))
        {
            entry = new McsCooldownStateEntry();
            dict[name] = entry;
        }
        return entry;
    }

    private sealed class ZeroCooldownState : IMcsCooldownState
    {
        public int CurrentCooldown => 0;
        public DateTime TimedCooldownEndUtc => DateTime.MinValue;
        public DateTime LastPlayedAt => DateTime.MinValue;
        public int UnplayedCount => 0;
        public int CurrentNominationCooldown => 0;
        public DateTime NominationTimedCooldownEndUtc => DateTime.MinValue;
        public bool IsCooldownActive => false;
        public bool IsNominationCooldownActive => false;
    }
}
