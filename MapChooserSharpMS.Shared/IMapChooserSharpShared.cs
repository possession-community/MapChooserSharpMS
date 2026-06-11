using MapChooserSharpMS.Shared.Events;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle;
using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.Nomination;
using MapChooserSharpMS.Shared.RockTheVote;
using MapChooserSharpMS.Shared.Ui.Menu;

namespace MapChooserSharpMS.Shared;

/// <summary>
/// API for MapChooserSharp
/// </summary>
public interface IMapChooserSharpShared
{
    /// <summary>
    /// ModSharp module identity
    /// </summary>
    static readonly string ModSharpModuleIdentity = typeof(IMapChooserSharpShared).Assembly.GetName().FullName;
    
    /// <summary>
    /// MapCycleController API, You can manipulate map cycle system
    /// (time limit, map transition, cooldown services)
    /// </summary>
    IMapCycleController MapCycleController { get; }

    /// <summary>
    /// MapCycleExtendController API, You can manipulate map extend system
    /// (direct extends and extend votes)
    /// </summary>
    IMapCycleExtendController MapCycleExtendController { get; }
    
    /// <summary>
    /// Nomination API, You can manipulate nomination system
    /// </summary>
    IMcsNominationController McsNominationController { get; }
    
    /// <summary>
    /// VoteController API, You can manipulate vote system
    /// </summary>
    IMcsMapVoteController McsMapVoteController { get; }
    
    /// <summary>
    /// RTVController API, You can manipulate RTV system
    /// </summary>
    IMcsRtvController McsRtvController { get; }
    
    /// <summary>
    /// MapConfigProvider API, You can manipulate map config
    /// </summary>
    IMcsMapConfigProvider McsMapConfigProvider { get; }

    /// <summary>
    /// Register the default menu compat adapter. Intended to be called once,
    /// during <c>OnAllModulesLoaded</c> of a companion compat module
    /// (see <c>McsFPMCompat</c>). Calling again replaces the previous adapter.
    /// The registered instance is consumed internally by MCS — it is not exposed
    /// back to external callers. MCS menu flows will throw
    /// <see cref="System.InvalidOperationException"/> if no compat has been
    /// registered by the time a menu is requested.
    /// </summary>
    void SetDefaultMenuCompat(IMcsMenuCompat menuCompat);
}