using MapChooserSharpMS.Shared.Events.Nomination.Params;
using MapChooserSharpMS.Shared.Ui.Menu;

namespace MapChooserSharpMS.Shared.Events.Nomination;

public interface INominationEventListener: IEventListenerBase
{
    /// <summary>
    /// Fired during nomination validation, before OnNomination / OnAdminNomination.
    /// Return <see cref="McsCancellableEvent.Stop"/> to fail the nomination check.
    /// </summary>
    McsCancellableEvent OnNominationCheckPassed(INominationCheckPassedEventParams @params)
        => McsCancellableEvent.Continue;

    /// <summary>
    /// Fired when a player nomination is committed (after validation + OnNominationCheckPassed).
    /// </summary>
    void OnNomination(INominationParams @params) {}

    /// <summary>
    /// Fired when an admin nominates a map.
    /// Return <see cref="McsCancellableEvent.Stop"/> to cancel the nomination.
    /// </summary>
    McsCancellableEvent OnAdminNomination(IAdminNominationParams @params)
        => McsCancellableEvent.Continue;
    
    void OnNominationChanged(INominationChangeParams @params) {}

    void OnNominationRemoved(INominationRemovedParams @params) {}

    /// <summary>
    /// Fired when a specific client is removed from a nomination's participant
    /// list (voluntary un-nomination or disconnect cleanup). This fires per
    /// client — if the last participant leaves a non-admin nomination, an
    /// additional <see cref="OnNominationRemoved"/> will fire for the whole
    /// entry.
    /// </summary>
    void OnUnNominate(IUnNominateParams @params) {}

    /// <summary>
    /// Fired when a nomination detail menu is about to open.
    /// Add extra <see cref="McsMenuItem"/> via <see cref="INominationMenuDetailsOpeningParams.ExtraItems"/>.
    /// </summary>
    void OnNominationMenuDetailsOpening(INominationMenuDetailsOpeningParams @params) {}
}