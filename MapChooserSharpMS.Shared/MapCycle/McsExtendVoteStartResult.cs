namespace MapChooserSharpMS.Shared.MapCycle;

/// <summary>
/// Result of an extend vote start attempt.
/// </summary>
public enum McsExtendVoteStartResult
{
    /// <summary>
    /// Extend vote started successfully.
    /// </summary>
    Started = 0,

    /// <summary>
    /// No vote-based extends left (MaxExtends exhausted).
    /// </summary>
    NoExtendsLeft,

    /// <summary>
    /// A map vote / another native vote is in progress, or next map is
    /// already confirmed.
    /// </summary>
    AnotherVoteInProgress,

    /// <summary>
    /// An extend vote is already in progress.
    /// </summary>
    ExtendVoteAlreadyInProgress,

    /// <summary>
    /// There is no active time/round limit to extend
    /// (map cycle mode is none, or the limit manager is not initialized).
    /// </summary>
    TimeLimitNotActive,

    /// <summary>
    /// Failed to initiate the native vote (NativeVoteManager unavailable or refused).
    /// </summary>
    FailedToInitiateNativeVote,
}
