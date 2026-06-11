using System.Linq;
using MapChooserSharpMS.Modules.MapVote.Managers;
using MapChooserSharpMS.Modules.MapVote.Models;
using MapChooserSharpMS.Modules.MapVote.Services;
using Microsoft.Extensions.Logging;
using NativeVoteManagerMS.Shared;
using NativeVoteManagerMS.Shared.Types;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Modules.MapVote.Handlers;

internal abstract class MapVoteNativeHandlerBase : IMultiChoiceVoteHandler
{
    protected readonly MapVoteControllingService Service;
    protected readonly VoteControllingManager VoteManager;
    protected readonly MapVoteInformation Session;
    protected readonly ILogger Logger;

    protected MapVoteNativeHandlerBase(
        MapVoteControllingService service,
        VoteControllingManager voteManager,
        MapVoteInformation session,
        ILogger logger)
    {
        Service = service;
        VoteManager = voteManager;
        Session = session;
        Logger = logger;
    }

    protected abstract string VoteKind { get; }

    protected abstract bool IsRunoff { get; }

    protected bool IsStaleSession() => !ReferenceEquals(VoteManager.CurrentSession, Session);

    public void OnVoteInitiated()
    {
        Logger.LogDebug("[{Kind}] Vote initiated", VoteKind);
    }

    public void OnChoice(IGameClient chooser, VoteContent content, MultiChoiceVoteState state)
    {
        if (IsStaleSession()) return;

        var option = Session.VoteOptions.FirstOrDefault(o => o.MapName == content.InternalName);
        if (option is not null)
            Session.AddVote(chooser.Slot, option);

        Logger.LogDebug(
            "[{Kind}] {Player} voted {Choice} ({Voted}/{Total})",
            VoteKind, chooser.Name, content.InternalName,
            state.VotedCount, state.ParticipantCount);
    }

    public void OnVoteCancelled()
    {
        Logger.LogInformation("[{Kind}] Vote cancelled by NativeVoteManager", VoteKind);

        if (!IsStaleSession())
            Service.HandleExternalCancel(Session);
    }

    public void OnVotePassed(VoteResult result)
    {
        if (IsStaleSession()) return;

        Logger.LogInformation("[{Kind}] Passed: winner={Winner}",
            VoteKind, result.Winner?.InternalName ?? "<none>");
        Service.HandleVoteResult(Session, IsRunoff);
    }

    public void OnVoteFailed(VoteResult result)
    {
        if (IsStaleSession()) return;

        Logger.LogInformation("[{Kind}] Failed: no decisive winner", VoteKind);
        Service.HandleVoteResult(Session, IsRunoff);
    }

    public void OnParticipantDisconnected(IGameClient client, MultiChoiceVoteState state)
    {
        if (IsStaleSession()) return;

        Session.RemoveVote(client.Slot);
    }
}
