namespace MapChooserSharpMS.Shared.MapCycle;

/// <summary>
/// Result of a map extend attempt.
/// </summary>
public enum McsMapExtendResult
{
    /// <summary>
    /// Map extended successfully.
    /// </summary>
    Extended = 0,

    /// <summary>
    /// No vote-based extends left (MaxExtends exhausted).
    /// </summary>
    NoExtendsLeft,

    /// <summary>
    /// No !ext command extends left (MaxExtCommandUses exhausted).
    /// </summary>
    NoExtCommandUsesLeft,

    /// <summary>
    /// There is no active time/round limit to extend
    /// (map cycle mode is none, or the limit manager is not initialized).
    /// </summary>
    TimeLimitNotActive,
}
