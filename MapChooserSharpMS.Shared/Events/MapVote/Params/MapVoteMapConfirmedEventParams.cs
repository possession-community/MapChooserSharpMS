using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Shared.Events.MapVote.Params;

/// <summary>
/// Fired when next map is confirmed by vote.
/// </summary>
public interface IMapVoteMapConfirmedEventParams : IEventBaseParams
{
    IMapConfig ConfirmedMap { get; }

    /// <summary>
    /// True when this map confirmation was triggered by an RTV vote.
    /// </summary>
    bool IsRtvVote { get; }
}