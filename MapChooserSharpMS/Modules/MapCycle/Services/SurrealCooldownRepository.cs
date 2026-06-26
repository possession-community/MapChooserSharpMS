using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Wuling.Abstract.Tianshi.Surreal;

namespace MapChooserSharpMS.Modules.MapCycle.Services;

internal sealed class SurrealCooldownRepository : ICooldownPersistence
{
    private const string TableMapCooldown = "mcs_map_cooldown";
    private const string TableGroupCooldown = "mcs_group_cooldown";

    private readonly IWulingSurreal _surreal;
    private readonly ILogger _logger;
    private readonly string _surqlDirectory;

    internal SurrealCooldownRepository(IWulingSurreal surreal, ILogger logger, string moduleDirectory)
    {
        _surreal = surreal;
        _logger = logger;
        _surqlDirectory = Path.Combine(moduleDirectory, "surql");
    }

    public async Task EnsureSchemasAsync(CancellationToken ct = default)
    {
        await _surreal.EnsureSchemasAsync(_surqlDirectory, ct);
        _logger.LogInformation("[CooldownPersistence] SurrealDB schemas ensured from {Path}", _surqlDirectory);
    }

    public async Task<IReadOnlyList<NamedCooldownRecord>> LoadAllMapCooldownsAsync(CancellationToken ct = default)
    {
        return await LoadAllAsync(TableMapCooldown, ct);
    }

    public async Task<IReadOnlyList<NamedCooldownRecord>> LoadAllGroupCooldownsAsync(CancellationToken ct = default)
    {
        return await LoadAllAsync(TableGroupCooldown, ct);
    }

    public async Task SaveMapCooldownAsync(string mapName, CooldownRecord record, CancellationToken ct = default)
    {
        await UpsertAsync(TableMapCooldown, mapName, record, ct);
    }

    public async Task SaveGroupCooldownAsync(string groupName, CooldownRecord record, CancellationToken ct = default)
    {
        await UpsertAsync(TableGroupCooldown, groupName, record, ct);
    }

    public void SaveMapCooldownFireAndForget(string mapName, CooldownRecord record)
    {
        _ = Task.Run(async () =>
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
        _ = Task.Run(async () =>
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
        _ = Task.Run(async () =>
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

    private async Task UpsertAsync(string table, string name, CooldownRecord record, CancellationToken ct = default)
    {
        var surql = $"UPSERT type::record('{table}', $name) SET name = $name, cooldown = $cooldown, timed_cooldown_end = $timed_cooldown_end, last_played_at = $last_played_at, nom_cooldown = $nom_cooldown, nom_timed_cooldown_end = $nom_timed_cooldown_end, last_nominated_at = $last_nominated_at;";
        var vars = new Dictionary<string, object?>
        {
            ["name"] = name,
            ["cooldown"] = record.Cooldown,
            ["timed_cooldown_end"] = record.TimedCooldownEnd,
            ["last_played_at"] = record.LastPlayedAt,
            ["nom_cooldown"] = record.NomCooldown,
            ["nom_timed_cooldown_end"] = record.NomTimedCooldownEnd,
            ["last_nominated_at"] = record.LastNominatedAt,
        };

        await _surreal.WriteAsync(surql, vars, ct);
    }

    private async Task<IReadOnlyList<NamedCooldownRecord>> LoadAllAsync(string table, CancellationToken ct)
    {
        var surql = $"SELECT * FROM {table};";
        var dtos = await _surreal.QueryAsync<SurrealCooldownDto>(surql, ct: ct);

        var results = new List<NamedCooldownRecord>(dtos.Count);
        foreach (var dto in dtos)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.name))
                    continue;

                var record = new CooldownRecord(
                    Cooldown: dto.cooldown,
                    TimedCooldownEnd: dto.timed_cooldown_end,
                    LastPlayedAt: dto.last_played_at,
                    NomCooldown: dto.nom_cooldown,
                    NomTimedCooldownEnd: dto.nom_timed_cooldown_end,
                    LastNominatedAt: dto.last_nominated_at);

                results.Add(new NamedCooldownRecord(dto.name, record));
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
        public string? name { get; set; }
        public int cooldown { get; set; }
        public DateTime timed_cooldown_end { get; set; }
        public DateTime last_played_at { get; set; }
        public int nom_cooldown { get; set; }
        public DateTime nom_timed_cooldown_end { get; set; }
        public DateTime last_nominated_at { get; set; }
    }
}
