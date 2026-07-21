using System;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.MapCycle.Cooldown;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Services;
using Microsoft.Extensions.Logging;

namespace MapChooserSharpMS.Modules.MapCycle.Services;

internal sealed class McsMapCooldownCommandService : IMapCooldownCommandService
{
    private readonly ILogger _logger;
    private readonly IMcsMapConfigProvider _mapConfigProvider;
    private readonly IMcsInternalCooldownStore _store;
    private ICooldownPersistence _persistence = NullCooldownPersistence.Instance;

    internal McsMapCooldownCommandService(ILogger logger, IMcsMapConfigProvider mapConfigProvider, IMcsInternalCooldownStore store)
    {
        _logger = logger;
        _mapConfigProvider = mapConfigProvider;
        _store = store;
    }

    internal void SetPersistence(ICooldownPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<bool> SetCooldown(IMapConfig mapConfig, int cooldown)
    {
        var entry = _store.GetOrCreateRawMapEntry(mapConfig.MapName);
        entry.CurrentCooldown = cooldown;

        await SaveMapWriteThrough(mapConfig.MapName, entry, nameof(SetCooldown));

        _logger.LogInformation("[Cooldown] SetCooldown: {Map} = {Count}", mapConfig.MapName, cooldown);
        return true;
    }

    public async Task<bool> SetGroupCooldown(string groupName, int cooldown)
    {
        if (!GroupExists(groupName))
            return false;

        var entry = _store.GetOrCreateRawGroupEntry(groupName);
        entry.CurrentCooldown = cooldown;

        await SaveGroupWriteThrough(groupName, entry, nameof(SetGroupCooldown));

        _logger.LogInformation("[Cooldown] SetGroupCooldown: {Group} = {Count}", groupName, cooldown);
        return true;
    }

    public async Task<bool> SetTimedCooldown(IMapConfig mapConfig, TimeSpan cooldown)
    {
        var entry = _store.GetOrCreateRawMapEntry(mapConfig.MapName);
        entry.TimedCooldownEndUtc = DateTime.UtcNow + cooldown;

        await SaveMapWriteThrough(mapConfig.MapName, entry, nameof(SetTimedCooldown));

        _logger.LogInformation("[Cooldown] SetTimedCooldown: {Map} until {Until}", mapConfig.MapName, entry.TimedCooldownEndUtc);
        return true;
    }

    public async Task<bool> SetGroupTimedCooldown(string groupName, TimeSpan cooldown)
    {
        if (!GroupExists(groupName))
            return false;

        var entry = _store.GetOrCreateRawGroupEntry(groupName);
        entry.TimedCooldownEndUtc = DateTime.UtcNow + cooldown;

        await SaveGroupWriteThrough(groupName, entry, nameof(SetGroupTimedCooldown));

        _logger.LogInformation("[Cooldown] SetGroupTimedCooldown: {Group} until {Until}", groupName, entry.TimedCooldownEndUtc);
        return true;
    }

    public Task<bool> ClearGroupCooldown(string groupName)
    {
        return SetGroupCooldown(groupName, 0);
    }

    public Task<bool> ClearGroupTimedCooldown(string groupName)
    {
        return SetGroupTimedCooldown(groupName, TimeSpan.Zero);
    }

    public Task<bool> ExcludeFromNomination(IMapConfig mapConfig)
    {
        return SetCooldown(mapConfig, int.MaxValue);
    }

    public Task<bool> ClearCooldown(IMapConfig mapConfig)
    {
        return SetCooldown(mapConfig, 0);
    }

    public async Task<bool> ClearTimedCooldown(IMapConfig mapConfig)
    {
        var entry = _store.GetOrCreateRawMapEntry(mapConfig.MapName);
        entry.TimedCooldownEndUtc = DateTime.MinValue;

        await SaveMapWriteThrough(mapConfig.MapName, entry, nameof(ClearTimedCooldown));

        _logger.LogInformation("[Cooldown] ClearTimedCooldown: {Map}", mapConfig.MapName);
        return true;
    }

    private bool GroupExists(string groupName)
    {
        return _mapConfigProvider.GetGroupSettings().TryGetValue(groupName, out var variants)
               && variants.Count > 0;
    }

    private async Task SaveMapWriteThrough(string mapName, McsCooldownStateEntry entry, string operation)
    {
        try
        {
            await _persistence.SaveMapCooldownAsync(mapName, BuildRecord(entry));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cooldown] Write-through failed for {Operation}: {Map}", operation, mapName);
        }
    }

    private async Task SaveGroupWriteThrough(string groupName, McsCooldownStateEntry entry, string operation)
    {
        try
        {
            await _persistence.SaveGroupCooldownAsync(groupName, BuildRecord(entry));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cooldown] Write-through failed for {Operation}: {Group}", operation, groupName);
        }
    }

    private static CooldownRecord BuildRecord(McsCooldownStateEntry entry)
    {
        return new CooldownRecord(
            Cooldown: entry.CurrentCooldown,
            TimedCooldownEnd: entry.TimedCooldownEndUtc,
            LastPlayedAt: entry.LastPlayedAt,
            UnplayedCount: entry.UnplayedCount,
            NomCooldown: entry.CurrentNominationCooldown,
            NomTimedCooldownEnd: entry.NominationTimedCooldownEndUtc);
    }
}
