using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;

namespace MapChooserSharpMS.Shared.Events.MapCycle.Params;

public interface ITimeLimitReachedEventParams : IEventBaseParams
{
    TimeLimitType LimitType { get; }
}
