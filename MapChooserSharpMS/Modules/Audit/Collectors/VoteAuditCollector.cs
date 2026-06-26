using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.Audit.Services;
using MapChooserSharpMS.Modules.MapVote;
using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.Nomination;

namespace MapChooserSharpMS.Modules.Audit.Collectors;

internal sealed class VoteAuditCollector
{
    private readonly string _serverId;

    private DateTime _voteStartedAt;
    private string _startReason = "";
    private float _voteDurationConfig;
    private int _totalPlayers;
    private List<(string mapName, Shared.MapConfig.IMapConfig? mapConfig)>? _initialCandidates;

    internal VoteAuditCollector(string serverId)
    {
        _serverId = serverId;
    }

    internal void OnVoteInitiated(bool isRtv, float voteDuration, int participantCount)
    {
        _voteStartedAt = DateTime.UtcNow;
        _startReason = isRtv ? "rtv" : "timelimit";
        _voteDurationConfig = voteDuration;
        _totalPlayers = participantCount;
        _initialCandidates = null;
    }

    internal void CaptureInitialCandidates(IReadOnlyCollection<IMapVoteOption> candidates)
    {
        _initialCandidates = candidates.Select(c => (c.MapName, c.MapConfig)).ToList();
    }

    internal (AuditVote vote, List<AuditVoteCandidate> candidates) BuildFinishedRecord(
        IMapVoteInformation voteInfo,
        IReadOnlyDictionary<string, IMcsNominationData> nominatedMaps)
    {
        string voteId = Guid.NewGuid().ToString("N");
        int totalVotes = voteInfo.VoteOptions.Sum(o => o.VoteParticipants.Count);

        var vote = new AuditVote(
            Id: voteId,
            VoteStartedAt: _voteStartedAt,
            VoteEndedAt: DateTime.UtcNow,
            VoteResult: "completed",
            MapVoteStartReason: _startReason,
            VoteDurationConfig: _voteDurationConfig,
            TotalPlayers: _totalPlayers,
            TotalVotes: totalVotes,
            ServerId: _serverId);

        var processedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<AuditVoteCandidate>();

        foreach (var option in voteInfo.VoteOptions)
        {
            processedNames.Add(option.MapName);
            candidates.Add(BuildCandidate(voteId, option.MapName, option.MapConfig,
                option.VoteParticipants.Count,
                voteInfo.Winner is not null && string.Equals(voteInfo.Winner.MapName, option.MapName, StringComparison.OrdinalIgnoreCase),
                nominatedMaps.ContainsKey(option.MapName)));
        }

        if (_initialCandidates is not null)
        {
            foreach (var (mapName, mapConfig) in _initialCandidates)
            {
                if (processedNames.Contains(mapName))
                    continue;

                candidates.Add(BuildCandidate(voteId, mapName, mapConfig,
                    0, false, nominatedMaps.ContainsKey(mapName)));
            }
        }

        return (vote, candidates);
    }

    private static AuditVoteCandidate BuildCandidate(string voteId, string mapName,
        Shared.MapConfig.IMapConfig? mapConfig, int voteCount, bool isWinner, bool isNominated)
    {
        string candidateType;
        if (mapName == MapVoteConstants.ExtendMapInternalName)
            candidateType = "extend";
        else if (mapName == MapVoteConstants.DontChangeMapInternalName)
            candidateType = "dont_change";
        else
            candidateType = "map";

        return new AuditVoteCandidate(
            VoteId: voteId,
            MapName: mapName,
            WorkshopId: mapConfig?.WorkshopId is > 0 ? mapConfig.WorkshopId : null,
            VoteCount: voteCount,
            IsWinner: isWinner,
            IsNominated: isNominated,
            CandidateType: candidateType);
    }

    internal AuditVote BuildCancelledRecord()
    {
        return new AuditVote(
            Id: Guid.NewGuid().ToString("N"),
            VoteStartedAt: _voteStartedAt,
            VoteEndedAt: DateTime.UtcNow,
            VoteResult: "cancelled",
            MapVoteStartReason: _startReason,
            VoteDurationConfig: _voteDurationConfig,
            TotalPlayers: _totalPlayers,
            TotalVotes: 0,
            ServerId: _serverId);
    }
}
