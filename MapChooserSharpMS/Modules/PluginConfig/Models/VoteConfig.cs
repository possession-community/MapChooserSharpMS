using MapChooserSharp.Modules.MapVote.Countdown;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;

namespace MapChooserSharpMS.Modules.PluginConfig.Models;

internal class VoteConfig(
    int maxMenuElements,
    bool shouldPrintVoteToChat,
    bool shouldPrintVoteRemainingTime,
    IMcsVoteSoundConfig voteSoundConfig,
    McsCountdownUiType currentCountdownUiType)
    : IMcsVoteConfig
{
    public McsCountdownUiType CurrentCountdownUiType { get; } = currentCountdownUiType;
    public int MaxMenuElements { get; } = maxMenuElements;
    public bool ShouldPrintVoteToChat { get; } = shouldPrintVoteToChat;
    public bool ShouldPrintVoteRemainingTime { get; } = shouldPrintVoteRemainingTime;
    public IMcsVoteSoundConfig VoteSoundConfig { get; } = voteSoundConfig;
}
