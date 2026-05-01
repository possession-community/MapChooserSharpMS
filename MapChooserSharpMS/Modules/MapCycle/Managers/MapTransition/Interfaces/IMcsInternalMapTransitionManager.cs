using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Managers.MapTransition;

namespace MapChooserSharpMS.Modules.MapCycle.Managers.MapTransition.Interfaces;

/// <summary>
/// Internal interface for MapTransitionManager. Adds writers / lifecycle
/// hooks that the public <see cref="IMapTransitionManager"/> intentionally
/// does not expose to external consumers.
/// </summary>
internal interface IMcsInternalMapTransitionManager : IMapTransitionManager
{
    /// <summary>
    /// Resolve and cache the current map's <see cref="IMapConfig"/> from
    /// the given map name. Called by MapCycle on map activation.
    /// Sets <see cref="IMapTransitionManager.CurrentMap"/> to null when no
    /// matching config is found in MapConfigProvider.
    /// </summary>
    void SetCurrentMap(string mapName);

    /// <summary>
    /// Clear current/next map state on map deactivation.
    /// </summary>
    void ClearState();

    /// <summary>
    /// Notify the manager that a round has ended. When
    /// <see cref="IMapTransitionManager.ChangeMapOnNextRoundEnd"/> is set
    /// and a next map is configured, this triggers
    /// <see cref="IMapTransitionManager.TransitionToNextMap"/>.
    /// </summary>
    void OnRoundEnd();
}
