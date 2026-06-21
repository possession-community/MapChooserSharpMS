using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Managers.MapTransition;

namespace MapChooserSharpMS.Modules.MapCycle.Managers.MapTransition.Interfaces;

internal interface IMcsInternalMapTransitionManager : IMapTransitionManager
{
    void SetCurrentMap(string mapName);

    void ClearState();

    void OnRoundEnd();

    bool IsIntermissionFired { get; }

    void BeginMapTransition(MapTransitionTrigger trigger, float? delayOverride = null);
}
