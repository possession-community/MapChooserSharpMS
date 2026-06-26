using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.MapVote.Managers;
using MapChooserSharpMS.Shared.MapVote.Services;

namespace MapChooserSharpMS.Modules.MapVote.Interfaces;

/// <summary>
/// Internal MapVote controller surface — exposes managers / services whose
/// APIs can mutate vote state. These are kept off <see cref="IMcsMapVoteController"/>
/// (the public surface) so external consumers can't reach them.
/// </summary>
internal interface IMcsInternalVoteController : IMcsMapVoteController
{
    IVoteControllingManager MapVoteManager { get; }

    IClientVoteHandlingService ClientVoteHandlingService { get; }
}