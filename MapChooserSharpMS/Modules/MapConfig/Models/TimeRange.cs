using System;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapConfig.Models;

internal sealed class TimeRange : ITimeRange
{
    public TimeOnly StartTime { get; }
    public TimeOnly EndTime { get; }

    public TimeRange(TimeOnly startTime, TimeOnly endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
    }

    /// <summary>
    /// Parses a time range string in "HH:mm-HH:mm" format.
    /// </summary>
    public static TimeRange Parse(string rangeString)
    {
        var dashIndex = rangeString.IndexOf('-');
        if (dashIndex < 0)
            throw new FormatException($"Invalid time range format: '{rangeString}'. Expected 'HH:mm-HH:mm'.");

        var startPart = rangeString.AsSpan(0, dashIndex);
        var endPart = rangeString.AsSpan(dashIndex + 1);

        var start = TimeOnly.Parse(startPart);
        var end = TimeOnly.Parse(endPart);

        return new TimeRange(start, end);
    }

    public bool IsInRange(TimeOnly time)
    {
        // 00:00-00:00 means always in range (not configured)
        if (StartTime == EndTime)
            return true;

        // Overnight range: e.g., 22:00-03:00
        if (StartTime > EndTime)
            return time >= StartTime || time < EndTime;

        // Normal range: e.g., 10:00-12:00
        return time >= StartTime && time < EndTime;
    }
}
