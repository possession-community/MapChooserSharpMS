using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;

namespace MapChooserSharpMS.Shared.Events.MapCycle.Params;

public interface IVoteStartThresholdReachedEventParams : IEventBaseParams
{
    TimeLimitType LimitType { get; }
}
