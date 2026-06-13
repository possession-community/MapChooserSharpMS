using System.Collections.Generic;
using Sharp.Shared.Enums;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Modules.RockTheVote;

internal sealed class RtvConVars
{
    public readonly IConVar CommandUnlockTimeNextMapConfirmed;
    public readonly IConVar CommandUnlockTimeMapNotChanged;
    public readonly IConVar CommandUnlockTimeMapExtend;
    public readonly IConVar CommandUnlockTimeMapStart;
    public readonly IConVar VoteStartThreshold;
    public readonly IConVar MapChangeTimingAfterRtvSuccess;
    public readonly IConVar MinimumRequirements;
    public readonly IConVar BroadcastPlayerCast;
    public readonly IConVar ImmediateChangeThreshold;
    public readonly IConVar ThresholdDecayTime;

    public RtvConVars(IConVarManager cvm)
    {
        CommandUnlockTimeNextMapConfirmed = cvm.CreateConVar("mcs_rtv_command_unlock_time_next_map_confirmed", 0.0F, 0.0F, 1200.0F, "Seconds to take unlock RTV command after next map confirmed in vote", ConVarFlags.None)!;
        CommandUnlockTimeMapNotChanged = cvm.CreateConVar("mcs_rtv_command_unlock_time_map_dont_change", 0.0F, 0.0F, 1200.0F, "Seconds to take unlock RTV command after map is not changed in rtv vote", ConVarFlags.None)!;
        CommandUnlockTimeMapExtend = cvm.CreateConVar("mcs_rtv_command_unlock_time_map_extend", 0.0F, 0.0F, 1200.0F, "Seconds to take unlock RTV command after map is extended in vote", ConVarFlags.None)!;
        CommandUnlockTimeMapStart = cvm.CreateConVar("mcs_rtv_command_unlock_time_map_start", 0.0F, 0.0F, 1200.0F, "Seconds to take unlock RTV command after map started", ConVarFlags.None)!;
        VoteStartThreshold = cvm.CreateConVar("mcs_rtv_vote_start_threshold", 0.5F, 0.0F, 1.0F, "How many percent to require start rtv vote?", ConVarFlags.None)!;
        MapChangeTimingAfterRtvSuccess = cvm.CreateConVar("mcs_rtv_map_change_timing", 3.0F, 0.0F, 60.0F, "Seconds to change map after RTV is success. Set 0.0 to change immediately", ConVarFlags.None)!;
        MinimumRequirements = cvm.CreateConVar("mcs_rtv_minimum_requirements", 0, 0, 64, "Minimum RTV requirements to start RTV vote. Set 0 to disable this requirement", ConVarFlags.None)!;
        BroadcastPlayerCast = cvm.CreateConVar("mcs_rtv_broadcast_player_cast", 1, 0, 1, "Broadcast when a player casts RTV vote", ConVarFlags.None)!;
        ImmediateChangeThreshold = cvm.CreateConVar("mcs_rtv_immediate_change_threshold", 0.0F, 0.0F, 1.0F, "Post-vote RTV ratio to trigger immediate map change. 0 = disabled (always round-end)", ConVarFlags.None)!;
        ThresholdDecayTime = cvm.CreateConVar("mcs_rtv_threshold_decay_time", 0.0F, 0.0F, 3600.0F, "Seconds to decay RTV threshold from 100% to configured value. 0 = disabled", ConVarFlags.None)!;
    }

    public IEnumerable<IConVar> All()
    {
        yield return CommandUnlockTimeNextMapConfirmed;
        yield return CommandUnlockTimeMapNotChanged;
        yield return CommandUnlockTimeMapExtend;
        yield return CommandUnlockTimeMapStart;
        yield return VoteStartThreshold;
        yield return MapChangeTimingAfterRtvSuccess;
        yield return MinimumRequirements;
        yield return BroadcastPlayerCast;
        yield return ImmediateChangeThreshold;
        yield return ThresholdDecayTime;
    }
}
