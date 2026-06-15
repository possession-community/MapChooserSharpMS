using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Managers.MapTransition;

namespace MapChooserSharpMS.Shared.Events.MapVote.Params;

/// <summary>
/// Fired when next map is confirmed by vote.
/// </summary>
public interface IMapVoteMapConfirmedEventParams : IEventBaseParams
{
    IMapConfig ConfirmedMap { get; }

    /// <summary>
    /// Map information including nominator SteamIDs resolved from the nomination data.
    /// </summary>
    IMapInformation MapInformation { get; }

    /// <summary>
    /// True when this map confirmation was triggered by an RTV vote.
    /// </summary>
    bool IsRtvVote { get; }
}