using MapChooserSharpMS.Shared.Nomination.Services;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Ui.Menu;

/// <summary>
/// Menu compat adapter for nomination-related menus.
/// </summary>
public interface IMcsNominationMenuCompat
{
    /// <summary>
    /// Set by MCS during registration. Provides access to nomination menu services
    /// (e.g. <see cref="INominationMenuManagementService.CollectExtraMenuItems"/>).
    /// Initialize with <c>null!</c> in constructor — MCS sets this before any menu is shown.
    /// </summary>
    INominationMenuManagementService NominationMenuService { get; set; }

    void ShowNominationMenu(IGameClient target, McsNominationMenuContext context);

    void CloseMenu(IGameClient target);

    void Cleanup();
}
