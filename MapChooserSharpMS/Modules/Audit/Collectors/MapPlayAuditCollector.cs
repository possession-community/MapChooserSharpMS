using System;
using System.Collections.Generic;
using MapChooserSharpMS.Modules.Audit.Services;
using MapChooserSharpMS.Modules.Statistics;

namespace MapChooserSharpMS.Modules.Audit.Collectors;

internal sealed class MapPlayAuditCollector
{
    private readonly McsStatisticsTracker _tracker;
    private readonly string _serverId;

    private string _mapName = "";
    private long? _workshopId;
    private IReadOnlyList<string> _groupNames = Array.Empty<string>();
    private string _timelimitType = "";
    private float _configuredTimelimit;
    private int _initialMaxExtends;
    private int _initialMaxExtCommandUses;
    private int _extendCount;
    private int _adminVoteExtendCount;

    internal MapPlayAuditCollector(McsStatisticsTracker tracker, string serverId)
    {
        _tracker = tracker;
        _serverId = serverId;
    }

    internal void OnMapStart(string mapName, long? workshopId, IReadOnlyList<string> groupNames,
        string timelimitType, float configuredTimelimit, int maxExtends, int maxExtCommandUses)
    {
        _mapName = mapName;
        _workshopId = workshopId;
        _groupNames = groupNames;
        _timelimitType = timelimitType;
        _configuredTimelimit = configuredTimelimit;
        _initialMaxExtends = maxExtends;
        _initialMaxExtCommandUses = maxExtCommandUses;
        _extendCount = 0;
        _adminVoteExtendCount = 0;
    }

    internal void OnExtend()
    {
        _extendCount++;
    }

    internal void OnAdminExtend()
    {
        _adminVoteExtendCount++;
        _extendCount++;
    }

    internal AuditMapPlay? BuildRecord()
    {
        if (string.IsNullOrEmpty(_mapName))
            return null;

        return new AuditMapPlay(
            MapName: _mapName,
            WorkshopId: _workshopId,
            GroupNames: _groupNames,
            PeakPlayerCount: _tracker.PeakPlayerCount,
            EndPlayerCount: _tracker.CurrentPlayerCount,
            MapStartedAt: _tracker.MapStartedAt,
            MapEndedAt: DateTime.UtcNow,
            MapEndReason: _tracker.MapEndReason,
            RoundCount: _tracker.RoundCount,
            TimelimitType: _timelimitType,
            ConfiguredTimelimit: _configuredTimelimit,
            ServerId: _serverId,
            ExtendCount: _extendCount,
            MaxNormalExtends: _initialMaxExtends,
            NormalExtendsUsed: 0,
            AdminVoteExtendCount: _adminVoteExtendCount,
            UserExtExtendsUsed: 0,
            MaxUserExtExtends: _initialMaxExtCommandUses);
    }
}
