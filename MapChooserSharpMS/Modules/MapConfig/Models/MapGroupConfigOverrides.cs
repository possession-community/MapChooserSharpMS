using System;
using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapConfig.Models;

internal sealed record MapGroupConfigOverrides(
    IMapGroupConfig GroupConfig,
    string OverrideConfigName,
    bool Enabled,
    bool ForceOverride,
    int OverridePriority,
    IReadOnlyCollection<DayOfWeek> TargetDays,
    IReadOnlyCollection<ITimeRange> TargetTimeRanges) : IMapGroupConfigOverrides;
