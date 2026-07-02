using MapChooserSharpMS.Modules.MapCycle.Services;
using NativeVoteManagerMS.Shared;
using NativeVoteManagerMS.Shared.Types;

namespace MapChooserSharpMS.Modules.MapCycle.Handlers;

/// <summary>
/// NVM yes/no vote handler for the extend vote. Pure relay — all decisions
/// live in <see cref="McsExtendVoteService"/>, guarded by the generation
/// captured at vote start so stale callbacks are ignored.
/// </summary>
internal sealed class ExtendVoteNativeHandler(McsExtendVoteService service, int generation)
    : IYesNoVoteHandler
{
    public void OnVotePassed(VoteResult result)
        => service.HandleVotePassed(generation, result);

    public void OnVoteFailed(VoteResult result)
        => service.HandleVoteFailed(generation, result);

    public void OnVoteCancelled()
        => service.HandleVoteCancelled(generation);
}
