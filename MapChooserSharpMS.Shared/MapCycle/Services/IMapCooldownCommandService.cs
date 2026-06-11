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
}