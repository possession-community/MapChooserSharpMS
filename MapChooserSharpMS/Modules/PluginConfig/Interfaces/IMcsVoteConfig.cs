using System.Collections.Generic;
using MapChooserSharp.Modules.MapVote.Countdown;
using MapChooserSharpMS.Modules.Ui.Menu;

namespace MapChooserSharpMS.Modules.PluginConfig.Interfaces;

internal interface IMcsVoteConfig
{
    internal List<McsSupportedMenuType> AvailableMenuTypes { get; }

    internal McsSupportedMenuType CurrentMenuType { get; }

    internal McsCountdownUiType CurrentCountdownUiType { get; }

    internal int MaxMenuElements { get; }

    internal bool ShouldPrintVoteToChat { get; }

    internal bool ShouldPrintVoteRemainingTime { get; }

    internal IMcsVoteSoundConfig VoteSoundConfig { get; }
}
