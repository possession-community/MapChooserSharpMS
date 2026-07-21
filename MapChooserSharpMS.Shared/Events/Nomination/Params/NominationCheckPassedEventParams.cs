using MapChooserSharpMS.Shared.MapConfig;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Events.Nomination.Params;

/// <summary>
/// Fired after all built-in nomination checks have passed for a specific map.
/// </summary>
public interface INominationCheckPassedEventParams : IEventBaseParams, IEnforceableEvent
{
    /// <summary>
    /// Client who activated this event. null for console admin nomination and
    /// random pick. Check <see cref="IEnforceableEvent.EnforcedByAdmin"/> to
    /// distinguish the admin nomination path from a player nomination.
    /// </summary>
    IGameClient? Client { get; }

    /// <summary>
    /// The map that passed all nomination checks.
    /// </summary>
    IMapConfig MapConfig { get; }
}