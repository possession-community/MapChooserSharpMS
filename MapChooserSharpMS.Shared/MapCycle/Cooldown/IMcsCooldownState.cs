using System;

namespace MapChooserSharpMS.Shared.MapCycle.Cooldown;

/// <summary>
/// Read-only view of a map's or group's runtime cooldown state.
/// Obtain instances from <see cref="IMcsCooldownStore"/>.
/// </summary>
public interface IMcsCooldownState
{
    /// <summary>
    /// Current play-based cooldown. The map cannot be picked or nominated while above 0.
    /// <see cref="int.MaxValue"/> means excluded from nomination until cleared by an admin.
    /// </summary>
    int CurrentCooldown { get; }

    /// <summary>
    /// UTC time until which the time-based cooldown is active.
    /// <see cref="DateTime.MinValue"/> when no timed cooldown is set.
    /// </summary>
    DateTime TimedCooldownEndUtc { get; }

    /// <summary>
    /// When this map was last played (UTC). <see cref="DateTime.MinValue"/> if never.
    /// </summary>
    DateTime LastPlayedAt { get; }

    /// <summary>
    /// Number of maps played since this map's cooldown fully expired
    /// (both count and timed). Reset to 0 when cooldown is applied.
    /// </summary>
    int UnplayedCount { get; }

    /// <summary>
    /// Current nomination cooldown count.
    /// </summary>
    int CurrentNominationCooldown { get; }

    /// <summary>
    /// UTC time until which the time-based nomination cooldown is active.
    /// </summary>
    DateTime NominationTimedCooldownEndUtc { get; }

    /// <summary>
    /// True while either cooldown axis (count or timed) is active.
    /// </summary>
    bool IsCooldownActive { get; }

    /// <summary>
    /// True while either nomination cooldown axis (count or timed) is active.
    /// </summary>
    bool IsNominationCooldownActive { get; }
}
