using MapChooserSharpMS.Shared.MapConfig;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Events.MapCycle.Params;

/// <summary>
/// Fired when an extend vote was cancelled
/// </summary>
public interface IExtendVoteCancelledEventParams: IEventBaseParams
{
    /// <summary>
    /// The map that was being voted to extend (current map).
    /// null when MCS has no config for the current map.
    /// </summary>
    IMapConfig? CurrentMap { get; }

    /// <summary>
    /// Who cancelled the extend vote. null means console/server
    /// or an external cancellation.
    /// </summary>
    IGameClient? CancelledBy { get; }
}
