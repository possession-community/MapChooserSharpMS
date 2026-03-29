using System;
using MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit.Interfaces;

namespace MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit;

internal sealed class SystemTimeLimitClock : ITimeLimitClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
