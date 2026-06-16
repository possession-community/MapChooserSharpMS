using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Ui.Menu;

/// <summary>
/// Menu compat adapter for vote-related menus.
/// </summary>
public interface IMcsVoteMenuCompat
{
    void ShowMenu(IGameClient target, McsVoteMenuDefinition menu);

    void CloseMenu(IGameClient target);

    void Cleanup();
}
