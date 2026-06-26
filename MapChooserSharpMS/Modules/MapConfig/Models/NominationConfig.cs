using System;
using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapConfig.Models;

internal sealed record NominationConfig(
    int MaxPlayers,
    int MinPlayers,
    bool ProhibitAdminNomination,
    IReadOnlyList<DayOfWeek> DaysAllowed,
    IReadOnlyList<ITimeRange> AllowedTimeRanges,
    bool RestrictToAllowedUsersOnly = false) : INominationConfig;
