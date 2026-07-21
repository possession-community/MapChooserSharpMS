using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;
using MapChooserSharpMS.Shared.MapCycle.Cooldown;

namespace MapChooserSharpMS.Modules.MapCycle.Cooldown;

internal interface IMcsInternalCooldownStore : IMcsCooldownStore
{
    /// <summary>
    /// Mutable raw (own-server) entry for a map, created on first access.
    /// All writes go through raw entries; they are what gets persisted.
    /// </summary>
    McsCooldownStateEntry GetOrCreateRawMapEntry(string mapName);

    McsCooldownStateEntry GetOrCreateRawGroupEntry(string groupName);

    bool TryGetRawMapEntry(string mapName, [NotNullWhen(true)] out McsCooldownStateEntry? entry);

    bool TryGetRawGroupEntry(string groupName, [NotNullWhen(true)] out McsCooldownStateEntry? entry);

    IReadOnlyDictionary<string, McsCooldownStateEntry> RawMapEntries { get; }

    IReadOnlyDictionary<string, McsCooldownStateEntry> RawGroupEntries { get; }

    /// <summary>
    /// Applies a database load: records with <paramref name="ownServerKey"/> seed the
    /// raw layer, all other records rebuild the foreign aggregate layer from scratch.
    /// </summary>
    void ApplyLoadedRecords(
        IReadOnlyList<ScopedCooldownRecord> maps,
        IReadOnlyList<ScopedCooldownRecord> groups,
        string ownServerKey);
}
