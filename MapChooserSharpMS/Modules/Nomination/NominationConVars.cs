using Sharp.Shared.Enums;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Modules.Nomination;

internal sealed class NominationConVars
{
    public readonly IConVar BroadcastEnabled;

    public NominationConVars(IConVarManager cvm)
    {
        BroadcastEnabled = cvm.CreateConVar("mcs_nomination_broadcast_enabled", 1, 0, 1, "Broadcast nomination messages to all players", ConVarFlags.None)!;
    }
}
