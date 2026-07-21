using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.PluginConfig.Enums;

namespace MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;

internal interface ICooldownPersistence
{
    Task EnsureSchemasAsync(CancellationToken ct = default);

    /// <summary>
    /// Loads all cooldown records whose server key matches the scope.
    /// </summary>
    Task<IReadOnlyList<ScopedCooldownRecord>> LoadMapCooldownsAsync(CooldownScopeQuery scope, CancellationToken ct = default);

    Task<IReadOnlyList<ScopedCooldownRecord>> LoadGroupCooldownsAsync(CooldownScopeQuery scope, CancellationToken ct = default);

    // Writes always target this server's own record (implementations carry the server key).

    Task SaveMapCooldownAsync(string mapName, CooldownRecord record, CancellationToken ct = default);

    Task SaveGroupCooldownAsync(string groupName, CooldownRecord record, CancellationToken ct = default);

    void SaveMapCooldownFireAndForget(string mapName, CooldownRecord record);

    void SaveGroupCooldownFireAndForget(string groupName, CooldownRecord record);

    void SaveAllCooldownsFireAndForget(
        IReadOnlyList<(string name, CooldownRecord record)> maps,
        IReadOnlyList<(string name, CooldownRecord record)> groups);
}

/// <summary>
/// Resolved cooldown load scope. <see cref="Pattern"/> is the concrete pattern to
/// match against record server keys — an empty configured pattern must be replaced
/// with this server's own key before constructing this.
/// </summary>
internal readonly record struct CooldownScopeQuery(McsCooldownScopeMatchMode Mode, string Pattern);

internal sealed record CooldownRecord(
    int Cooldown,
    DateTime TimedCooldownEnd,
    DateTime LastPlayedAt,
    int UnplayedCount,
    int NomCooldown,
    DateTime NomTimedCooldownEnd);

internal sealed record ScopedCooldownRecord(
    string ServerKey,
    string Name,
    CooldownRecord Record);
