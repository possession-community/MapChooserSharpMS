using MapChooserSharpMS.Modules.MapVote.Models;
using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.MapVote.Managers;

namespace MapChooserSharpMS.Modules.MapVote.Managers;

internal sealed class VoteControllingManager : IVoteControllingManager
{
    private MapVoteInformation? _currentSession;

    public IMapVoteInformation? CurrentVote => _currentSession;

    internal MapVoteInformation? CurrentSession => _currentSession;

    internal MapVoteInformation CreateSession(bool isRtvVote)
    {
        var session = new MapVoteInformation { IsRtvVote = isRtvVote };
        _currentSession = session;
        return session;
    }

    internal void ClearSession()
    {
        _currentSession = null;
    }
}
