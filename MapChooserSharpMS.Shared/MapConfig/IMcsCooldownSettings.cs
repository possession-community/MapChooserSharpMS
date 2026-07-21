using System;

namespace MapChooserSharpMS.Shared.MapConfig;

/// <summary>
/// Cooldown settings declared in map/group config.
/// Contains configuration values only — runtime cooldown state lives in
/// <see cref="MapChooserSharpMS.Shared.MapCycle.Cooldown.IMcsCooldownStore"/>.
/// </summary>
public interface IMcsCooldownSettings
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
    /// Nomination cooldown count specified in map config.
    /// 0 = disabled (opt-in).
    /// </summary>
    int ConfigNominationCooldown { get; }

    /// <summary>
    /// Time-based nomination cooldown duration specified in map config.
    /// </summary>
    TimeSpan NominationTimedCooldown { get; }
}
