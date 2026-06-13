using MapChooserSharpMS.Shared.MapConfig;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Events.MapCycle.Params;

/// <summary>
/// Fired when an extend vote has started
/// </summary>
public interface IExtendVoteStartedEventParams: IEventBaseParams
{
    /// <summary>
    /// The map being voted to extend (current map).
    /// null when MCS has no config for the current map.
    /// </summary>
    IMapConfig? CurrentMap { get; }

    /// <summary>
    /// Who initiated the extend vote. null means console/server.
    /// </summary>
    IGameClient? Initiator { get; }

    /// <summary>
    /// Vote duration in seconds.
    /// </summary>
    float VoteDuration { get; }
}
