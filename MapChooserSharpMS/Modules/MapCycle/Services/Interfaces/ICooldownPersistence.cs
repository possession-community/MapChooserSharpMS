using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;

internal interface ICooldownPersistence
{
    Task EnsureSchemasAsync(CancellationToken ct = default);

    Task<IReadOnlyList<NamedCooldownRecord>> LoadAllMapCooldownsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<NamedCooldownRecord>> LoadAllGroupCooldownsAsync(CancellationToken ct = default);

    Task SaveMapCooldownAsync(string mapName, CooldownRecord record, CancellationToken ct = default);

    Task SaveGroupCooldownAsync(string groupName, CooldownRecord record, CancellationToken ct = default);

    void SaveMapCooldownFireAndForget(string mapName, CooldownRecord record);

    void SaveGroupCooldownFireAndForget(string groupName, CooldownRecord record);

    void SaveAllCooldownsFireAndForget(
        IReadOnlyList<(string name, CooldownRecord record)> maps,
        IReadOnlyList<(string name, CooldownRecord record)> groups);
}

internal sealed record CooldownRecord(
    int Cooldown,
    DateTime TimedCooldownEnd,
    DateTime LastPlayedAt,
    int UnplayedCount,
    int NomCooldown,
    DateTime NomTimedCooldownEnd,
    DateTime LastNominatedAt);

internal sealed record NamedCooldownRecord(
    string Name,
    CooldownRecord Record);
