using System.Collections.Generic;
using MapChooserSharpMS.Shared.Events.Nomination;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.Nomination.Managers;
using MapChooserSharpMS.Shared.Nomination.Services;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Nomination;

/// <summary>
/// API for nomination module
/// </summary>
public interface IMcsNominationController
{
    /// <summary>
    /// Nomination Service
    /// </summary>
    IMapNominationService NominationService { get; }
    
    /// <summary>
    /// Validation service
    /// </summary>
    INominationValidateService NominationValidateService { get; }
    
    /// <summary>
    /// Nomination menu management service
    /// </summary>
    INominationMenuManagementService NominationMenuManagementService { get; }
    
    /// <summary>
    /// Nomination Manager
    /// </summary>
    INominationManager NominationManager { get; }
    
    /// <summary>
    /// Installs event listener
    /// </summary>
    void InstallEventListener(INominationEventListener listener);

    /// <summary>
    /// Remove event listener
    /// </summary>
    void RemoveEventListener(INominationEventListener listener);
}