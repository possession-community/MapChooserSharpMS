using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MapChooserSharpMS.Modules.Audit.Services;

internal interface IAuditPersistence
{
    Task EnsureSchemasAsync(CancellationToken ct = default);

    void InsertMapPlayFireAndForget(AuditMapPlay record);

    void InsertNominationsFireAndForget(IReadOnlyList<AuditNomination> records);

    void InsertVoteFireAndForget(AuditVote vote, IReadOnlyList<AuditVoteCandidate> candidates);

    void InsertExtendVoteFireAndForget(AuditExtendVote record);

    void InsertRtvFireAndForget(AuditRtv rtv, IReadOnlyList<AuditRtvVote> votes);

    void InsertExtFireAndForget(AuditExt ext, IReadOnlyList<AuditExtVote> votes);

    void InsertCooldownExpiredFireAndForget(AuditCooldownExpired record);
}

internal sealed record AuditMapPlay(
    string MapName,
    long? WorkshopId,
    IReadOnlyList<string> GroupNames,
    int PeakPlayerCount,
    int EndPlayerCount,
    DateTime MapStartedAt,
    DateTime MapEndedAt,
    string MapEndReason,
    int RoundCount,
    string TimelimitType,
    float ConfiguredTimelimit,
    string ServerId,
    int ExtendCount,
    int MaxNormalExtends,
    int NormalExtendsUsed,
    int AdminVoteExtendCount,
    int UserExtExtendsUsed,
    int MaxUserExtExtends);

internal sealed record AuditNomination(
    DateTime NominatedAt,
    string MapName,
    long? WorkshopId,
    ulong? NominatorSteamId,
    string NominationType,
    string NominationResult,
    string GroupName,
    string ServerId);

internal sealed record AuditVote(
    string Id,
    DateTime VoteStartedAt,
    DateTime VoteEndedAt,
    string VoteResult,
    string MapVoteStartReason,
    float VoteDurationConfig,
    int TotalPlayers,
    int TotalVotes,
    string ServerId);

internal sealed record AuditVoteCandidate(
    string VoteId,
    string MapName,
    long? WorkshopId,
    int VoteCount,
    bool IsWinner,
    bool IsNominated,
    string CandidateType);

internal sealed record AuditExtendVote(
    DateTime VoteStartedAt,
    DateTime VoteEndedAt,
    string VoteResult,
    float SuccessThreshold,
    int YesCount,
    int NoCount,
    int TotalPlayers,
    bool Passed,
    ulong? InitiatorSteamId,
    string ServerId);

internal sealed record AuditRtv(
    string Id,
    DateTime TriggeredAt,
    int Threshold,
    int? ImmediateThreshold,
    bool IsForced,
    string MapState,
    string ServerId);

internal sealed record AuditRtvVote(
    string RtvId,
    ulong SteamId,
    DateTime VotedAt);

internal sealed record AuditExt(
    string Id,
    DateTime TriggeredAt,
    int Threshold,
    string MapState,
    string ServerId);

internal sealed record AuditExtVote(
    string ExtId,
    ulong SteamId,
    DateTime VotedAt);

internal sealed record AuditCooldownExpired(
    string Name,
    string CooldownType,
    DateTime BecameAvailableAt,
    string ServerId);
