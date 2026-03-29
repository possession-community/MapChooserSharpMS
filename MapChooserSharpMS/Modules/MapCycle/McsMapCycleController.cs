using System;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit;
using MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit.Interfaces;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.Events.MapCycle.Params;
using MapChooserSharpMS.Shared.MapCycle;
using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;
using MapChooserSharpMS.Shared.MapCycle.Services;
using Microsoft.Extensions.DependencyInjection;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.MapCycle;

internal sealed class McsMapCycleController(IServiceProvider serviceProvider, bool hotReload)
    : PluginModuleBase(serviceProvider, hotReload), IMapCycleController
{
    public override string PluginModuleName => "McsMapCycleController";
    public override string ModuleChatPrefix => "Prefix.MapCycle";
    protected override bool UseTranslationKeyInModuleChatPrefix => true;

    private IInternalEventManager _eventManager = null!;
    private IInternalTimeLimitManager _internalTimeLimitManager = null!;
    private TimeLimitTransitionTracker _transitionTracker = null!;

    public ITimeLimitManager CurrentMapTimeLimitManager => (ITimeLimitManager)_internalTimeLimitManager;

    // TODO Implement MapCooldown services
    public IMapCooldownQueryService MapCooldownQueryService => throw new NotImplementedException();
    public IMapCooldownCommandService MapCooldownCommandService => throw new NotImplementedException();

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IMapCycleController>(this);
    }

    protected override void OnInitialize()
    {
        _eventManager = ServiceProvider.GetRequiredService<IInternalEventManager>();
    }

    protected override void OnAllModulesLoaded()
    {
    }

    protected override void OnUnloadModule()
    {
    }

    public void InstallEventListener(IMapCycleEventListener listener)
    {
        _eventManager.RegisterListener(listener);
    }

    public void RemoveEventListener(IMapCycleEventListener listener)
    {
        _eventManager.RemoveListener(listener);
    }

    /// <summary>
    /// Initializes a time-based time limit for the current map.
    /// </summary>
    public void InitializeTimeBasedLimit(TimeSpan timeLimit, TimeSpan voteStartThreshold)
    {
        var clock = new SystemTimeLimitClock();
        var manager = new TimeBasedTimeLimitManager(timeLimit, clock);
        _internalTimeLimitManager = manager;

        _transitionTracker = new TimeLimitTransitionTracker(
            isVoteThresholdReached: () => manager.TimeLeft <= voteStartThreshold,
            isLimitReached: () => manager.IsLimitReached);
    }

    /// <summary>
    /// Initializes a round-based time limit for the current map.
    /// </summary>
    public void InitializeRoundBasedLimit(int roundLimit, int voteStartThreshold)
    {
        var manager = new RoundsTimeLimitManager(roundLimit);
        _internalTimeLimitManager = manager;

        _transitionTracker = new TimeLimitTransitionTracker(
            isVoteThresholdReached: () => manager.RoundsLeft <= voteStartThreshold,
            isLimitReached: () => manager.IsLimitReached);
    }

    /// <summary>
    /// Called every second by the server timer for time-based limits.
    /// </summary>
    public void OnTimerTick()
    {
        _internalTimeLimitManager.OnTick();
        FireTransitions();
    }

    /// <summary>
    /// Called at the end of each round for round-based limits.
    /// </summary>
    public void OnRoundEnd()
    {
        _internalTimeLimitManager.OnTick();
        FireTransitions();
    }

    /// <summary>
    /// Called after Extend/Set to re-evaluate transition flags.
    /// </summary>
    public void OnTimeLimitChanged()
    {
        _transitionTracker.ResetFlags();
    }

    private void FireTransitions()
    {
        var transitions = _transitionTracker.CheckTransitions();

        foreach (var transition in transitions)
        {
            switch (transition)
            {
                case TimeLimitTransitionState.VoteStartThresholdReached:
                    _eventManager.Fire<IMapCycleEventListener>(
                        l => l.OnVoteStartThresholdReached(
                            new EventManager.Events.MapCycle.VoteStartThresholdReachedParams(
                                Plugin, this, CurrentMapTimeLimitManager.TimeLimitType)));
                    break;

                case TimeLimitTransitionState.LimitReached:
                    _eventManager.Fire<IMapCycleEventListener>(
                        l => l.OnTimeLimitReached(
                            new EventManager.Events.MapCycle.TimeLimitReachedParams(
                                Plugin, this, CurrentMapTimeLimitManager.TimeLimitType)));
                    break;
            }
        }
    }
}
