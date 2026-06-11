using MapChooserSharpMS.Modules.MapVote.Managers;
using MapChooserSharpMS.Modules.MapVote.Models;
using MapChooserSharpMS.Modules.MapVote.Services;
using Microsoft.Extensions.Logging;

namespace MapChooserSharpMS.Modules.MapVote.Handlers;

internal sealed class InitialVoteNativeHandler(
    MapVoteControllingService service,
    VoteControllingManager voteManager,
    MapVoteInformation session,
    ILogger logger)
    : MapVoteNativeHandlerBase(service, voteManager, session, logger)
{
    protected override string VoteKind => "Initial";

    protected override bool IsRunoff => false;
}
