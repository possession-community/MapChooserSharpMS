using MapChooserSharpMS.Shared.Nomination.Services;

namespace MapChooserSharpMS.Shared.Ui.Menu;

/// <summary>
/// Menu compat adapter for nomination-related menus
/// (map list, confirm, admin nominate, remove nomination).
/// </summary>
public interface IMcsNominationMenuCompat : IMcsMenuCompat
{
    /// <summary>
    /// Set by MCS during registration. Provides access to nomination menu services
    /// (e.g. <see cref="INominationMenuManagementService.CollectExtraMenuItems"/>).
    /// Initialize with <c>null!</c> in constructor — MCS sets this before any menu is shown.
    /// </summary>
    INominationMenuManagementService NominationMenuService { get; set; }
}
