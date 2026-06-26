using MapChooserSharpMS.Modules.PluginConfig.Interfaces;

namespace MapChooserSharpMS.Modules.PluginConfig.Models;

internal class PluginConfig(
    IMcsVoteConfig voteConfig,
    IMcsMapCycleConfig mapCycleConfig,
    IMcsGeneralConfig generalConfig)
    : IMcsPluginConfig
{
    public IMcsVoteConfig VoteConfig { get; } = voteConfig;
    public IMcsMapCycleConfig MapCycleConfig { get; } = mapCycleConfig;
    public IMcsGeneralConfig GeneralConfig { get; } = generalConfig;
}
