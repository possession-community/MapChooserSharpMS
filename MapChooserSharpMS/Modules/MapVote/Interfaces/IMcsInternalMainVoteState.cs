using MapChooserSharpMS.Shared.MapVote;

namespace MapChooserSharpMS.Modules.MapVote.Interfaces;

/// <summary>
/// Writable view of the <b>main</b> map-selection vote's state. Not DI-
/// registered — the MapVote module owns the concrete state manager directly
/// and casts to this interface at call sites that need write access. The
/// interface exists so such call sites are type-restricted to main-vote
/// mutations (no accidental extend-vote writes via the same handle).
/// </summary>
internal interface IMcsInternalMainVoteState
{
    void SetState(McsMapVoteState? state);
    void Reset();
}
