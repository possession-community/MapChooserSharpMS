using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.MapCycle;

/// <summary>
/// Public facade for the map-extend system. <br/>
/// Two independent extend budgets exist per map: <br/>
/// - <see cref="ExtendsLeft"/> (map config <c>MaxExtends</c>) — consumed by the
///   map-vote "Extend Map" option and by extend votes (<c>!ve</c>). <br/>
/// - <see cref="ExtCommandUsesLeft"/> (map config <c>MaxExtCommandUses</c>) —
///   consumed by player-driven <c>!ext</c> extends.
/// </summary>
public interface IMapCycleExtendController
{
    /// <summary>
    /// Remaining vote-based extends for the current map.
    /// </summary>
    int ExtendsLeft { get; }

    /// <summary>
    /// Remaining !ext command extends for the current map.
    /// </summary>
    int ExtCommandUsesLeft { get; }

    /// <summary>
    /// True while an extend vote (native yes/no vote) is in progress.
    /// </summary>
    bool IsExtendVoteInProgress { get; }

    /// <summary>
    /// Extends the current map by the configured amount
    /// (<c>ExtendTimePerExtends</c> minutes or <c>ExtendRoundsPerExtends</c> rounds,
    /// depending on the active map cycle mode). <br/>
    /// This is the admin/API entry point — it does NOT consume either extend budget.
    /// </summary>
    McsMapExtendResult TryExtendCurrentMap();

    /// <summary>
    /// Starts a native yes/no extend vote. On pass, the map is extended and
    /// <see cref="ExtendsLeft"/> is consumed.
    /// </summary>
    /// <param name="initiator">Vote initiator. null means console/server.</param>
    McsExtendVoteStartResult StartExtendVote(IGameClient? initiator = null);

    /// <summary>
    /// Cancels the in-progress extend vote.
    /// </summary>
    /// <param name="canceller">Who cancelled. null means console/server.</param>
    /// <returns>True when an extend vote was in progress and got cancelled.</returns>
    bool CancelExtendVote(IGameClient? canceller = null);
}
