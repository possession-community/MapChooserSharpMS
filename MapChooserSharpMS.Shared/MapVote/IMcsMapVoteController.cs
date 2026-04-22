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
    /// <summary>
    /// Installs event listener
    /// </summary>
    void InstallEventListener(IMapVoteEventListener listener);

    /// <summary>
    /// Remove event listener
    /// </summary>
    void RemoveEventListener(IMapVoteEventListener listener);
}