using System;
using System.Collections.Generic;

namespace MapChooserSharpMS.Shared.MapConfig;

public interface IBaseOverrideConfig
{
    /// <summary>
    /// Sentinel value for the base (non-override) config entry.
    /// Empty string is guaranteed safe — TOML disallows empty table names.
    /// </summary>
    const string BaseConfigName = "";

    string OverrideConfigName { get; }
 
    bool Enabled { get; }
    
    bool ForceOverride { get; }
    
    int OverridePriority { get; }
    
    IReadOnlyCollection<DayOfWeek> TargetDays { get; }
    
    IReadOnlyCollection<ITimeRange> TargetTimeRanges { get; }
}