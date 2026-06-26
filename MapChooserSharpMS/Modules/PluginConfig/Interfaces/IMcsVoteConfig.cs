using MapChooserSharp.Modules.MapVote.Countdown;

namespace MapChooserSharpMS.Modules.PluginConfig.Interfaces;

internal interface IMcsVoteConfig
{
    internal McsCountdownUiType CurrentCountdownUiType { get; }

    internal int MaxMenuElements { get; }

    internal bool ShouldPrintVoteToChat { get; }

    internal bool ShouldPrintVoteRemainingTime { get; }

    internal IMcsVoteSoundConfig VoteSoundConfig { get; }
}
