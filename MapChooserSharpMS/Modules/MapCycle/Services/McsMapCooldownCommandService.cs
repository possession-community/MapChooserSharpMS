using System;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.MapConfig.Models;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Services;
using Microsoft.Extensions.Logging;

namespace MapChooserSharpMS.Modules.MapCycle.Services;

internal sealed class McsMapCooldownCommandService : IMapCooldownCommandService
{
    private readonly ILogger _logger;
    private readonly IMcsMapConfigProvider _mapConfigProvider;

    internal McsMapCooldownCommandService(ILogger logger, IMcsMapConfigProvider mapConfigProvider)
    {
        _logger = logger;
        _mapConfigProvider = mapConfigProvider;
    }

    public Task<bool> SetCooldown(IMapConfig mapConfig, int cooldown)
    {
        if (mapConfig.CooldownConfig is not CooldownConfig cc)
            return Task.FromResult(false);

        cc.CurrentCooldown = cooldown;

        _logger.LogInformation("[Cooldown] SetCooldown: {Map} = {Count}", mapConfig.MapName, cooldown);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Sets the current cooldown for a group by name, on every override
    /// variant of that group. Maps reference the parsed group config
    /// instances directly, so this propagates to all maps in the group.
    /// Internal-only until cooldown persistence lands (group cooldowns are
    /// not on the public surface yet); name-keyed so the future DB layer can
    /// persist by group name.
    /// </summary>
    /// <returns>False when the group is unknown or nothing could be set.</returns>
    internal bool SetGroupCooldown(string groupName, int cooldown)
    {
        if (!_mapConfigProvider.GetGroupSettings().TryGetValue(groupName, out var groupVariants)
            || groupVariants.Count == 0)
        {
            return false;
        }

        bool anySet = false;

        foreach (var variant in groupVariants)
        {
            if (variant.GroupConfig.CooldownConfig is not CooldownConfig cc)
                continue;

            cc.CurrentCooldown = cooldown;
            anySet = true;
        }

        if (anySet)
            _logger.LogInformation("[Cooldown] SetGroupCooldown: {Group} = {Count}", groupName, cooldown);

        return anySet;
    }

    public Task<bool> SetTimedCooldown(IMapConfig mapConfig, TimeSpan cooldown)
    {
        if (mapConfig.CooldownConfig is not CooldownConfig cc)
            return Task.FromResult(false);

        cc.TimedCooldownEndUtc = DateTime.UtcNow + cooldown;

        _logger.LogInformation("[Cooldown] SetTimedCooldown: {Map} until {Until}", mapConfig.MapName, cc.TimedCooldownEndUtc);
        return Task.FromResult(true);
    }

    public Task<bool> ExcludeFromNomination(IMapConfig mapConfig)
    {
        return SetCooldown(mapConfig, int.MaxValue);
    }

    public Task<bool> ClearCooldown(IMapConfig mapConfig)
    {
        return SetCooldown(mapConfig, 0);
    }

    public Task<bool> ClearTimedCooldown(IMapConfig mapConfig)
    {
        if (mapConfig.CooldownConfig is not CooldownConfig cc)
            return Task.FromResult(false);

        cc.TimedCooldownEndUtc = DateTime.MinValue;

        _logger.LogInformation("[Cooldown] ClearTimedCooldown: {Map}", mapConfig.MapName);
        return Task.FromResult(true);
    }
}
