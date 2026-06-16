using System.Collections.Generic;
using MapChooserSharpMS.Shared.Ui.Menu;
using Sharp.Modules.MenuManager.Shared;
using Sharp.Shared.Objects;

namespace McsFPMCompat;

/// <summary>
/// FPM <c>MenuManager</c> backed implementation of <see cref="IMcsMenuCompat"/>.
/// Stateless except for tracking the single active menu per client so
/// <see cref="CloseMenu"/> can match against the right instance.
/// </summary>
public sealed class FpmMcsMenuCompat(IMenuManager menuManager) : IMcsNominationMenuCompat, IMcsVoteMenuCompat
{
    private readonly Dictionary<IGameClient, Menu> _activeMenus = new();

    public void ShowMenu(IGameClient target, McsMenuDefinition menu)
    {
        var builder = Menu.Create();
        builder.Title(menu.Title);

        foreach (var item in menu.Items)
        {
            var onSelect = item.OnSelect;
            builder.Item(item.DisplayText, controller =>
            {
                controller.Exit();
                onSelect?.Invoke(target);
            });
        }

        var built = builder.Build();
        _activeMenus[target] = built;

        menuManager.DisplayMenu(target, built);
    }

    public void CloseMenu(IGameClient target)
    {
        if (!_activeMenus.TryGetValue(target, out var menu))
            return;

        if (menuManager.IsInCurrentMenu(target, menu))
            menuManager.QuitMenu(target);

        _activeMenus.Remove(target);
    }

    public void Cleanup()
    {
        _activeMenus.Clear();
    }
}
