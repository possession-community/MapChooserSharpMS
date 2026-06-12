using System;

namespace MapChooserSharpMS.Shared.MapConfig;

/// <summary>
/// Map's cooldown
/// </summary>
public interface ICooldownConfig
{
    /// <summary>
    /// Play-based cooldown count specified in map config.
    /// </summary>
    int ConfigCooldown { get; }

    /// <summary>
    /// Time-based cooldown duration specified in map config.
    /// </summary>
    TimeSpan TimedCooldown { get; }

    /// <summary>
    /// Current play-based cooldown in memory.
    /// </summary>
    int CurrentCooldown { get; }

    /// <summary>
    /// When this map was last played (UTC).
    /// </summary>
    DateTime LastPlayedAt { get; }

    /// <summary>
    /// Nomination cooldown count specified in map config.
    /// 0 = disabled (opt-in).
    /// </summary>
    int ConfigNominationCooldown { get; }

    /// <summary>
    /// Time-based nomination cooldown duration specified in map config.
    /// </summary>
    TimeSpan NominationTimedCooldown { get; }
}