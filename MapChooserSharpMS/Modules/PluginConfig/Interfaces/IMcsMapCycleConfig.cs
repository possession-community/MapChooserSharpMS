using MapChooserSharpMS.Modules.PluginConfig.Enums;

namespace MapChooserSharpMS.Modules.PluginConfig.Interfaces;

internal interface IMcsMapCycleConfig
{
    internal int FallbackDefaultMaxExtends { get; }

    internal int FallbackExtendTimePerExtends { get; }

    internal int FallbackExtendRoundsPerExtends { get; }

    internal int FallbackMaxExtCommandUses { get; }

    internal bool ShouldStopSourceTvRecording { get; }

    internal McsMapConfigExecutionType MapConfigExecutionType { get; }

    internal string MapConfigDirectoryPath { get; }

    internal bool PauseMapCycleWhenServerEmpty { get; }
}
