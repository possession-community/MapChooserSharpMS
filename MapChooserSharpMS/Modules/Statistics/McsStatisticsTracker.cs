using System;

namespace MapChooserSharpMS.Modules.Statistics;

internal sealed class McsStatisticsTracker
{
    private int _currentPlayerCount;
    private int _peakPlayerCount;
    private int _roundCount;
    private DateTime _mapStartedAt;
    private string _mapEndReason = "unknown";

    public int CurrentPlayerCount => _currentPlayerCount;
    public int PeakPlayerCount => _peakPlayerCount;
    public int RoundCount => _roundCount;
    public DateTime MapStartedAt => _mapStartedAt;
    public string MapEndReason => _mapEndReason;

    public void ResetForNewMap()
    {
        _currentPlayerCount = 0;
        _peakPlayerCount = 0;
        _roundCount = 0;
        _mapStartedAt = DateTime.UtcNow;
        _mapEndReason = "unknown";
    }

    public void OnPlayerConnected()
    {
        _currentPlayerCount++;
        if (_currentPlayerCount > _peakPlayerCount)
            _peakPlayerCount = _currentPlayerCount;
    }

    public void OnPlayerDisconnected()
    {
        if (_currentPlayerCount > 0)
            _currentPlayerCount--;
    }

    public void OnRoundEnd()
    {
        _roundCount++;
    }

    public void SetCurrentPlayerCount(int count)
    {
        _currentPlayerCount = count;
        if (_currentPlayerCount > _peakPlayerCount)
            _peakPlayerCount = _currentPlayerCount;
    }

    public void SetMapEndReason(string reason)
    {
        if (_mapEndReason == "unknown")
            _mapEndReason = reason;
    }
}
