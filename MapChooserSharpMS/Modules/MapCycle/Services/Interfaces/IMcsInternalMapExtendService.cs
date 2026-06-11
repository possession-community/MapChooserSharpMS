using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle;

namespace MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;

/// <summary>
/// What triggered a map extend. Determines which extend budget is consumed.
/// </summary>
internal enum McsExtendTrigger
{
    /// <summary>
    /// "Extend Map" option won the map vote. Consumes <c>MaxExtends</c>.
    /// </summary>
    MapVote,

    /// <summary>
    /// Extend vote (!ve) passed. Consumes <c>MaxExtends</c>.
    /// </summary>
    ExtendVote,

    /// <summary>
    /// !ext participation threshold reached. Consumes <c>MaxExtCommandUses</c>.
    /// </summary>
    ExtCommand,

    /// <summary>
    /// Admin command or external API call. Consumes nothing.
    /// </summary>
    AdminOrApi,
}

/// <summary>
/// Internal surface of the map extend service. Owns the two per-map extend
/// budgets and is the single execution path for extending the current map's
/// time/round limit (internal manager only — game ConVars are not written).
/// </summary>
internal interface IMcsInternalMapExtendService
{
    /// <summary>
    /// Remaining vote-based extends (MaxExtends).
    /// </summary>
    int ExtendsLeft { get; }

    /// <summary>
    /// Remaining !ext command extends (MaxExtCommandUses).
    /// </summary>
    int ExtCommandUsesLeft { get; }

    /// <summary>
    /// True when there is an active time/round limit that can be extended.
    /// False when the map cycle mode is none or the limit manager is not
    /// initialized — extends would silently no-op, so entry points should
    /// reject early.
    /// </summary>
    bool CanExtendNow { get; }

    /// <summary>
    /// Extends the current map by the configured amount, consuming the budget
    /// matching <paramref name="trigger"/>. Fires
    /// <see cref="MapChooserSharpMS.Shared.Events.MapVote.IMapVoteEventListener.OnMapExtended"/>
    /// with the real amount/type on success.
    /// </summary>
    McsMapExtendResult TryExtend(McsExtendTrigger trigger);

    /// <summary>
    /// Loads budgets and per-extend amounts for the new map. Falls back to
    /// plugin config values when <paramref name="mapConfig"/> is null.
    /// </summary>
    void InitializeForCurrentMap(IMapConfig? mapConfig);

    /// <summary>
    /// Clears budgets on map deactivation.
    /// </summary>
    void ClearState();
}
