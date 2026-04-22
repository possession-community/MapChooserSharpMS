using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Ui.Menu;

/// <summary>
/// Low-level abstraction that renders MapChooserSharpMS menus through a concrete
/// menu plugin (e.g. FPM <c>MenuManager</c>). MCS itself never talks to a menu
/// library directly — it builds <see cref="McsMenuDefinition"/> instances and
/// hands them to whatever implementation was registered via
/// <see cref="IMapChooserSharpShared.SetDefaultMenuCompat"/>.
/// </summary>
public interface IMcsMenuCompat
{
    /// <summary>
    /// Display <paramref name="menu"/> to <paramref name="target"/>. Any MCS menu
    /// previously opened for this client via this compat should be closed first.
    /// </summary>
    void ShowMenu(IGameClient target, McsMenuDefinition menu);

    /// <summary>
    /// Close the currently open MCS menu for <paramref name="target"/>, if any.
    /// No-op when the client is not in a menu owned by this compat.
    /// </summary>
    void CloseMenu(IGameClient target);

    /// <summary>
    /// Drop all cached menu state. Invoked on plugin unload / map change as needed.
    /// </summary>
    void Cleanup();
}
