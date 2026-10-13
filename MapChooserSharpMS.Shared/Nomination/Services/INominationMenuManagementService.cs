using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.Ui.Menu;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Nomination.Services;

public interface INominationMenuManagementService
{
    /// <summary>
    /// Shows nomination menu with specified configs list
    /// </summary>
    /// <param name="client"></param>
    /// <param name="configs"></param>
    void ShowNominationMenu(IGameClient client, List<IMapConfig> configs);

    /// <summary>
    /// Shows nomination menu with all maps
    /// </summary>
    /// <param name="client"></param>
    void ShowNominationMenu(IGameClient client);
    
    /// <summary>
    /// Shows the browsable list of all maps (<c>!maplist</c>). Same flow as the
    /// nomination menu — sort selection, list, per-map details — under its own title.
    /// </summary>
    /// <param name="client"></param>
    void ShowMapListMenu(IGameClient client);

    /// <summary>
    /// Shows the browsable map list restricted to the specified configs
    /// (<c>!maplist &lt;query&gt;</c>). Sort selection stays available.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="configs"></param>
    void ShowMapListMenu(IGameClient client, List<IMapConfig> configs);

    /// <summary>
    /// Shows admin nomination menu with specified configs list
    /// </summary>
    /// <param name="client"></param>
    /// <param name="configs"></param>
    void ShowAdminNominationMenu(IGameClient client, List<IMapConfig> configs);
    
    /// <summary>
    /// Shows admin nomination menu with all maps
    /// </summary>
    /// <param name="client"></param>
    void ShowAdminNominationMenu(IGameClient client);
    
    /// <summary>
    /// Shows nomination remove menu with specified nomination data
    /// </summary>
    /// <param name="client"></param>
    /// <param name="configs"></param>
    void ShowRemoveNominationMenu(IGameClient client, List<IMcsNominationData> configs);
    
    /// <summary>
    /// Shows nomination remove menu with all nomination data
    /// </summary>
    /// <param name="client"></param>
    void ShowRemoveNominationMenu(IGameClient client);

    /// <summary>
    /// Nominates a map. Non-admin nominations always show a confirmation menu first;
    /// admin nominations are executed immediately.
    /// </summary>
    void NominateOrConfirm(IGameClient client, IMapConfig config, bool isAdmin);

    /// <summary>
    /// Fires OnNominationMenuDetailsOpening and returns the collected extra items.
    /// </summary>
    List<McsMenuItem> CollectExtraMenuItems(IMapConfig mapConfig, IGameClient client);
}