using System;
using System.Collections.Generic;
using MapChooserSharpMS.Modules.Audit.Services;

namespace MapChooserSharpMS.Modules.Audit.Collectors;

internal sealed class RtvAuditCollector
{
    private readonly string _serverId;
    private readonly List<(ulong steamId, DateTime votedAt)> _pendingVotes = new();

    internal RtvAuditCollector(string serverId)
    {
        _serverId = serverId;
    }

    internal void OnClientRtvCast(ulong steamId)
    {
        _pendingVotes.Add((steamId, DateTime.UtcNow));
    }

    internal void OnClientRtvUnCast(ulong steamId)
    {
        _pendingVotes.RemoveAll(v => v.steamId == steamId);
    }

    internal (AuditRtv rtv, List<AuditRtvVote> votes) BuildTriggerRecord(
        int threshold, int? immediateThreshold, bool isForced, bool isNextMapConfirmed)
    {
        string id = Guid.NewGuid().ToString("N");
        string mapState = isNextMapConfirmed ? "next_map_confirmed" : "next_map_not_confirmed";

        var rtv = new AuditRtv(
            Id: id,
            TriggeredAt: DateTime.UtcNow,
            Threshold: threshold,
            ImmediateThreshold: immediateThreshold,
            IsForced: isForced,
            MapState: mapState,
            ServerId: _serverId);

        var votes = new List<AuditRtvVote>(_pendingVotes.Count);
        foreach (var (steamId, votedAt) in _pendingVotes)
            votes.Add(new AuditRtvVote(id, steamId, votedAt));

        _pendingVotes.Clear();
        return (rtv, votes);
    }

    internal void Reset()
    {
        _pendingVotes.Clear();
    }
}
