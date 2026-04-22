using System;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit;
using MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit.Interfaces;
using MapChooserSharpMS.Modules.MapVote.Interfaces;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.MapCycle;
using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;
using MapChooserSharpMS.Shared.MapCycle.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.MapCycle;

internal sealed class McsMapCycleController : PluginModuleBase, IMapCycleController, IGameListener, IEventListener
{
    public override string PluginModuleName => "McsMapCycleController";
    public override string ModuleChatPrefix => "Prefix.MapCycle";
    protected override bool UseTranslationKeyInModuleChatPrefix => true;

    private IInternalEventManager _eventManager = null!;
    private MapCycleConVars _conVars = null!;

    // Writer for the extend-vote slot of the plugin-owned state manager.
    // Received via ctor already narrowed to <see cref="IMcsInternalExtendVoteState"/>
    // — this controller cannot touch the main-vote slot even by accident.
    private readonly IMcsInternalExtendVoteState _voteState;

    private IInternalTimeLimitManager? _internalTimeLimitManager;
    private TimeLimitTransitionTracker? _transitionTracker;

    private MapCycleMode _mode = MapCycleMode.None;
    private Guid _tickTimerId = Guid.Empty;

    public ITimeLimitManager CurrentMapTimeLimitManager =>
        (ITimeLimitManager?)_internalTimeLimitManager
        ?? throw new InvalidOperationException("TimeLimit manager is not initialized for the current map");

    // TODO Implement MapCooldown services
    public IMapCooldownQueryService MapCooldownQueryService => throw new NotImplementedException();
    public IMapCooldownCommandService MapCooldownCommandService => throw new NotImplementedException();

    public int ListenerVersion => 1;
    public int ListenerPriority => 0;

    internal McsMapCycleController(
        IServiceProvider serviceProvider,
        bool hotReload,
        IMcsInternalExtendVoteState voteState) : base(serviceProvider, hotReload)
    {
        _voteState = voteState;
        _conVars = new MapCycleConVars(Plugin.SharedSystem.GetConVarManager());
        foreach (var cv in _conVars.All()) TrackConVar(cv);
    }

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IMapCycleController>(this);
    }

    protected override void OnInitialize()
    {
        _eventManager = ServiceProvider.GetRequiredService<IInternalEventManager>();

        SharedSystem.GetModSharp().InstallGameListener(this);

        var em = SharedSystem.GetEventManager();
        em.InstallEventListener(this);
        em.HookEvent("round_start");
        em.HookEvent("round_end");

        if (HotReload)
        {
            InitializeForCurrentMap();
        }
    }

    protected override void OnAllModulesLoaded()
    {
    }

    protected override void OnUnloadModule()
    {
        TearDownCurrentMap();

        SharedSystem.GetModSharp().RemoveGameListener(this);
        SharedSystem.GetEventManager().RemoveEventListener(this);
    }

    public void InstallEventListener(IMapCycleEventListener listener)
    {
        _eventManager.RegisterListener(listener);
    }

    public void RemoveEventListener(IMapCycleEventListener listener)
    {
        _eventManager.RemoveListener(listener);
    }

    #region IGameListener

    public void OnGameActivate()
    {
        InitializeForCurrentMap();
    }

    public void OnGameDeactivate()
    {
        TearDownCurrentMap();
    }

    #endregion

    #region IEventListener

    public void FireGameEvent(IGameEvent @event)
    {
        if (_mode != MapCycleMode.Round)
            return;

        switch (@event.Name)
        {
            case "round_start":
                OnRoundStart();
                break;
            case "round_end":
                OnRoundEnd();
                break;
        }
    }

    #endregion

    #region Lifecycle

    private void InitializeForCurrentMap()
    {
        TearDownCurrentMap();

        var mode = ParseMode(_conVars.Mode.GetString());
        var cvm = SharedSystem.GetConVarManager();

        switch (mode)
        {
            case MapCycleMode.Round:
            {
                int maxRounds = cvm.FindConVar("mp_maxrounds")?.GetInt32() ?? 0;
                int roundThreshold = _conVars.VoteStartRoundThreshold.GetInt32();
                InitializeRoundBasedLimit(maxRounds, roundThreshold);
                _mode = MapCycleMode.Round;
                Logger.LogInformation(
                    "[MapCycle] Round-based mode: maxRounds={Max}, voteStartThreshold={Threshold}",
                    maxRounds, roundThreshold);
                break;
            }
            case MapCycleMode.Time:
            {
                float timeLimitMinutes = cvm.FindConVar("mp_timelimit")?.GetFloat() ?? 0f;
                var timeLimit = TimeSpan.FromMinutes(timeLimitMinutes);
                var voteThreshold = TimeSpan.FromSeconds(_conVars.VoteStartTimeThresholdSeconds.GetInt32());
                InitializeTimeBasedLimit(timeLimit, voteThreshold);
                _mode = MapCycleMode.Time;
                Logger.LogInformation(
                    "[MapCycle] Time-based mode: timeLimit={Limit}, voteStartThreshold={Threshold}",
                    timeLimit, voteThreshold);
                break;
            }
            default:
                _mode = MapCycleMode.None;
                Logger.LogInformation("[MapCycle] Mode=None; skipping TimeLimit init");
                return;
        }

        _tickTimerId = SharedSystem.GetModSharp().PushTimer(
            OnTimerTick,
            1.0,
            GameTimerFlags.Repeatable | GameTimerFlags.StopOnMapEnd);
    }

    private MapCycleMode ParseMode(string raw)
    {
        switch (raw.Trim().ToLowerInvariant())
        {
            case "time":
                return MapCycleMode.Time;
            case "round":
                return MapCycleMode.Round;
            case "none":
            case "":
                return MapCycleMode.None;
            default:
                Logger.LogWarning(
                    "[MapCycle] Unknown mcs_mapcycle_mode value '{Raw}'; falling back to None",
                    raw);
                return MapCycleMode.None;
        }
    }

    private void TearDownCurrentMap()
    {
        if (_tickTimerId != Guid.Empty)
        {
            SharedSystem.GetModSharp().StopTimer(_tickTimerId);
            _tickTimerId = Guid.Empty;
        }

        _internalTimeLimitManager = null;
        _transitionTracker = null;
        _mode = MapCycleMode.None;
    }

    #endregion

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
    /// Called every second by the map-scoped timer. Drives time-based limits.
    /// </summary>
    public void OnTimerTick()
    {
        if (_mode != MapCycleMode.Time)
            return;

        _internalTimeLimitManager?.OnTick();
        FireTransitions();
    }

    /// <summary>
    /// Called on round_start. Advances the round-based counter.
    /// </summary>
    private void OnRoundStart()
    {
        _internalTimeLimitManager?.OnTick();
    }

    /// <summary>
    /// Called on round_end. Checks transitions for round-based limits.
    /// </summary>
    private void OnRoundEnd()
    {
        FireTransitions();
    }

    /// <summary>
    /// Called after Extend/Set to re-evaluate transition flags.
    /// </summary>
    public void OnTimeLimitChanged()
    {
        _transitionTracker?.ResetFlags();
    }

    private void FireTransitions()
    {
        if (_transitionTracker is null || _internalTimeLimitManager is null)
            return;

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
