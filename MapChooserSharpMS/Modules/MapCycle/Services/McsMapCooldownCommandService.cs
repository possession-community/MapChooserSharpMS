using System;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.MapConfig.Models;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Services;
using Microsoft.Extensions.Logging;

namespace MapChooserSharpMS.Modules.MapCycle.Services;

internal sealed class McsMapCooldownCommandService : IMapCooldownCommandService
{
    private readonly ILogger _logger;
    private readonly IMcsMapConfigProvider _mapConfigProvider;
    private ICooldownPersistence _persistence = NullCooldownPersistence.Instance;

    internal McsMapCooldownCommandService(ILogger logger, IMcsMapConfigProvider mapConfigProvider)
    {
        _logger = logger;
        _mapConfigProvider = mapConfigProvider;
    }

    internal void SetPersistence(ICooldownPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<bool> SetCooldown(IMapConfig mapConfig, int cooldown)
    {
        if (mapConfig.CooldownConfig is not CooldownConfig cc)
            return false;

        cc.CurrentCooldown = cooldown;

        try
        {
            await _persistence.SaveMapCooldownAsync(mapConfig.MapName, BuildMapRecord(cc));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cooldown] Write-through failed for SetCooldown: {Map}", mapConfig.MapName);
        }

        _logger.LogInformation("[Cooldown] SetCooldown: {Map} = {Count}", mapConfig.MapName, cooldown);
        return true;
    }

    internal async Task<bool> SetGroupCooldown(string groupName, int cooldown)
    {
        if (!_mapConfigProvider.GetGroupSettings().TryGetValue(groupName, out var groupVariants)
            || groupVariants.Count == 0)
        {
            return false;
        }

        bool anySet = false;
        CooldownConfig? lastCc = null;

        foreach (var variant in groupVariants)
        {
            if (variant.GroupConfig.CooldownConfig is not CooldownConfig cc)
                continue;

            cc.CurrentCooldown = cooldown;
            anySet = true;
            lastCc = cc;
        }

        if (anySet && lastCc is not null)
        {
            try
            {
                await _persistence.SaveGroupCooldownAsync(groupName, BuildGroupRecord(lastCc));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Cooldown] Write-through failed for SetGroupCooldown: {Group}", groupName);
            }

            _logger.LogInformation("[Cooldown] SetGroupCooldown: {Group} = {Count}", groupName, cooldown);
        }

        return anySet;
    }

    public async Task<bool> SetTimedCooldown(IMapConfig mapConfig, TimeSpan cooldown)
    {
        if (mapConfig.CooldownConfig is not CooldownConfig cc)
            return false;

        cc.TimedCooldownEndUtc = DateTime.UtcNow + cooldown;

        try
        {
            await _persistence.SaveMapCooldownAsync(mapConfig.MapName, BuildMapRecord(cc));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cooldown] Write-through failed for SetTimedCooldown: {Map}", mapConfig.MapName);
        }

        _logger.LogInformation("[Cooldown] SetTimedCooldown: {Map} until {Until}", mapConfig.MapName, cc.TimedCooldownEndUtc);
        return true;
    }

    internal async Task<bool> SetGroupTimedCooldown(string groupName, TimeSpan cooldown)
    {
        if (!_mapConfigProvider.GetGroupSettings().TryGetValue(groupName, out var groupVariants)
            || groupVariants.Count == 0)
        {
            return false;
        }

        bool anySet = false;
        var endUtc = DateTime.UtcNow + cooldown;
        CooldownConfig? lastCc = null;

        foreach (var variant in groupVariants)
        {
            if (variant.GroupConfig.CooldownConfig is not CooldownConfig cc)
                continue;

            cc.TimedCooldownEndUtc = endUtc;
            anySet = true;
            lastCc = cc;
        }

        if (anySet && lastCc is not null)
        {
            try
            {
                await _persistence.SaveGroupCooldownAsync(groupName, BuildGroupRecord(lastCc));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Cooldown] Write-through failed for SetGroupTimedCooldown: {Group}", groupName);
            }

            _logger.LogInformation("[Cooldown] SetGroupTimedCooldown: {Group} until {Until}", groupName, endUtc);
        }

        return anySet;
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
        if (mapConfig.CooldownConfig is not CooldownConfig cc)
            return false;

        cc.TimedCooldownEndUtc = DateTime.MinValue;

        try
        {
            await _persistence.SaveMapCooldownAsync(mapConfig.MapName, BuildMapRecord(cc));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cooldown] Write-through failed for ClearTimedCooldown: {Map}", mapConfig.MapName);
        }

        _logger.LogInformation("[Cooldown] ClearTimedCooldown: {Map}", mapConfig.MapName);
        return true;
    }

    private static CooldownRecord BuildMapRecord(CooldownConfig cc)
    {
        return new CooldownRecord(
            Cooldown: cc.CurrentCooldown,
            TimedCooldownEnd: cc.TimedCooldownEndUtc,
            LastPlayedAt: cc.LastPlayedAt,
            NomCooldown: cc.CurrentNominationCooldown,
            NomTimedCooldownEnd: cc.NominationTimedCooldownEndUtc,
            LastNominatedAt: DateTime.MinValue);
    }

    private static CooldownRecord BuildGroupRecord(CooldownConfig cc)
    {
        return new CooldownRecord(
            Cooldown: cc.CurrentCooldown,
            TimedCooldownEnd: cc.TimedCooldownEndUtc,
            LastPlayedAt: cc.LastPlayedAt,
            NomCooldown: cc.CurrentNominationCooldown,
            NomTimedCooldownEnd: cc.NominationTimedCooldownEndUtc,
            LastNominatedAt: DateTime.MinValue);
    }
}
