using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapConfig.Models;

internal sealed record MapGroupConfig(
    string GroupName,
    string ShortGroupName,
    int MapCooldownOverride,
    int NominationLimit,
    bool IsDisabled,
    int MaxExtends,
    int MaxExtCommandUses,
    int MapTime,
    int ExtendTimePerExtends,
    int MapRounds,
    int ExtendRoundsPerExtends,
    IRandomPickConfig RandomPickConfig,
    INominationConfig NominationConfig,
    ICooldownConfig CooldownConfig,
    IExtraConfigAccessor ExtraConfiguration,
    IReadOnlyList<string> SearchTags) : IMapGroupConfig;
