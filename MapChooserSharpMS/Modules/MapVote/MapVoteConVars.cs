using System.Collections.Generic;
using Sharp.Shared.Enums;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Modules.MapVote;

internal sealed class MapVoteConVars
{
    public readonly IConVar ShuffleMenu;
    public readonly IConVar VoteEndTime;
    public readonly IConVar VoteCountdownTime;
    public readonly IConVar RunoffMapPickupThreshold;
    public readonly IConVar WinnerPickupThreshold;
    public readonly IConVar ExcludeSpectators;

    public MapVoteConVars(IConVarManager cvm)
    {
        ShuffleMenu = cvm.CreateConVar("mcs_vote_shuffle_menu", false, "Should vote menu elements is shuffled per player?", ConVarFlags.None)!;
        VoteEndTime = cvm.CreateConVar("mcs_vote_end_time", 15.0F, 5.0F, 120.0F, "How long to take vote ends in seconds?", ConVarFlags.None)!;
        VoteCountdownTime = cvm.CreateConVar("mcs_vote_countdown_time", 13, 0, 120, "How long to take vote starts in seconds", ConVarFlags.None)!;
        RunoffMapPickupThreshold = cvm.CreateConVar("mcs_vote_runoff_map_pickup_threshold", 0.3F, 0.0F, 1.0F, "If there is no vote that higher than mcs_vote_winner_pickup_threshold, then it will pick up maps higher than this percentage for runoff vote", ConVarFlags.None)!;
        WinnerPickupThreshold = cvm.CreateConVar("mcs_vote_winner_pickup_threshold", 0.7F, 0.0F, 1.0F, "If vote is higher than this percent, it will picked up as winner.", ConVarFlags.None)!;
        ExcludeSpectators = cvm.CreateConVar("mcs_vote_exclude_spectators", false, "Should exclude spectators from vote", ConVarFlags.None)!;
    }

    public IEnumerable<IConVar> All()
    {
        yield return ShuffleMenu;
        yield return VoteEndTime;
        yield return VoteCountdownTime;
        yield return RunoffMapPickupThreshold;
        yield return WinnerPickupThreshold;
        yield return ExcludeSpectators;
    }
}
