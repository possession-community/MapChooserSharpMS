using MapChooserSharpMS.Modules.PluginConfig.Enums;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;

namespace MapChooserSharpMS.Modules.PluginConfig.Models;

internal class McsMapCycleConfig(
    int defaultMaxExtends,
    int fallbackMaxExtCommandUses,
    int fallbackExtendTimePerExtends,
    int fallbackExtendRoundsPerExtends,
    bool shouldStopSourceTvRecording,
    McsMapConfigExecutionType mapConfigExecutionType,
    string mapConfigDirectoryPath,
    bool pauseMapCycleWhenServerEmpty)
    : IMcsMapCycleConfig
{
    public int FallbackDefaultMaxExtends { get; } = defaultMaxExtends;
    public int FallbackMaxExtCommandUses { get; } = fallbackMaxExtCommandUses;
    public bool ShouldStopSourceTvRecording { get; } = shouldStopSourceTvRecording;
    public McsMapConfigExecutionType MapConfigExecutionType { get; } = mapConfigExecutionType;
    public string MapConfigDirectoryPath { get; } = mapConfigDirectoryPath;
    public int FallbackExtendTimePerExtends { get; } = fallbackExtendTimePerExtends;
    public int FallbackExtendRoundsPerExtends { get; } = fallbackExtendRoundsPerExtends;
    public bool PauseMapCycleWhenServerEmpty { get; } = pauseMapCycleWhenServerEmpty;
}
