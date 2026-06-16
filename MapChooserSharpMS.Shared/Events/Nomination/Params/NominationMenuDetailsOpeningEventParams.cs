using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.Ui.Menu;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Events.Nomination.Params;

/// <summary>
/// Fired when a nomination detail/confirm menu is about to be shown.
/// Listeners can append extra menu items (e.g. map info, cost display).
/// </summary>
public interface INominationMenuDetailsOpeningParams : IEventBaseParams
{
    IMapConfig MapConfig { get; }

    IGameClient Client { get; }

    List<McsMenuItem> ExtraItems { get; }
}
