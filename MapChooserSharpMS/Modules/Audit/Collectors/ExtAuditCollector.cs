using System;
using System.Collections.Generic;
using MapChooserSharpMS.Modules.Audit.Services;

namespace MapChooserSharpMS.Modules.Audit.Collectors;

internal sealed class ExtAuditCollector
{
    private readonly string _serverId;
    private readonly List<(ulong steamId, DateTime votedAt)> _pendingVotes = new();

    internal ExtAuditCollector(string serverId)
    {
        _serverId = serverId;
    }

    internal void OnExtCommandExecute(ulong steamId)
    {
        _pendingVotes.Add((steamId, DateTime.UtcNow));
    }

    internal (AuditExt ext, List<AuditExtVote> votes) BuildTriggerRecord(
        int threshold, bool isNextMapConfirmed)
    {
        string id = Guid.NewGuid().ToString("N");
        string mapState = isNextMapConfirmed ? "next_map_confirmed" : "next_map_not_confirmed";

        var ext = new AuditExt(
            Id: id,
            TriggeredAt: DateTime.UtcNow,
            Threshold: threshold,
            MapState: mapState,
            ServerId: _serverId);

        var votes = new List<AuditExtVote>(_pendingVotes.Count);
        foreach (var (steamId, votedAt) in _pendingVotes)
            votes.Add(new AuditExtVote(id, steamId, votedAt));

        _pendingVotes.Clear();
        return (ext, votes);
    }

    internal void Reset()
    {
        _pendingVotes.Clear();
    }
}
