using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapConfig.Models;

internal sealed record RandomPickConfig(
    uint MapSelectionWeight,
    bool IsPickable,
    bool BypassNominationRestriction) : IRandomPickConfig;
