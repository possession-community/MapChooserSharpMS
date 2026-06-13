using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapConfig.Models;

internal sealed record MapConfig(
    string MapName,
    string MapNameAlias,
    string MapDescription,
    long WorkshopId,
    string Tag,
    string I18nTag,
    List<IMapGroupConfig> GroupSettings,
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
    IExtraConfigAccessor ExtraConfiguration) : IMapConfig;
