using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;

namespace MapChooserSharpMS.Modules.MapCycle.Services;

internal sealed class NullCooldownPersistence : ICooldownPersistence
{
    internal static readonly NullCooldownPersistence Instance = new();

    private static readonly IReadOnlyList<ScopedCooldownRecord> EmptyRecords = Array.Empty<ScopedCooldownRecord>();

    public Task EnsureSchemasAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task<IReadOnlyList<ScopedCooldownRecord>> LoadMapCooldownsAsync(CooldownScopeQuery scope, CancellationToken ct = default)
        => Task.FromResult(EmptyRecords);

    public Task<IReadOnlyList<ScopedCooldownRecord>> LoadGroupCooldownsAsync(CooldownScopeQuery scope, CancellationToken ct = default)
        => Task.FromResult(EmptyRecords);

    public Task SaveMapCooldownAsync(string mapName, CooldownRecord record, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SaveGroupCooldownAsync(string groupName, CooldownRecord record, CancellationToken ct = default)
        => Task.CompletedTask;

    public void SaveMapCooldownFireAndForget(string mapName, CooldownRecord record) { }

    public void SaveGroupCooldownFireAndForget(string groupName, CooldownRecord record) { }

    public void SaveAllCooldownsFireAndForget(
        IReadOnlyList<(string name, CooldownRecord record)> maps,
        IReadOnlyList<(string name, CooldownRecord record)> groups) { }
}
