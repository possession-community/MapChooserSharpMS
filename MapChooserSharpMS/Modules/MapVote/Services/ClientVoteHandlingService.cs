using MapChooserSharpMS.Modules.MapVote.Managers;
using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.MapVote.Services;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace MapChooserSharpMS.Modules.MapVote.Services;

internal sealed class ClientVoteHandlingService(VoteControllingManager voteManager) : IClientVoteHandlingService
{
    public bool TryAddClientVote(IGameClient client, IMapVoteOption option)
    {
        var session = voteManager.CurrentSession;
        if (session is null)
            return false;

        if (session.CurrentState is not (McsMapVoteState.Voting or McsMapVoteState.RunoffVoting))
            return false;

        return session.AddVote(client.Slot, option);
    }

    public void RemoveClientVote(IGameClient client)
        => RemoveClientVote(client.Slot);

    public void RemoveClientVote(PlayerSlot slot)
    {
        voteManager.CurrentSession?.RemoveVote(slot);
    }

    public void ClientReVote(IGameClient client)
    {
        RemoveClientVote(client);
        // Menu-based re-vote is handled by the UI layer; NativeVote does not support re-vote.
    }
}
