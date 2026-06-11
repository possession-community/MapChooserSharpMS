using MapChooserSharpMS.Shared;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycleController;
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
        IMapCycleControllerApi mapCycleController,
        IMapCycleExtendControllerApi mapCycleExtendController,
        IMapCycleExtendVoteControllerApi mapCycleExtendVoteController,
        IMcsNominationController nominationController,
        IMcsMapVoteController mapVoteController,
        IMcsRtvController rtvController,
        IMcsMapConfigProvider mapConfigProvider)
    {
        _plugin = plugin;
        MapCycleController = mapCycleController;
        MapCycleExtendController = mapCycleExtendController;
        MapCycleExtendVoteController = mapCycleExtendVoteController;
        McsNominationController = nominationController;
        McsMapVoteController = mapVoteController;
        McsRtvController = rtvController;
        McsMapConfigProvider = mapConfigProvider;
    }

    public IMapCycleControllerApi MapCycleController { get; }
    public IMapCycleExtendControllerApi MapCycleExtendController { get; }
    public IMapCycleExtendVoteControllerApi MapCycleExtendVoteController { get; }
    public IMcsNominationController McsNominationController { get; }
    public IMcsMapVoteController McsMapVoteController { get; }
    public IMcsRtvController McsRtvController { get; }
    public IMcsMapConfigProvider McsMapConfigProvider { get; }

    public void SetDefaultMenuCompat(IMcsMenuCompat menuCompat)
    {
        _plugin.MenuCompat = menuCompat;
    }
}
