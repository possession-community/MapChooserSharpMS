using System;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapConfig.Models;

internal sealed class CooldownConfig : ICooldownConfig
{
    public int ConfigCooldown { get; }
    public TimeSpan TimedCooldown { get; }
    public int CurrentCooldown { get; set; }
    public DateTime LastPlayedAt { get; set; }

    /// <summary>
    /// Authoritative UTC time until which the timed cooldown is active.
    /// Set on cooldown apply (<c>UtcNow + TimedCooldown</c>) or by admin
    /// override (<c>UtcNow + custom duration</c>).
    /// <c>DateTime.MinValue</c> means no timed cooldown.
    /// </summary>
    internal DateTime TimedCooldownEndUtc { get; set; } = DateTime.MinValue;

    public CooldownConfig(int configCooldown, TimeSpan timedCooldown)
    {
        ConfigCooldown = configCooldown;
        TimedCooldown = timedCooldown;
    }
}
