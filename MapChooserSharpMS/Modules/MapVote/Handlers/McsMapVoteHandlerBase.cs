using Microsoft.Extensions.Logging;
using NativeVoteManagerMS.Shared;
using NativeVoteManagerMS.Shared.Types;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Modules.MapVote.Handlers;

internal abstract class McsMapVoteHandlerBase(ILogger logger) : IMultiChoiceVoteHandler
{
    protected const string ExtendMapInternalName = "MapChooserSharp:ExtendMap";
    protected const string DontChangeMapInternalName = "MapChooserSharp:DontChangeMap";

    protected readonly ILogger Logger = logger;

    protected abstract string VoteKind { get; }

    public virtual void OnVoteInitiated()
    {
        Logger.LogDebug("[{Kind}] Vote initiated", VoteKind);
    }

    public virtual void OnChoice(IGameClient chooser, VoteContent content, MultiChoiceVoteState state)
    {
        Logger.LogDebug(
            "[{Kind}] {Player} voted {Choice} ({Voted}/{Participant})",
            VoteKind, chooser.Name, content.InternalName, state.VotedCount, state.ParticipantCount);
    }

    public virtual void OnVoteCancelled()
    {
        Logger.LogInformation("[{Kind}] Vote cancelled", VoteKind);
    }

    public abstract void OnVotePassed(VoteResult result);
    public abstract void OnVoteFailed(VoteResult result);

    protected static bool IsExtendMap(VoteContent content)
        => content.InternalName == ExtendMapInternalName;

    protected static bool IsDontChangeMap(VoteContent content)
        => content.InternalName == DontChangeMapInternalName;

    protected static bool IsNonMapPlaceholder(VoteContent content)
        => IsExtendMap(content) || IsDontChangeMap(content);

    protected static VoteChoiceResult? GetTopChoice(VoteResult result)
    {
        VoteChoiceResult? top = null;
        foreach (var choice in result.Choices)
        {
            if (top is null || choice.Voters.Count > top.Voters.Count)
                top = choice;
        }
        return top;
    }

    protected static int GetTotalVotes(VoteResult result)
    {
        int total = 0;
        foreach (var choice in result.Choices)
            total += choice.Voters.Count;
        return total;
    }
}
