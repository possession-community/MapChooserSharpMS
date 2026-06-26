using System.Collections.Generic;

namespace MapChooserSharpMS.Shared.MapConfig;

/// <summary>
/// Map group's name and cooldown
/// </summary>
public interface IMapGroupConfig: IBaseMapConfig
{
    /// <summary>
    /// Group name
    /// </summary>
    public string GroupName { get; }
    
    /// <summary>
    /// Short group name tag for display (max 4 chars, e.g. "HD", "EZ")
    /// </summary>
    public string ShortGroupName { get; }

    /// <summary>
    /// Map cooldown will override when set to positive integer
    /// </summary>
    public int MapCooldownOverride { get; }

    /// <summary>
    /// Maximum number of nominations allowed from this group. 0 = unlimited.
    /// </summary>
    public int NominationLimit { get; }

    /// <summary>
    /// Search tags inherited by maps in this group
    /// </summary>
    public IReadOnlyList<string> SearchTags { get; }
}