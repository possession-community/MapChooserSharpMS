using Sharp.Shared.Enums;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Modules.Nomination;

internal sealed class NominationConVars
{
    public readonly IConVar BroadcastEnabled;
    public readonly IConVar PlayerCooldown;
    public readonly IConVar PlayerTimedCooldown;
    public readonly IConVar AllowDuringVoteCountdown;

    public NominationConVars(IConVarManager cvm)
    {
        BroadcastEnabled = cvm.CreateConVar("mcs_nomination_broadcast_enabled", 1, 0, 1, "Broadcast nomination messages to all players", ConVarFlags.None)!;
        AllowDuringVoteCountdown = cvm.CreateConVar("mcs_nomination_allow_during_vote_countdown", 1, 0, 1, "Allow nominations during the pre-vote countdown (0 = nominations close when the countdown starts)", ConVarFlags.None)!;
        PlayerCooldown = cvm.CreateConVar("mcs_nomination_player_cooldown", 0, 0, int.MaxValue, "Per-player nomination cooldown in map count (0 = disabled)", ConVarFlags.None)!;
        PlayerTimedCooldown = cvm.CreateConVar("mcs_nomination_player_timed_cooldown", 0f, 0f, float.MaxValue, "Per-player nomination cooldown in seconds (0 = disabled)", ConVarFlags.None)!;
    }
}
