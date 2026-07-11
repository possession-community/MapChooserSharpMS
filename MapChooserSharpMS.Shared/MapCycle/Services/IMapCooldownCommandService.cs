using System;
using System.Threading.Tasks;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Shared.MapCycle.Services;

public interface IMapCooldownCommandService
{
    /// <summary>
    /// Applies cooldown to specified map.
    /// </summary>
    /// <param name="mapConfig"></param>
    /// <param name="cooldown"></param>
    /// <returns>True when successfully saved to database, otherwise false</returns>
    Task<bool> SetCooldown(IMapConfig mapConfig, int cooldown);
    
    /// <summary>
    /// Applies timed cooldown to specified map.
    /// </summary>
    /// <param name="mapConfig"></param>
    /// <param name="cooldown"></param>
    /// <returns>True when successfully saved to database, otherwise false</returns>
    Task<bool> SetTimedCooldown(IMapConfig mapConfig, TimeSpan cooldown);
    
    /// <summary>
    /// Sets the map cooldown to <see cref="int.MaxValue"/>, effectively excluding
    /// the map from nomination and random picking until the cooldown is cleared.
    /// Note: this manipulates the <b>map</b> cooldown (play-based), not the
    /// planned nomination cooldown axis.
    /// </summary>
    /// <param name="mapConfig"></param>
    /// <returns>True when successfully saved to database, otherwise false</returns>
    Task<bool> ExcludeFromNomination(IMapConfig mapConfig);
    
    /// <summary>
    /// This will try to clear cooldown
    /// </summary>
    /// <param name="mapConfig"></param>
    /// <returns>True when successfully saved to database, otherwise false</returns>
    Task<bool> ClearCooldown(IMapConfig mapConfig);
    
    /// <summary>
    /// This will try to clear timed cooldown
    /// </summary>
    /// <param name="mapConfig"></param>
    /// <returns>True when successfully saved to database, otherwise false</returns>
    Task<bool> ClearTimedCooldown(IMapConfig mapConfig);

    /// <summary>
    /// Applies count-based cooldown to all variants of the specified group.
    /// </summary>
    /// <param name="groupName">Group name as defined in the map config</param>
    /// <param name="cooldown">Cooldown count to set</param>
    /// <returns>True when at least one variant was updated, otherwise false</returns>
    Task<bool> SetGroupCooldown(string groupName, int cooldown);

    /// <summary>
    /// Applies timed cooldown to all variants of the specified group.
    /// </summary>
    /// <param name="groupName">Group name as defined in the map config</param>
    /// <param name="cooldown">Duration of the timed cooldown</param>
    /// <returns>True when at least one variant was updated, otherwise false</returns>
    Task<bool> SetGroupTimedCooldown(string groupName, TimeSpan cooldown);

    /// <summary>
    /// Clears the count-based cooldown for the specified group (sets to 0).
    /// </summary>
    /// <param name="groupName">Group name as defined in the map config</param>
    /// <returns>True when at least one variant was updated, otherwise false</returns>
    Task<bool> ClearGroupCooldown(string groupName);

    /// <summary>
    /// Clears the timed cooldown for the specified group.
    /// </summary>
    /// <param name="groupName">Group name as defined in the map config</param>
    /// <returns>True when at least one variant was updated, otherwise false</returns>
    Task<bool> ClearGroupTimedCooldown(string groupName);
}