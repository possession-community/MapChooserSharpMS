using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.Audit.Services;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.Nomination;

namespace MapChooserSharpMS.Modules.Audit.Collectors;

internal sealed class NominationAuditCollector
{
    private readonly string _serverId;
    private readonly List<PendingNomination> _pending = new();

    internal NominationAuditCollector(string serverId)
    {
        _serverId = serverId;
    }

    internal void OnNomination(IMapConfig mapConfig, ulong? steamId, string nominationType, DateTime at)
    {
        _pending.Add(new PendingNomination(
            mapConfig.MapName,
            mapConfig.WorkshopId == 0 ? null : mapConfig.WorkshopId,
            steamId,
            nominationType,
            mapConfig.GroupSettings.FirstOrDefault()?.GroupName ?? "",
            at));
    }

    internal List<AuditNomination> OnNominationCancelled(string mapName, string result, ulong? steamId)
    {
        var matching = _pending.Where(p => p.MapName == mapName
            && (steamId == null || p.NominatorSteamId == steamId)).ToList();

        var records = new List<AuditNomination>();
        foreach (var p in matching)
        {
            _pending.Remove(p);
            records.Add(new AuditNomination(
                p.NominatedAt, p.MapName, p.WorkshopId, p.NominatorSteamId,
                p.NominationType, result, p.GroupName, _serverId));
        }
        return records;
    }

    internal List<AuditNomination> OnVoteStarted(IReadOnlyCollection<IMapVoteOption> voteCandidates)
    {
        var candidateNames = new HashSet<string>(
            voteCandidates.Select(c => c.MapName), StringComparer.OrdinalIgnoreCase);

        var notPicked = _pending.Where(p => !candidateNames.Contains(p.MapName)).ToList();
        var records = new List<AuditNomination>();
        foreach (var p in notPicked)
        {
            _pending.Remove(p);
            records.Add(new AuditNomination(
                p.NominatedAt, p.MapName, p.WorkshopId, p.NominatorSteamId,
                p.NominationType, "not_picked", p.GroupName, _serverId));
        }
        return records;
    }

    internal List<AuditNomination> OnVoteFinished(IMapVoteOption? winner,
        IReadOnlyDictionary<string, IMcsNominationData> nominatedMaps)
    {
        var records = new List<AuditNomination>();
        string winnerName = winner?.MapName ?? "";

        foreach (var p in _pending)
        {
            string result = string.Equals(p.MapName, winnerName, StringComparison.OrdinalIgnoreCase)
                ? "voted_won"
                : "voted_lost";

            records.Add(new AuditNomination(
                p.NominatedAt, p.MapName, p.WorkshopId, p.NominatorSteamId,
                p.NominationType, result, p.GroupName, _serverId));
        }

        _pending.Clear();
        return records;
    }

    internal void Reset()
    {
        _pending.Clear();
    }

    private sealed record PendingNomination(
        string MapName,
        long? WorkshopId,
        ulong? NominatorSteamId,
        string NominationType,
        string GroupName,
        DateTime NominatedAt);
}
