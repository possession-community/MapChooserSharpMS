using System;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;
using MapChooserSharpMS.Shared.MapCycle.Cooldown;

namespace MapChooserSharpMS.Modules.MapCycle.Cooldown;

/// <summary>
/// Pure field-wise combination rules for cooldown states:
/// the most restrictive value wins per field.
/// </summary>
internal static class McsCooldownAggregator
{
    /// <summary>
    /// Folds <paramref name="other"/> into <paramref name="target"/>.
    /// CurrentCooldown/nomination counts take Max, timed ends take the latest,
    /// LastPlayedAt takes the most recent, UnplayedCount takes Min — a map recently
    /// played anywhere in scope is not "unplayed". States that never saw a play
    /// (LastPlayedAt == MinValue) carry no meaningful UnplayedCount and don't
    /// contribute to it.
    /// </summary>
    public static void CombineInto(McsCooldownStateEntry target, IMcsCooldownState other)
    {
        if (other.LastPlayedAt != DateTime.MinValue)
        {
            target.UnplayedCount = target.LastPlayedAt == DateTime.MinValue
                ? other.UnplayedCount
                : Math.Min(target.UnplayedCount, other.UnplayedCount);
        }

        target.CurrentCooldown = Math.Max(target.CurrentCooldown, other.CurrentCooldown);

        if (other.TimedCooldownEndUtc > target.TimedCooldownEndUtc)
            target.TimedCooldownEndUtc = other.TimedCooldownEndUtc;

        if (other.LastPlayedAt > target.LastPlayedAt)
            target.LastPlayedAt = other.LastPlayedAt;

        target.CurrentNominationCooldown = Math.Max(target.CurrentNominationCooldown, other.CurrentNominationCooldown);

        if (other.NominationTimedCooldownEndUtc > target.NominationTimedCooldownEndUtc)
            target.NominationTimedCooldownEndUtc = other.NominationTimedCooldownEndUtc;
    }

    /// <summary>
    /// Returns a detached snapshot combining both states, leaving inputs untouched.
    /// </summary>
    public static McsCooldownStateEntry Combine(IMcsCooldownState a, IMcsCooldownState b)
    {
        var result = CopyOf(a);
        CombineInto(result, b);
        return result;
    }

    public static McsCooldownStateEntry CopyOf(IMcsCooldownState state)
    {
        return new McsCooldownStateEntry
        {
            CurrentCooldown = state.CurrentCooldown,
            TimedCooldownEndUtc = state.TimedCooldownEndUtc,
            LastPlayedAt = state.LastPlayedAt,
            UnplayedCount = state.UnplayedCount,
            CurrentNominationCooldown = state.CurrentNominationCooldown,
            NominationTimedCooldownEndUtc = state.NominationTimedCooldownEndUtc,
        };
    }

    public static McsCooldownStateEntry FromRecord(CooldownRecord record)
    {
        return new McsCooldownStateEntry
        {
            CurrentCooldown = record.Cooldown,
            TimedCooldownEndUtc = record.TimedCooldownEnd,
            LastPlayedAt = record.LastPlayedAt,
            UnplayedCount = record.UnplayedCount,
            CurrentNominationCooldown = record.NomCooldown,
            NominationTimedCooldownEndUtc = record.NomTimedCooldownEnd,
        };
    }
}
