using MapChooserSharpMS.Shared.MapConfig;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Events.Nomination.Params;

/// <summary>
/// Fired after all built-in nomination checks have passed for a specific map.
/// </summary>
public interface INominationCheckPassedEventParams : IEventBaseParams
{
    /// <summary>
    /// Client who activated this event. if it is console, then param is null
    /// </summary>
    IGameClient? Client { get; }

    /// <summary>
    /// The map that passed all nomination checks.
    /// </summary>
    IMapConfig MapConfig { get; }
}