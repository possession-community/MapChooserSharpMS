using System;
using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapConfig.Models;

internal sealed record NominationConfig(
    IReadOnlyList<string> RequiredPermissions,
    bool RestrictToAllowedUsersOnly,
    IReadOnlyList<uint> AllowedSteamIds,
    IReadOnlyList<uint> DisallowedSteamIds,
    int MaxPlayers,
    int MinPlayers,
    bool ProhibitAdminNomination,
    IReadOnlyList<DayOfWeek> DaysAllowed,
    IReadOnlyList<ITimeRange> AllowedTimeRanges) : INominationConfig;
