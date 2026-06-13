using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.MapCycle.Managers.MapTransition;
using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;
using MapChooserSharpMS.Shared.MapCycle.Services;

namespace MapChooserSharpMS.Shared.MapCycle;

public interface IMapCycleController
{
    ITimeLimitManager CurrentMapTimeLimitManager { get; }

    IMapTransitionManager MapTransitionManager { get; }

    IMapCooldownQueryService  MapCooldownQueryService { get; }

    IMapCooldownCommandService  MapCooldownCommandService { get; }

    void InstallEventListener(IMapCycleEventListener listener);

    void RemoveEventListener(IMapCycleEventListener listener);
}

