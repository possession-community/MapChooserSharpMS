using Microsoft.Extensions.Logging;
using NativeVoteManagerMS.Shared.Types;

namespace MapChooserSharpMS.Modules.MapVote.Handlers;

internal sealed class McsRunoffMapVoteHandler(ILogger logger) : McsMapVoteHandlerBase(logger)
{
    protected override string VoteKind => "RunoffVote";

    public override void OnVotePassed(VoteResult result)
    {
        // Decisive runoff winner — confirm as next map (or handle placeholder).
        // TODO: fire McsNextMapConfirmed / Extend / MapNotChanged events.
        Logger.LogInformation(
            "[{Kind}] Passed: winner={Winner}, totalVotes={Total}",
            VoteKind, result.Winner?.InternalName ?? "<none>", GetTotalVotes(result));
    }

    public override void OnVoteFailed(VoteResult result)
    {
        // Runoff does not escalate further — fall back to the most-voted choice
        // (matches original behavior: picks top, random if zero votes).
        // TODO: confirm fallback choice as next map.
        var top = GetTopChoice(result);
        Logger.LogInformation(
            "[{Kind}] Failed: falling back to top={Top}",
            VoteKind, top?.Content.InternalName ?? "<none>");
    }
}
