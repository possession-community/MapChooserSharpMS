using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;

namespace MapChooserSharpMS.Tests.MapCycle.Cooldown;

internal sealed class FakeCooldownPersistence : ICooldownPersistence
{
    internal List<(string Name, CooldownRecord Record)> SavedMaps { get; } = new();
    internal List<(string Name, CooldownRecord Record)> SavedGroups { get; } = new();
    internal List<(string Name, CooldownRecord Record)> FireAndForgetMaps { get; } = new();
    internal List<(string Name, CooldownRecord Record)> FireAndForgetGroups { get; } = new();
    internal List<(IReadOnlyList<(string, CooldownRecord)> Maps, IReadOnlyList<(string, CooldownRecord)> Groups)> BulkSaves { get; } = new();

    internal IReadOnlyList<NamedCooldownRecord> MapCooldownsToLoad { get; set; } = Array.Empty<NamedCooldownRecord>();
    internal IReadOnlyList<NamedCooldownRecord> GroupCooldownsToLoad { get; set; } = Array.Empty<NamedCooldownRecord>();

    internal bool SchemaEnsured { get; private set; }

    public Task EnsureSchemasAsync(CancellationToken ct = default)
    {
        SchemaEnsured = true;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<NamedCooldownRecord>> LoadAllMapCooldownsAsync(CancellationToken ct = default)
        => Task.FromResult(MapCooldownsToLoad);

    public Task<IReadOnlyList<NamedCooldownRecord>> LoadAllGroupCooldownsAsync(CancellationToken ct = default)
        => Task.FromResult(GroupCooldownsToLoad);

    public Task SaveMapCooldownAsync(string mapName, CooldownRecord record, CancellationToken ct = default)
    {
        SavedMaps.Add((mapName, record));
        return Task.CompletedTask;
    }

    public Task SaveGroupCooldownAsync(string groupName, CooldownRecord record, CancellationToken ct = default)
    {
        SavedGroups.Add((groupName, record));
        return Task.CompletedTask;
    }

    public void SaveMapCooldownFireAndForget(string mapName, CooldownRecord record)
    {
        FireAndForgetMaps.Add((mapName, record));
    }

    public void SaveGroupCooldownFireAndForget(string groupName, CooldownRecord record)
    {
        FireAndForgetGroups.Add((groupName, record));
    }

    public void SaveAllCooldownsFireAndForget(
        IReadOnlyList<(string name, CooldownRecord record)> maps,
        IReadOnlyList<(string name, CooldownRecord record)> groups)
    {
        BulkSaves.Add((maps, groups));
    }
}
