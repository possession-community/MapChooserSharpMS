using System;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapConfig.Models;

/// <summary>
/// Mutable cooldown runtime state, split out of <see cref="CooldownConfig"/> so it
/// can be shared across the same map/group's DaySettings variants instead of being
/// fragmented per-variant.
/// </summary>
internal sealed class CooldownRuntimeState
{
    public int CurrentCooldown { get; set; }
    public DateTime LastPlayedAt { get; set; }
    public DateTime TimedCooldownEndUtc { get; set; } = DateTime.MinValue;
    public int UnplayedCount { get; set; }
    public bool CooldownAuditRecorded { get; set; } = true;
    public int CurrentNominationCooldown { get; set; }
    public DateTime NominationTimedCooldownEndUtc { get; set; } = DateTime.MinValue;
}

internal sealed class CooldownConfig : ICooldownConfig
{
    public int ConfigCooldown { get; }
    public TimeSpan TimedCooldown { get; }

    public int CurrentCooldown
    {
        get => RuntimeState.CurrentCooldown;
        set => RuntimeState.CurrentCooldown = value;
    }

    public DateTime LastPlayedAt
    {
        get => RuntimeState.LastPlayedAt;
        set => RuntimeState.LastPlayedAt = value;
    }

    internal DateTime TimedCooldownEndUtc
    {
        get => RuntimeState.TimedCooldownEndUtc;
        set => RuntimeState.TimedCooldownEndUtc = value;
    }

    public int ConfigNominationCooldown { get; }
    public TimeSpan NominationTimedCooldown { get; }

    internal int CurrentNominationCooldown
    {
        get => RuntimeState.CurrentNominationCooldown;
        set => RuntimeState.CurrentNominationCooldown = value;
    }

    internal DateTime NominationTimedCooldownEndUtc
    {
        get => RuntimeState.NominationTimedCooldownEndUtc;
        set => RuntimeState.NominationTimedCooldownEndUtc = value;
    }

    public int UnplayedCount
    {
        get => RuntimeState.UnplayedCount;
        internal set => RuntimeState.UnplayedCount = value;
    }

    internal bool CooldownAuditRecorded
    {
        get => RuntimeState.CooldownAuditRecorded;
        set => RuntimeState.CooldownAuditRecorded = value;
    }

    internal CooldownRuntimeState RuntimeState { get; private set; }

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
        RuntimeState = new CooldownRuntimeState();
    }

    /// <summary>
    /// Replaces this variant's runtime state with a shared instance so cooldown
    /// writes/reads are consistent across DaySettings variants of the same map/group.
    /// </summary>
    internal void ShareRuntimeState(CooldownRuntimeState state)
    {
        RuntimeState = state;
    }
}
