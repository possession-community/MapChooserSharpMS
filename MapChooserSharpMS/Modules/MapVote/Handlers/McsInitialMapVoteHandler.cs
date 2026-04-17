using Microsoft.Extensions.Logging;
using NativeVoteManagerMS.Shared.Types;

namespace MapChooserSharpMS.Modules.MapVote.Handlers;

internal sealed class McsInitialMapVoteHandler(ILogger logger) : McsMapVoteHandlerBase(logger)
{
    protected override string VoteKind => "InitialVote";

    public override void OnVotePassed(VoteResult result)
    {
        // Winner crossed the WinnerPickupThreshold — confirm as next map
        // (or handle Extend/DontChange placeholder via IsNonMapPlaceholder).
        // TODO: fire McsNextMapConfirmed / Extend / MapNotChanged events.
        Logger.LogInformation(
            "[{Kind}] Passed: winner={Winner}, totalVotes={Total}",
            VoteKind, result.Winner?.InternalName ?? "<none>", GetTotalVotes(result));
    }

    public override void OnVoteFailed(VoteResult result)
    {
        // No choice reached the WinnerPickupThreshold — escalate to runoff
        // using the top contenders above RunoffMapPickupThreshold.
        // TODO: collect candidates and call back into the controller to
        //       initiate a runoff vote with McsRunoffMapVoteHandler.
        Logger.LogInformation(
            "[{Kind}] Failed: no decisive winner, runoff should start",
            VoteKind);
    }
}
