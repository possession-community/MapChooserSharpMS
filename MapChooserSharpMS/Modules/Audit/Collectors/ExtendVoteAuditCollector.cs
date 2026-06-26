using System;
using MapChooserSharpMS.Modules.Audit.Services;

namespace MapChooserSharpMS.Modules.Audit.Collectors;

internal sealed class ExtendVoteAuditCollector
{
    private readonly string _serverId;
    private readonly Func<float> _successThresholdProvider;

    private DateTime _voteStartedAt;
    private int _totalPlayers;
    private ulong? _initiatorSteamId;

    internal ExtendVoteAuditCollector(string serverId, Func<float> successThresholdProvider)
    {
        _serverId = serverId;
        _successThresholdProvider = successThresholdProvider;
    }

    internal void OnVoteStarted(int totalPlayers, ulong? initiatorSteamId)
    {
        _voteStartedAt = DateTime.UtcNow;
        _totalPlayers = totalPlayers;
        _initiatorSteamId = initiatorSteamId;
    }

    internal AuditExtendVote BuildFinishedRecord(bool passed, int yesCount, int noCount)
    {
        return new AuditExtendVote(
            VoteStartedAt: _voteStartedAt,
            VoteEndedAt: DateTime.UtcNow,
            VoteResult: "completed",
            SuccessThreshold: _successThresholdProvider(),
            YesCount: yesCount,
            NoCount: noCount,
            TotalPlayers: _totalPlayers,
            Passed: passed,
            InitiatorSteamId: _initiatorSteamId,
            ServerId: _serverId);
    }

    internal AuditExtendVote BuildCancelledRecord()
    {
        return new AuditExtendVote(
            VoteStartedAt: _voteStartedAt,
            VoteEndedAt: DateTime.UtcNow,
            VoteResult: "cancelled",
            SuccessThreshold: _successThresholdProvider(),
            YesCount: 0,
            NoCount: 0,
            TotalPlayers: _totalPlayers,
            Passed: false,
            InitiatorSteamId: _initiatorSteamId,
            ServerId: _serverId);
    }
}
