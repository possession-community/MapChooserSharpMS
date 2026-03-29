using System;

namespace MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit.Interfaces;

internal interface ITimeLimitClock
{
    DateTime UtcNow { get; }
}
