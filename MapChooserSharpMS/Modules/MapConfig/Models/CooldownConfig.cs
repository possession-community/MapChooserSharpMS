using System;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapConfig.Models;

internal sealed class CooldownConfig : ICooldownConfig
{
    public int ConfigCooldown { get; }
    public TimeSpan TimedCooldown { get; }
    public int CurrentCooldown { get; set; }
    public DateTime LastPlayedAt { get; set; }

    public CooldownConfig(int configCooldown, TimeSpan timedCooldown)
    {
        ConfigCooldown = configCooldown;
        TimedCooldown = timedCooldown;
    }
}
