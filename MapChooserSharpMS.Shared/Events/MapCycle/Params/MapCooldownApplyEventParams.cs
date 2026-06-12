using System;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Shared.Events.MapCycle.Params;

/// <summary>
/// Fired when MCS is about to apply map cooldown after a map is played.
/// Listeners can modify <see cref="Cooldown"/>, <see cref="TimedCooldownDuration"/>,
/// or set <see cref="IsCancelled"/> to prevent cooldown application.
/// </summary>
public interface IMapCooldownApplyEventParams : IEventBaseParams
{
    /// <summary>
    /// Map cooldown application target.
    /// </summary>
    IMapConfig AppliesTo { get; }

    /// <summary>
    /// Play-count cooldown to apply. Listeners may modify this value.
    /// Initialized to <see cref="IBaseMapConfig.CooldownConfig"/>.<c>ConfigCooldown</c>.
    /// </summary>
    int Cooldown { get; set; }

    /// <summary>
    /// Timed cooldown duration to apply. Listeners may modify this value.
    /// Initialized to <see cref="IBaseMapConfig.CooldownConfig"/>.<c>TimedCooldown</c>.
    /// </summary>
    TimeSpan TimedCooldownDuration { get; set; }

    /// <summary>
    /// When set to true, cooldown application is skipped entirely for this map.
    /// </summary>
    bool IsCancelled { get; set; }
}