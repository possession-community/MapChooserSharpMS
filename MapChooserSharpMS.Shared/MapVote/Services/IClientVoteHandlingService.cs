using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace MapChooserSharpMS.Shared.MapVote.Services;

public interface IClientVoteHandlingService
{
    /// <summary>
    /// Tries to add vote.
    /// </summary>
    bool TryAddClientVote(IGameClient client, IMapVoteOption option);

    /// <summary>
    /// Remove client's vote from the current vote.
    /// </summary>
    void RemoveClientVote(IGameClient client);

    /// <summary>
    /// Remove client's vote from the current vote by slot.
    /// Use when <see cref="IGameClient"/> is unavailable (e.g. disconnect).
    /// </summary>
    void RemoveClientVote(PlayerSlot slot);

    /// <summary>
    /// Removes client's vote and show vote menu to client.<br/>
    /// Not available for Native vote UI and silently ignored.
    /// </summary>
    void ClientReVote(IGameClient client);
}