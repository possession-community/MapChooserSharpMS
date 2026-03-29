using System;
using MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit;
using MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit.Interfaces;

namespace MapChooserSharpMS.Tests.MapCycle.TimeLimit;

internal sealed class FakeTimeLimitClock : ITimeLimitClock
{
    public DateTime UtcNow { get; set; } = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Advance(TimeSpan duration) => UtcNow += duration;
}
