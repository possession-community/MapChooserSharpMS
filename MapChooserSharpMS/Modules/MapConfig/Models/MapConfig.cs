using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapConfig.Models;

internal sealed record MapConfig(
    string MapName,
    string MapNameAlias,
    string MapDescription,
    long WorkshopId,
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
    IMcsCooldownSettings CooldownSettings,
    IExtraConfigAccessor ExtraConfiguration,
    IReadOnlyList<string> SearchTags,
    bool IsProvisional = false) : IMapConfig;
