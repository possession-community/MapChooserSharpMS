using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Shared.MapCycle.Managers.MapTransition;

/// <summary>
/// Wraps a map config with contextual metadata such as who nominated it.
/// </summary>
public interface IMapInformation
{
    /// <summary>
    /// The map configuration.
    /// </summary>
    IMapConfig MapConfig { get; }

    /// <summary>
    /// SteamIDs of the players who nominated this map, in nomination order.
    /// Empty if the map was set by admin, random pick, or API.
    /// </summary>
    IReadOnlyList<ulong> NominatorSteamIds { get; }
}
