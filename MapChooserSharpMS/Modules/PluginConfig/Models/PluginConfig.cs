using MapChooserSharpMS.Modules.PluginConfig.Interfaces;

namespace MapChooserSharpMS.Modules.PluginConfig.Models;

internal class PluginConfig(
    IMcsVoteConfig voteConfig,
    IMcsMapCycleConfig mapCycleConfig,
    IMcsGeneralConfig generalConfig,
    IMcsCooldownScopeConfig cooldownScopeConfig)
    : IMcsPluginConfig
{
    public IMcsVoteConfig VoteConfig { get; } = voteConfig;
    public IMcsMapCycleConfig MapCycleConfig { get; } = mapCycleConfig;
    public IMcsGeneralConfig GeneralConfig { get; } = generalConfig;
    public IMcsCooldownScopeConfig CooldownScopeConfig { get; } = cooldownScopeConfig;
}
