using System;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapConfig.Models;

internal sealed class CooldownConfig : ICooldownConfig
{
    public int ConfigCooldown { get; }
    public TimeSpan TimedCooldown { get; }
    public int CurrentCooldown { get; set; }
    public DateTime LastPlayedAt { get; set; }
    internal DateTime TimedCooldownEndUtc { get; set; } = DateTime.MinValue;

    public int ConfigNominationCooldown { get; }
    public TimeSpan NominationTimedCooldown { get; }
    internal int CurrentNominationCooldown { get; set; }
    internal DateTime NominationTimedCooldownEndUtc { get; set; } = DateTime.MinValue;

    public int UnplayedCount { get; internal set; }

    internal bool CooldownAuditRecorded { get; set; } = true;

    internal bool HasAnyCooldownConfigured => ConfigCooldown > 0 || TimedCooldown > TimeSpan.Zero;

    internal bool IsFullyAvailable => CurrentCooldown == 0 && TimedCooldownEndUtc <= DateTime.UtcNow;

    public CooldownConfig(
        int configCooldown,
        TimeSpan timedCooldown,
        int configNominationCooldown = 0,
        TimeSpan nominationTimedCooldown = default)
    {
        ConfigCooldown = configCooldown;
        TimedCooldown = timedCooldown;
        ConfigNominationCooldown = configNominationCooldown;
        NominationTimedCooldown = nominationTimedCooldown;
    }
}
