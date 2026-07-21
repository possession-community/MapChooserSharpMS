using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.MapCycle.Cooldown;
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

    /// <summary>
    /// Runtime cooldown state store (map/group name keyed, config-independent).
    /// </summary>
    IMcsCooldownStore CooldownStore { get; }

    void InstallEventListener(IMapCycleEventListener listener);

    void RemoveEventListener(IMapCycleEventListener listener);
}

