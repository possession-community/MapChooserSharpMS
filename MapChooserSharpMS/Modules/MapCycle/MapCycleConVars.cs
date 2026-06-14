using System.Collections.Generic;
using Sharp.Shared.Enums;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Modules.MapCycle;

internal sealed class MapCycleConVars
{
    public readonly IConVar Mode;
    public readonly IConVar VoteStartTimeThresholdSeconds;
    public readonly IConVar VoteStartRoundThreshold;
    public readonly IConVar ExtUserVoteThreshold;
    public readonly IConVar VoteExtendSuccessThreshold;
    public readonly IConVar VoteExtendVoteTime;
    public readonly IConVar TransitionRetryAttempts;
    public readonly IConVar TransitionRetryInterval;
    public readonly IConVar TransitionFallbackMap;
    public readonly IConVar TransitionDelay;

    public MapCycleConVars(IConVarManager cvm)
    {
        Mode = cvm.CreateConVar(
            "mcs_mapcycle_mode", "time",
            "MapCycle mode. One of: none, time, round",
            ConVarFlags.None)!;

        VoteStartTimeThresholdSeconds = cvm.CreateConVar(
            "mcs_mapcycle_vote_start_time_threshold", 180, 0, 3600,
            "Seconds before time limit at which the map vote should start (time-based mode)",
            ConVarFlags.None)!;

        VoteStartRoundThreshold = cvm.CreateConVar(
            "mcs_mapcycle_vote_start_round_threshold", 3, 0, 120,
            "Rounds remaining at which the map vote should start (round-based mode)",
            ConVarFlags.None)!;

        ExtUserVoteThreshold = cvm.CreateConVar(
            "mcs_ext_user_vote_threshold", 0.5F, 0.0F, 1.0F,
            "Ratio of real players required for !ext to extend the map",
            ConVarFlags.None)!;

        VoteExtendSuccessThreshold = cvm.CreateConVar(
            "mcs_vote_extend_success_threshold", 0.5F, 0.0F, 1.0F,
            "Ratio of yes votes required for an extend vote to pass",
            ConVarFlags.None)!;

        VoteExtendVoteTime = cvm.CreateConVar(
            "mcs_vote_extend_vote_time", 15.0F, 10.0F, 60.0F,
            "How long the extend vote lasts in seconds",
            ConVarFlags.None)!;

        TransitionRetryAttempts = cvm.CreateConVar(
            "mcs_map_transition_retry_attempts", 3, 1, 10,
            "How many times to retry the map change when the map did not change",
            ConVarFlags.None)!;

        TransitionRetryInterval = cvm.CreateConVar(
            "mcs_map_transition_retry_interval", 30.0F, 5.0F, 300.0F,
            "Seconds to wait between map change retries",
            ConVarFlags.None)!;

        TransitionFallbackMap = cvm.CreateConVar(
            "mcs_map_transition_fallback_map", "de_dust2",
            "Map to change to when all map change retries failed",
            ConVarFlags.None)!;

        TransitionDelay = cvm.CreateConVar(
            "mcs_map_transition_delay", 0.0F, 0.0F, 60.0F,
            "Seconds to wait after round end before changing map. 0 = immediate",
            ConVarFlags.None)!;
    }

    public IEnumerable<IConVar> All()
    {
        yield return Mode;
        yield return VoteStartTimeThresholdSeconds;
        yield return VoteStartRoundThreshold;
        yield return ExtUserVoteThreshold;
        yield return VoteExtendSuccessThreshold;
        yield return VoteExtendVoteTime;
        yield return TransitionRetryAttempts;
        yield return TransitionRetryInterval;
        yield return TransitionFallbackMap;
        yield return TransitionDelay;
    }
}
