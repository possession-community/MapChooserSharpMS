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

    internal McsMapCooldownCommandService(ILogger logger)
    {
        _logger = logger;
    }

    public Task<bool> SetCooldown(IMapConfig mapConfig, int cooldown)
    {
        if (mapConfig.CooldownConfig is not CooldownConfig cc)
            return Task.FromResult(false);

        cc.CurrentCooldown = cooldown;

        _logger.LogInformation("[Cooldown] SetCooldown: {Map} = {Count}", mapConfig.MapName, cooldown);
        return Task.FromResult(true);
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
