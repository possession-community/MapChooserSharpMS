using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;
using MapChooserSharpMS.Modules.PluginConfig.Enums;
using Microsoft.Extensions.Logging;
using Wuling.Abstract.Tianshi.Surreal;

namespace MapChooserSharpMS.Modules.MapCycle.Services;

internal sealed class SurrealCooldownRepository : ICooldownPersistence, IDisposable
{
    private const string TableMapCooldown = "mcs_map_cooldown";
    private const string TableGroupCooldown = "mcs_group_cooldown";

    private readonly IWulingSurreal _surreal;
    private readonly ILogger _logger;
    private readonly string _surqlDirectory;
    private readonly string _serverKey;
    private readonly Channel<Func<Task>> _queue;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _drainTask;

    internal SurrealCooldownRepository(IWulingSurreal surreal, ILogger logger, string moduleDirectory, string serverKey)
    {
        _surreal = surreal;
        _logger = logger;
        _surqlDirectory = Path.Combine(moduleDirectory, "surql");
        _serverKey = serverKey;

        _queue = Channel.CreateUnbounded<Func<Task>>(new UnboundedChannelOptions
        {
            SingleReader = true,
        });
        _drainTask = Task.Run(RunQueueAsync);
    }

    public void Dispose()
    {
        _queue.Writer.TryComplete();
        try
        {
            _drainTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException) { }
        _cts.Cancel();
        _cts.Dispose();
    }

