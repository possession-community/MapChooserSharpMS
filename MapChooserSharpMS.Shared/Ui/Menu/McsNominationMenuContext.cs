using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig.Services;
using MapChooserSharpMS.Shared.MapCycle.Services;
using MapChooserSharpMS.Shared.Nomination.Services;

namespace MapChooserSharpMS.Shared.Ui.Menu;

/// <summary>
/// Context passed to <see cref="IMcsNominationMenuCompat.ShowNominationMenu"/>.
/// Contains all data and services the compat needs to build a rich nomination menu.
/// </summary>
public sealed class McsNominationMenuContext
{
    public required string Title { get; init; }

    public required IReadOnlyList<McsNominationMenuItem> Items { get; init; }

    public required IMapConfigToolingService ToolingService { get; init; }

    public required IMapCooldownQueryService CooldownQueryService { get; init; }

    public required INominationMenuManagementService NominationMenuService { get; init; }
}
