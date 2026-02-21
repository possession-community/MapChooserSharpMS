using MapChooserSharpMS.Modules.PluginConfig.Interfaces;

namespace MapChooserSharpMS.Modules.PluginConfig.Models;

internal class VoteSoundConfig : IMcsVoteSoundConfig
{
    public VoteSoundConfig(string vSndEvtsSoundFilePath, IMcsVoteSound voteCountdownSounds, IMcsVoteSound runoffVoteCountdownSounds)
    {
        if (!vSndEvtsSoundFilePath.EndsWith(".vsndevts"))
        {
            VSndEvtsSoundFilePath = string.Empty;
        }
        else
        {
            VSndEvtsSoundFilePath = vSndEvtsSoundFilePath;
        }

        InitialVoteSounds = voteCountdownSounds;
        RunoffVoteSounds = runoffVoteCountdownSounds;
    }

    public string VSndEvtsSoundFilePath { get; }
    public IMcsVoteSound InitialVoteSounds { get; }
    public IMcsVoteSound RunoffVoteSounds { get; }
}
