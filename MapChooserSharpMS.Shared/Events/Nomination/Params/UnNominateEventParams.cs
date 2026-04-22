using MapChooserSharpMS.Shared.Nomination;

namespace MapChooserSharpMS.Shared.Events.Nomination.Params;

/// <summary>
/// Fired when a client is removed from a nomination's participant list via
/// <c>TryUnNominate</c>. The parent nomination entry may or may not be
/// removed as a side effect (see <see cref="OnNominationRemoved"/>).
/// </summary>
public interface IUnNominateParams : IEventBaseParams, IMcsNominationEventBaseParams
{
    /// <summary>
    /// Slot of the client whose participation was removed. Always valid even
    /// when <see cref="IMcsNominationEventBaseParams.Client"/> is null
    /// (e.g. the player has already disconnected).
    /// </summary>
    int Slot { get; }

    /// <summary>
    /// Why this un-nomination happened.
    /// </summary>
    UnNominateReason Reason { get; }
}
