using System.Collections.Generic;
using MapChooserSharpMS.Shared.Nomination.Services;
using MapChooserSharpMS.Shared.Ui.Menu;
using Sharp.Modules.MenuManager.Shared;
using Sharp.Shared.Objects;

namespace McsFPMCompat;

/// <summary>
/// FPM MenuManager backed implementation for nomination menus.
/// <para>
/// Example — collecting extra menu items in a detail sub-menu:
/// <code>
/// var extras = NominationMenuService.CollectExtraMenuItems(item.MapConfig, target);
/// foreach (var extra in extras)
///     detailBuilder.Item(extra.DisplayText, ctrl => { ctrl.Exit(); extra.OnSelect?.Invoke(target); });
/// </code>
/// </para>
/// </summary>
public sealed class FpmNominationMenuCompat(IMenuManager menuManager) : IMcsNominationMenuCompat
{
    private readonly Dictionary<IGameClient, Menu> _activeMenus = new();

    public INominationMenuManagementService NominationMenuService { get; set; } = null!;

    public void ShowNominationMenu(IGameClient target, McsNominationMenuContext context)
    {
        var builder = Menu.Create();
        builder.Title(context.Title);

        foreach (var item in context.Items)
        {
            var capturedItem = item;
            builder.Item(item.DisplayText, controller =>
            {
                controller.Exit();
                capturedItem.OnNominate?.Invoke(target);
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
