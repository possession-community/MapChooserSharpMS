using System;
using MapChooserSharpMS.Shared.Events.MapVote;

namespace MapChooserSharpMS.Shared.MapVote;

/// <summary>
/// Public MapVoteController API. Kept deliberately thin: read-only vote
/// state lives on <see cref="IMcsMainVoteState"/> (and
/// <see cref="IMcsExtendVoteState"/> for the extend vote) so consumers that
/// only query state depend on those narrower interfaces. Writable internals
/// (managers, controlling service, client-vote handling service) live on
/// the internal controller and are not exposed here.
/// </summary>
public interface IMcsMapVoteController
{
    void InstallEventListener(IMapVoteEventListener listener);

    void RemoveEventListener(IMapVoteEventListener listener);

    /// <summary>
    /// External plugins can provide a custom winner threshold for initial votes.
    /// The func is invoked each time a vote starts; the returned float is used as
    /// the pass threshold (0.0–1.0). Set to <c>null</c> to revert to the default
    /// ConVar-based threshold. Runoff votes always pass regardless of this value.
    /// </summary>
    Func<float>? CustomWinnerThresholdProvider { get; set; }
}