    private async Task RunQueueAsync()
    {
        try
        {
            await foreach (var workItem in _queue.Reader.ReadAllAsync(_cts.Token))
            {
                try
                {
                    await workItem();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "[CooldownPersistence] Queued operation failed");
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private Task Enqueue(Func<Task> workItem)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.Writer.TryWrite(async () =>
        {
            try
            {
                await workItem();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    private Task<T> Enqueue<T>(Func<Task<T>> workItem)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.Writer.TryWrite(async () =>
        {
            try
            {
                tcs.SetResult(await workItem());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    public Task EnsureSchemasAsync(CancellationToken ct = default)
    {
        return Enqueue(async () =>
        {
            await _surreal.EnsureSchemasAsync(_surqlDirectory, ct);
            _logger.LogInformation("[CooldownPersistence] SurrealDB schemas ensured from {Path}", _surqlDirectory);
        });
    }

    public Task<IReadOnlyList<ScopedCooldownRecord>> LoadMapCooldownsAsync(CooldownScopeQuery scope, CancellationToken ct = default)
    {
        return Enqueue(() => LoadScopedAsync(TableMapCooldown, scope, ct));
    }

    public Task<IReadOnlyList<ScopedCooldownRecord>> LoadGroupCooldownsAsync(CooldownScopeQuery scope, CancellationToken ct = default)
    {
        return Enqueue(() => LoadScopedAsync(TableGroupCooldown, scope, ct));
    }

    public Task SaveMapCooldownAsync(string mapName, CooldownRecord record, CancellationToken ct = default)
    {
        return Enqueue(() => UpsertAsync(TableMapCooldown, mapName, record, ct));
    }

    public Task SaveGroupCooldownAsync(string groupName, CooldownRecord record, CancellationToken ct = default)
    {
        return Enqueue(() => UpsertAsync(TableGroupCooldown, groupName, record, ct));
    }

    public void SaveMapCooldownFireAndForget(string mapName, CooldownRecord record)
    {
        _queue.Writer.TryWrite(async () =>
        {
            try
            {
                await UpsertAsync(TableMapCooldown, mapName, record);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[CooldownPersistence] Fire-and-forget save failed for map {Map}", mapName);
            }
        });
    }

    public void SaveGroupCooldownFireAndForget(string groupName, CooldownRecord record)
    {
        _queue.Writer.TryWrite(async () =>
        {
            try
            {
                await UpsertAsync(TableGroupCooldown, groupName, record);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[CooldownPersistence] Fire-and-forget save failed for group {Group}", groupName);
            }
        });
    }

    public void SaveAllCooldownsFireAndForget(
        IReadOnlyList<(string name, CooldownRecord record)> maps,
        IReadOnlyList<(string name, CooldownRecord record)> groups)
    {
        _queue.Writer.TryWrite(async () =>
        {
            try
            {
                foreach (var (name, record) in maps)
                    await UpsertAsync(TableMapCooldown, name, record);

                foreach (var (name, record) in groups)
                    await UpsertAsync(TableGroupCooldown, name, record);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[CooldownPersistence] Fire-and-forget bulk save failed");
            }
        });
    }

    /// <summary>
    /// Builds the scoped load query. Records are keyed by [server_key, name] but
    /// selected via the indexed server_key field for scope matching.
    /// </summary>
    internal static string BuildLoadSurql(string table, McsCooldownScopeMatchMode mode)
    {
        return mode switch
        {
            McsCooldownScopeMatchMode.Exact
                => $"SELECT * FROM {table} WHERE server_key = $pattern;",
            McsCooldownScopeMatchMode.StartsWith
                => $"SELECT * FROM {table} WHERE string::starts_with(server_key, $pattern);",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown cooldown scope match mode"),
        };
    }

    private async Task UpsertAsync(string table, string name, CooldownRecord record, CancellationToken ct = default)
    {
        var surql = $"UPSERT type::thing('{table}', [$server_key, $name]) SET server_key = $server_key, name = $name, cooldown = $cooldown, timed_cooldown_end = $timed_cooldown_end, last_played_at = $last_played_at, unplayed_count = $unplayed_count, nom_cooldown = $nom_cooldown, nom_timed_cooldown_end = $nom_timed_cooldown_end;";
        var vars = new Dictionary<string, object?>
        {
            ["server_key"] = _serverKey,
            ["name"] = name,
            ["cooldown"] = record.Cooldown,
            ["timed_cooldown_end"] = record.TimedCooldownEnd,
            ["last_played_at"] = record.LastPlayedAt,
            ["unplayed_count"] = record.UnplayedCount,
            ["nom_cooldown"] = record.NomCooldown,
            ["nom_timed_cooldown_end"] = record.NomTimedCooldownEnd,
        };

        await _surreal.WriteAsync(surql, vars, ct);
    }

    private async Task<IReadOnlyList<ScopedCooldownRecord>> LoadScopedAsync(string table, CooldownScopeQuery scope, CancellationToken ct)
    {
        var surql = BuildLoadSurql(table, scope.Mode);
        var vars = new Dictionary<string, object?>
        {
            ["pattern"] = scope.Pattern,
        };
        var dtos = await _surreal.QueryAsync<SurrealCooldownDto>(surql, vars, ct: ct);

        var results = new List<ScopedCooldownRecord>(dtos.Count);
        foreach (var dto in dtos)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.name) || string.IsNullOrEmpty(dto.server_key))
                    continue;

                var record = new CooldownRecord(
                    Cooldown: dto.cooldown,
                    TimedCooldownEnd: dto.timed_cooldown_end,
                    LastPlayedAt: dto.last_played_at,
                    UnplayedCount: dto.unplayed_count,
                    NomCooldown: dto.nom_cooldown,
                    NomTimedCooldownEnd: dto.nom_timed_cooldown_end);

                results.Add(new ScopedCooldownRecord(dto.server_key, dto.name, record));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[CooldownPersistence] Failed to parse cooldown DTO: {Name}", dto.name);
            }
        }

        return results;
    }

    // Property names must match SurrealDB column names exactly (snake_case)
    // — Dahomey.Cbor uses C# property names by default, not [JsonPropertyName]
    internal sealed class SurrealCooldownDto
    {
        public string? server_key { get; set; }
        public string? name { get; set; }
        public int cooldown { get; set; }
        public DateTime timed_cooldown_end { get; set; }
        public DateTime last_played_at { get; set; }
        public int unplayed_count { get; set; }
        public int nom_cooldown { get; set; }
        public DateTime nom_timed_cooldown_end { get; set; }
    }
}
