using System;
using System.Collections.Generic;

namespace MapChooserSharpMS.Shared.MapConfig;

/// <summary>
/// Nomination related settings
/// </summary>
public interface INominationConfig
{
    /// <summary>
    /// When player count is exceed this value, normal user will not able to nominate this map.
    /// </summary>
    public int MaxPlayers { get; }
    
    /// <summary>
    /// When player count is not exceed this value, normal user will not able to nominate this map.
    /// </summary>
    public int MinPlayers { get; }
    
    /// <summary>
    /// When this value set to true, only root user and console can nominate.
    /// </summary>
    public bool ProhibitAdminNomination { get; }
    
    /// <summary>
    /// Days when nomination is allowed
    /// </summary>
    public IReadOnlyList<DayOfWeek> DaysAllowed { get; }

    /// <summary>
    /// Time ranges when nomination is allowed
    /// </summary>
    public IReadOnlyList<ITimeRange> AllowedTimeRanges { get; }

    /// <summary>
    /// When true, only players with allow permission nodes can nominate this map.
    /// </summary>
    public bool RestrictToAllowedUsersOnly { get; }
}