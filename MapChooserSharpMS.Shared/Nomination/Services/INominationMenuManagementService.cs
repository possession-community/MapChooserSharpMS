using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;
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
}