using MapChooserSharpMS.Shared;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle;
using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.Nomination;
using MapChooserSharpMS.Shared.RockTheVote;
using MapChooserSharpMS.Shared.Ui.Menu;

namespace MapChooserSharpMS;

internal sealed class McsSharedApi : IMapChooserSharpShared
{
    private readonly MapChooserSharpMs _plugin;

    internal McsSharedApi(
        MapChooserSharpMs plugin,
        IMapCycleController mapCycleController,
        IMapCycleExtendController mapCycleExtendController,
        IMcsNominationController nominationController,
        IMcsMapVoteController mapVoteController,
        IMcsRtvController rtvController,
        IMcsMapConfigProvider mapConfigProvider)
    {
        _plugin = plugin;
        MapCycleController = mapCycleController;
        MapCycleExtendController = mapCycleExtendController;
        McsNominationController = nominationController;
        McsMapVoteController = mapVoteController;
        McsRtvController = rtvController;
        McsMapConfigProvider = mapConfigProvider;
    }

    public IMapCycleController MapCycleController { get; }
    public IMapCycleExtendController MapCycleExtendController { get; }
    public IMcsNominationController McsNominationController { get; }
    public IMcsMapVoteController McsMapVoteController { get; }
    public IMcsRtvController McsRtvController { get; }
    public IMcsMapConfigProvider McsMapConfigProvider { get; }

    public void SetNominationMenuCompat(IMcsNominationMenuCompat menuCompat)
    {
        _plugin.NominationMenuCompat = menuCompat;
        menuCompat.NominationMenuService = McsNominationController.NominationMenuManagementService;
    }

    public void SetVoteMenuCompat(IMcsVoteMenuCompat menuCompat)
    {
        _plugin.VoteMenuCompat = menuCompat;
    }
}
