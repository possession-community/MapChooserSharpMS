using System;
using System.Globalization;
using System.Threading;
using MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit.Interfaces;
using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;

namespace MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit;

internal sealed class TimeBasedTimeLimitManager : IInternalTimeBasedTimeLimitManager
{
    private readonly Lock _lock = new();
    private readonly ITimeLimitClock _clock;

    private DateTime _endTimeUtc;
    private TimeSpan? _cachedTimeLeft;

    public TimeBasedTimeLimitManager(
        TimeSpan initialTimeLimit,
        ITimeLimitClock clock)
    {
        _clock = clock;
        _endTimeUtc = clock.UtcNow + initialTimeLimit;
    }

    public TimeLimitType TimeLimitType => TimeLimitType.Time;

    public bool IsLimitReached => TimeLeft <= TimeSpan.Zero;

    public TimeSpan TimeLeft
    {
        get
        {
            lock (_lock)
            {
                if (_cachedTimeLeft is { } cached)
                    return cached;

                _cachedTimeLeft = ComputeTimeLeft();
                return _cachedTimeLeft.Value;
            }
        }
    }

    public bool Extend(TimeSpan time)
    {
        lock (_lock)
        {
            SetCore(ComputeTimeLeft() + time);
        }

        return true;
    }

    public bool Set(TimeSpan time)
    {
        lock (_lock)
        {
            SetCore(time);
        }

        return true;
    }

    private TimeSpan ComputeTimeLeft()
    {
        var remaining = _endTimeUtc - _clock.UtcNow;
        return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    private void SetCore(TimeSpan time)
    {
        _endTimeUtc = _clock.UtcNow + time;
        _cachedTimeLeft = null;
    }

    // TODO Translation support
    public string GetFormattedTimeLeft(CultureInfo? info = null)
    {
        var remaining = TimeLeft;

        if (remaining <= TimeSpan.Zero)
            return "ThresholdReached";

        if (remaining.TotalHours >= 1)
            return $"{(int)remaining.TotalHours}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";

        return $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
    }

    public void OnTick()
    {
        lock (_lock)
        {
            _cachedTimeLeft = null;
            _cachedTimeLeft = ComputeTimeLeft();
        }
    }
}
