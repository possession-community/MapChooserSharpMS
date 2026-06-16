using MapChooserSharpMS.Shared.Events.Nomination.Params;
using MapChooserSharpMS.Shared.Ui.Menu;

namespace MapChooserSharpMS.Shared.Events.Nomination;

public interface INominationEventListener: IEventListenerBase
{
    /// <summary>
    /// You can add additional nomination validation in here <br/>
    /// This event will fire before OnNomination and OnAdminNomination <br/>
    /// If return true, nomination check will fail <br/>
    /// </summary>
    /// <param name="params"></param>
    /// <returns></returns>
    bool OnNominationCheckPassed(INominationCheckPassedEventParams @params)
        => false;
    
    /// <summary>
    /// If return true, nomination will be cancelled
    /// </summary>
    bool OnNomination(INominationParams @params)
        => false;
    
    /// <summary>
    /// If return true, nomination will be cancelled
    /// </summary>
    bool OnAdminNomination(IAdminNominationParams @params)
        => false;
    
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