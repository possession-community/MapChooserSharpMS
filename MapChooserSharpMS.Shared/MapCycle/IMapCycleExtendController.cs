using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.MapCycle;

/// <summary>
/// Public facade for the map-extend system. <br/>
/// Two independent extend budgets exist per map: <br/>
/// - <see cref="ExtendsLeft"/> (map config <c>MaxExtends</c>) — consumed by the
///   map-vote "Extend Map" option. <br/>
/// - <see cref="ExtCommandUsesLeft"/> (map config <c>MaxExtCommandUses</c>) —
///   consumed by player-driven <c>!ext</c> extends. <br/>
/// Admin paths (<see cref="TryExtendCurrentMap"/> and extend votes —
/// <c>!ve</c> is admin-only) consume neither budget.
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
    McsMapExtendResult TryExtendCurrentMap(int? overrideAmount = null);

    /// <summary>
    /// Directly sets the remaining !ext command uses for the current map.
    /// </summary>
    void SetExtCommandUsesLeft(int count);

    /// <summary>
    /// Whether the !ext command is currently accepting participants.
    /// </summary>
    bool IsExtEnabled { get; }

    /// <summary>
    /// Enables the !ext command.
    /// </summary>
    void EnableExt();

    /// <summary>
    /// Disables the !ext command. Existing participants are not cleared.
    /// </summary>
    void DisableExt();

    /// <summary>
    /// Starts a native yes/no extend vote (admin-only entry point). On pass,
    /// the map is extended through the admin path — no extend budget is
    /// consumed.
    /// </summary>
    /// <param name="initiator">Vote initiator. null means console/server.</param>
    /// <param name="overrideAmount">Override extend amount (minutes or rounds). null uses config default.</param>
    McsExtendVoteStartResult StartExtendVote(IGameClient? initiator = null, int? overrideAmount = null);

    /// <summary>
    /// Cancels the in-progress extend vote.
    /// </summary>
    /// <param name="canceller">Who cancelled. null means console/server.</param>
    /// <returns>True when an extend vote was in progress and got cancelled.</returns>
    bool CancelExtendVote(IGameClient? canceller = null);
}
