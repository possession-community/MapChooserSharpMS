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
    }

    public IEnumerable<IConVar> All()
    {
        yield return Mode;
        yield return VoteStartTimeThresholdSeconds;
        yield return VoteStartRoundThreshold;
    }
}
