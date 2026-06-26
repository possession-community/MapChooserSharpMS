using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wuling.Abstract.Tianshi.Surreal;

namespace MapChooserSharpMS.Modules.Nomination.Services;

internal sealed class PlayerNominationCooldownService
{
    private const string Table = "mcs_user_nom_cooldown";

    private readonly ILogger _logger;
    private readonly string _serverId;
    private readonly IWulingSurreal _surreal;
    private readonly Dictionary<ulong, PlayerNomCooldownState> _states = new();

    internal PlayerNominationCooldownService(ILogger logger, string serverId, IWulingSurreal surreal)
    {
        _logger = logger;
        _serverId = serverId;
        _surreal = surreal;
    }

    internal bool IsInCooldown(ulong steamId)
    {
        if (!_states.TryGetValue(steamId, out var state))
            return false;

        bool hasCount = state.RemainingCount > 0;
        bool hasTimed = state.CooldownUntil > DateTime.UtcNow;

        if (!hasCount && !hasTimed)
        {
            _states.Remove(steamId);
            return false;
        }

        return true;
    }

    internal PlayerNomCooldownState? GetState(ulong steamId)
    {
        if (_states.TryGetValue(steamId, out var state))
            return state;
        return null;
    }

    internal void SetCooldown(ulong steamId, int count, float timedSeconds)
    {
        var until = timedSeconds > 0
            ? DateTime.UtcNow.AddSeconds(timedSeconds)
            : DateTime.MinValue;

        var state = new PlayerNomCooldownState(count, until);

        if (count <= 0 && until <= DateTime.UtcNow)
        {
            _states.Remove(steamId);
            RemoveFromDbFireAndForget(steamId);
            return;
        }

        _states[steamId] = state;
        SaveToDbFireAndForget(steamId, state);
    }

    internal void DecrementAll()
    {
        var toRemove = new List<ulong>();

        foreach (var (steamId, state) in _states)
        {
            int newCount = state.RemainingCount > 0 ? state.RemainingCount - 1 : 0;
            bool hasTimed = state.CooldownUntil > DateTime.UtcNow;

            if (newCount <= 0 && !hasTimed)
            {
                toRemove.Add(steamId);
                continue;
            }

            _states[steamId] = state with { RemainingCount = newCount };
        }

        foreach (var steamId in toRemove)
            _states.Remove(steamId);

        SaveAllToDbFireAndForget();
    }

    internal async Task LoadFromDbAsync()
    {
        try
        {
            var surql = $"SELECT * FROM {Table} WHERE server_id = $server_id;";
            var vars = new Dictionary<string, object?> { ["server_id"] = _serverId };
            var results = await _surreal.QueryAsync<NomCooldownDto>(surql, vars);

            int loaded = 0;
            foreach (var dto in results)
            {
                if (dto.steam_id <= 0)
                    continue;

                var steamId = (ulong)dto.steam_id;
                if (dto.remaining_count <= 0 && dto.cooldown_until <= DateTime.UtcNow)
                    continue;

                _states[steamId] = new PlayerNomCooldownState(dto.remaining_count, dto.cooldown_until);
                loaded++;
            }

            _logger.LogInformation("[PlayerNomCD] Loaded {Count} player cooldowns from DB", loaded);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PlayerNomCD] Failed to load player cooldowns from DB");
        }
    }

    internal sealed class NomCooldownDto
    {
        public long steam_id { get; set; }
        public int remaining_count { get; set; }
        public DateTime cooldown_until { get; set; }
        public string? server_id { get; set; }
    }

    private void SaveToDbFireAndForget(ulong steamId, PlayerNomCooldownState state)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var surql = $"UPSERT type::record('{Table}', $id) SET steam_id = $steam_id, remaining_count = $remaining_count, cooldown_until = $cooldown_until, server_id = $server_id;";
                var vars = new Dictionary<string, object?>
                {
                    ["id"] = $"{_serverId}_{steamId}",
                    ["steam_id"] = (long)steamId,
                    ["remaining_count"] = state.RemainingCount,
                    ["cooldown_until"] = state.CooldownUntil,
                    ["server_id"] = _serverId,
                };
                await _surreal.WriteAsync(surql, vars);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PlayerNomCD] Failed to save cooldown for {SteamId}", steamId);
            }
        });
    }

    private void RemoveFromDbFireAndForget(ulong steamId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var surql = $"DELETE type::record('{Table}', $id);";
                var vars = new Dictionary<string, object?> { ["id"] = $"{_serverId}_{steamId}" };
                await _surreal.WriteAsync(surql, vars);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PlayerNomCD] Failed to remove cooldown for {SteamId}", steamId);
            }
        });
    }

    private void SaveAllToDbFireAndForget()
    {
        var snapshot = new Dictionary<ulong, PlayerNomCooldownState>(_states);

        _ = Task.Run(async () =>
        {
            try
            {
                foreach (var (steamId, state) in snapshot)
                {
                    var surql = $"UPSERT type::record('{Table}', $id) SET steam_id = $steam_id, remaining_count = $remaining_count, cooldown_until = $cooldown_until, server_id = $server_id;";
                    var vars = new Dictionary<string, object?>
                    {
                        ["id"] = $"{_serverId}_{steamId}",
                        ["steam_id"] = (long)steamId,
                        ["remaining_count"] = state.RemainingCount,
                        ["cooldown_until"] = state.CooldownUntil,
                        ["server_id"] = _serverId,
                    };
                    await _surreal.WriteAsync(surql, vars);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PlayerNomCD] Failed to bulk save cooldowns");
            }
        });
    }
}

internal sealed record PlayerNomCooldownState(int RemainingCount, DateTime CooldownUntil)
    : MapChooserSharpMS.Shared.Nomination.IPlayerNominationCooldownState;

