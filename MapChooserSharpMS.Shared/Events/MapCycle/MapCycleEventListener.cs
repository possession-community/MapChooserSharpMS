using MapChooserSharpMS.Shared.Events.MapCycle.Params;

namespace MapChooserSharpMS.Shared.Events.MapCycle;

public interface IMapCycleEventListener : IEventListenerBase
{
    /// <summary>
    /// Fired when the !ext command is executed.
    /// Return <see cref="McsCancellableEvent.Stop"/> to cancel the command.
    /// </summary>
    McsCancellableEvent OnExtCommandExecute(IExtCommandExecuteEventParams @params)
        => McsCancellableEvent.Continue;
    
    /// <summary>
    /// Fired when map info command is fully executed (you can add more information for !mapinfo command by this)
    /// </summary>
    /// <param name="params"></param>
    void OnMapInfoCommandExecuted(IMapInfoCommandExecutedParams @params) {}

    /// <summary>
    /// Fired when extend vote start
    /// </summary>
    /// <param name="params"></param>
    void OnExtendVoteStarted(IExtendVoteStartedEventParams @params) {}
    
    /// <summary>
    /// Fired when extend vote cancelled
    /// </summary>
    /// <param name="params"></param>
    void OnExtendVoteCancelled(IExtendVoteCancelledEventParams @params) {}

    /// <summary>
    /// Fired when extend vote concluded (passed or failed)
    /// </summary>
    /// <param name="params"></param>
    void OnExtendVoteFinished(IExtendVoteFinishedEventParams @params) {}
    
    
    /// <summary>
    /// FIred when next map is confirmed
    /// </summary>
    /// <param name="params"></param>
    void OnNextMapConfirmed(INextMapConfirmedEventParams @params) {}
    
    /// <summary>
    /// Fired when next map is removed
    /// </summary>
    /// <param name="params"></param>
    void OnNextMapRemoved(INextMapRemovedEventParams @params) {}
    
    
    /// <summary>
    /// Fired when going to intermission state
    /// </summary>
    void OnMcsIntermission(IMcsIntermissionParams @params) {}
    
    /// <summary>
    /// Fired when MCS is about to apply map cooldown. The params are editable —
    /// listeners can modify <see cref="IMapCooldownApplyEventParams.Cooldown"/>,
    /// <see cref="IMapCooldownApplyEventParams.TimedCooldownDuration"/>,
    /// or set <see cref="IMapCooldownApplyEventParams.IsCancelled"/> to suppress.
    /// </summary>
    void OnMapCooldownApply(IMapCooldownApplyEventParams eventParams) {}

    /// <summary>
    /// Fired when time or round limit has been reached
    /// </summary>
    void OnTimeLimitReached(ITimeLimitReachedEventParams @params) {}

    /// <summary>
    /// Fired when remaining time or rounds cross the vote-start threshold
    /// </summary>
    void OnVoteStartThresholdReached(IVoteStartThresholdReachedEventParams @params) {}
}