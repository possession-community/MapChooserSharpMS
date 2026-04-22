using MapChooserSharpMS.Shared.MapVote;

namespace MapChooserSharpMS.Modules.MapVote.Interfaces;

/// <summary>
/// Writable view of the <b>extend</b> vote's state. Not DI-registered — the
/// owning module (MapCycle once it lands) holds a direct reference and casts
/// to this interface at write sites so it can't accidentally mutate the
/// main-vote slot instead.
/// </summary>
internal interface IMcsInternalExtendVoteState
{
    void SetState(McsMapVoteState? state);
    void Reset();
}
