using System;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.MapCycle.Managers.MapTransition;
using MapChooserSharpMS.Modules.MapCycle.Managers.MapTransition.Interfaces;
using MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit;
using MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit.Interfaces;
using MapChooserSharpMS.Modules.MapCycle.Services;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;
using MapChooserSharpMS.Modules.MapVote.Interfaces;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.Events.MapVote;
using MapChooserSharpMS.Shared.Events.MapVote.Params;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle;
using MapChooserSharpMS.Shared.MapCycle.Managers.MapTransition;
using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;
using MapChooserSharpMS.Shared.MapCycle.Services;
using MapChooserSharpMS.Shared.MapVote;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NativeVoteManagerMS.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.MapCycle;

internal sealed class McsMapCycleController
    : PluginModuleBase,
      IMapCycleController,
      IMapCycleExtendController,
      IGameListener,
      IClientListener,
      IEventListener,
      IMapVoteEventListener
{
    public override string PluginModuleName => "McsMapCycleController";
    public override string ModuleChatPrefix => "Prefix.MapCycle";
    protected override bool UseTranslationKeyInModuleChatPrefix => true;

    private IInternalEventManager _eventManager = null!;
    private MapCycleConVars _conVars = null!;

    // Writer for the extend-vote slot of the plugin-owned state manager.
    // Received via ctor already narrowed to <see cref="IMcsInternalExtendVoteState"/>
    // — this controller cannot touch the main-vote slot even by accident.
    private IMcsInternalExtendVoteState _voteState = null!;

    private IInternalTimeLimitManager? _internalTimeLimitManager;
    private TimeLimitTransitionTracker? _transitionTracker;
    private IMcsInternalMapTransitionManager _mapTransitionManager = null!;
    private McsMapExtendService _extendService = null!;
    private McsExtCommandService _extCommandService = null!;
    private McsExtendVoteService _extendVoteService = null!;
    private McsMapCooldownQueryService _cooldownQueryService = null!;
    private McsMapCooldownCommandService _cooldownCommandService = null!;
    private McsMapCooldownLifecycleService _cooldownLifecycleService = null!;

    private MapCycleMode _mode = MapCycleMode.None;
    private Guid _tickTimerId = Guid.Empty;

    public ITimeLimitManager CurrentMapTimeLimitManager =>
        _internalTimeLimitManager
        ?? throw new InvalidOperationException("TimeLimit manager is not initialized for the current map");

    public IMapTransitionManager MapTransitionManager => _mapTransitionManager;

    public IMapCooldownQueryService MapCooldownQueryService => _cooldownQueryService;
    public IMapCooldownCommandService MapCooldownCommandService => _cooldownCommandService;

    #region IMapCycleExtendController

    public int ExtendsLeft => _extendService.ExtendsLeft;

    public int ExtCommandUsesLeft => _extendService.ExtCommandUsesLeft;

    public bool IsExtendVoteInProgress => _extendVoteService.IsExtendVoteInProgress;

    public McsMapExtendResult TryExtendCurrentMap()
        => _extendService.TryExtend(McsExtendTrigger.AdminOrApi);

    public void SetExtCommandUsesLeft(int count)
        => _extendService.SetExtCommandUsesLeft(count);

    public bool IsExtEnabled => _extCommandService.IsEnabled;

    public void EnableExt() => _extCommandService.IsEnabled = true;

    public void DisableExt() => _extCommandService.IsEnabled = false;

    public McsExtendVoteStartResult StartExtendVote(IGameClient? initiator = null)
        => _extendVoteService.StartExtendVote(initiator);

    public bool CancelExtendVote(IGameClient? canceller = null)
        => _extendVoteService.CancelExtendVote(canceller);

    #endregion

    internal McsExtCommandService ExtCommandService => _extCommandService;

    public int ListenerVersion => 1;
    public int ListenerPriority => 0;

    public McsMapCycleController(
        IServiceProvider serviceProvider,
        bool hotReload) : base(serviceProvider, hotReload)
    {
        _conVars = new MapCycleConVars(Plugin.SharedSystem.GetConVarManager());
        foreach (var cv in _conVars.All()) TrackConVar(cv);
    }

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IMapCycleController>(this);
        services.AddSingleton<IMapCycleExtendController>(this);
        // Created during OnInitialize; expose via factory so DI consumers
        // resolve the same instance once it's constructed.
        services.AddSingleton<IMapTransitionManager>(_ => _mapTransitionManager);
        services.AddSingleton<IMcsInternalMapTransitionManager>(_ => _mapTransitionManager);
        services.AddSingleton<IMcsInternalMapExtendService>(_ => _extendService);
        services.AddSingleton<McsExtCommandService>(_ => _extCommandService);
        services.AddSingleton<McsMapCooldownLifecycleService>(_ => _cooldownLifecycleService);
    }

    protected override void OnInitialize()
    {
        _voteState = ServiceProvider.GetRequiredService<IMcsInternalExtendVoteState>();
        _eventManager = ServiceProvider.GetRequiredService<IInternalEventManager>();
        _mapTransitionManager = new McsMapTransitionManager(
            SharedSystem,
            ServiceProvider.GetRequiredService<IMcsMapConfigProvider>(),
            Logger,
            Plugin,
            this,
            _eventManager,
            () => _internalTimeLimitManager?.IsLimitReached == true);

        var configProvider = ServiceProvider.GetRequiredService<IMcsPluginConfigProvider>();
        var readOnlyVoteState = ServiceProvider.GetRequiredService<IMcsReadOnlyVoteState>();

        _extendService = new McsMapExtendService(
            Plugin, this, Logger, _eventManager, configProvider,
            () => _internalTimeLimitManager,
            OnTimeLimitChanged);

        _extCommandService = new McsExtCommandService(
            Plugin, this, Logger, _eventManager, _extendService, readOnlyVoteState, _conVars);

        _extendVoteService = new McsExtendVoteService(
            Plugin, this, Logger, _eventManager, _extendService,
            _voteState, readOnlyVoteState, _conVars,
            () => _mapTransitionManager.CurrentMap);

        var mapConfigProvider = ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();
        _cooldownQueryService = new McsMapCooldownQueryService();
        _cooldownCommandService = new McsMapCooldownCommandService(Logger);
        _cooldownLifecycleService = new McsMapCooldownLifecycleService(
            Logger, Plugin, this, mapConfigProvider, _eventManager);

        SharedSystem.GetModSharp().InstallGameListener(this);
        SharedSystem.GetClientManager().InstallClientListener(this);

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
        _extendVoteService.NativeVoteManager = SharedSystem.GetSharpModuleManager()
            .GetRequiredSharpModuleInterface<INativeVoteManager>(INativeVoteManager.ModSharpModuleIdentity)
            .Instance;

        _eventManager.RegisterListener<IMapVoteEventListener>(this);

        AddCommandsUnderNamespace("MapChooserSharpMS.Modules.MapCycle.Commands");
    }

    protected override void OnUnloadModule()
    {
        TearDownCurrentMap();

        _eventManager.RemoveListener<IMapVoteEventListener>(this);
        SharedSystem.GetModSharp().RemoveGameListener(this);
        SharedSystem.GetClientManager().RemoveClientListener(this);
        SharedSystem.GetEventManager().RemoveEventListener(this);
    }

    #region IMapVoteEventListener

    public void OnMapConfirmed(IMapVoteMapConfirmedEventParams @params)
    {
        _mapTransitionManager.TrySetNextMap(@params.ConfirmedMap);

        if (!@params.IsRtvVote)
            return;

        var cvm = SharedSystem.GetConVarManager();
        bool immediate = cvm.FindConVar("mcs_vote_change_map_immediately_rtv_vote_success")?.GetInt32() != 0;

        if (immediate)
        {
            float delay = cvm.FindConVar("mcs_rtv_map_change_timing")?.GetFloat() ?? 3.0f;
            var intermissionParams = new EventManager.Events.MapCycle.McsIntermissionParams(
                Plugin, this, @params.ConfirmedMap);
            _eventManager.Fire<IMapCycleEventListener>(e => e.OnMcsIntermission(intermissionParams));
            _mapTransitionManager.ChangeMapOnNextRoundEnd = false;
            _mapTransitionManager.TransitionToNextMap(delay);
        }
        else
        {
            _mapTransitionManager.ChangeMapOnNextRoundEnd = true;
        }
    }

    #endregion

    #region IClientListener

    public void OnClientDisconnecting(IGameClient client, NetworkDisconnectionReason reason)
    {
        _extCommandService.RemoveParticipant(client.Slot);
    }

    #endregion

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
        switch (@event.Name)
        {
            case "round_start":
                if (_mode == MapCycleMode.Round)
                    OnRoundStart();
                break;
            case "round_end":
                _mapTransitionManager.OnRoundEnd();
                break;
        }
    }

    #endregion

    #region Lifecycle

    private void InitializeForCurrentMap()
    {
        TearDownCurrentMap();

        var currentMapName = SharedSystem.GetModSharp().GetMapName() ?? string.Empty;
        _mapTransitionManager.SetCurrentMap(currentMapName);
        _extendService.InitializeForCurrentMap(_mapTransitionManager.CurrentMap);
        _extCommandService.ClearParticipants();

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

        _cooldownLifecycleService?.DecrementAllCooldowns();

        if (_cooldownLifecycleService is not null && _mapTransitionManager?.CurrentMap is { } playedMap)
            _cooldownLifecycleService.ApplyPlayedMapCooldown(playedMap);

        _internalTimeLimitManager = null;
        _transitionTracker = null;
        _mode = MapCycleMode.None;
        _mapTransitionManager?.ClearState();
        _extendService?.ClearState();
        _extCommandService?.ClearParticipants();
        _extendVoteService?.ResetOnMapEnd();
    }

    #endregion

    /// <summary>
    /// Initializes a time-based time limit for the current map.
    /// </summary>
    private void InitializeTimeBasedLimit(TimeSpan timeLimit, TimeSpan voteStartThreshold)
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
    private void InitializeRoundBasedLimit(int roundLimit, int voteStartThreshold)
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
    private void OnTimerTick()
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
        if (_mode != MapCycleMode.Round)
            return;
        
        _internalTimeLimitManager?.OnTick();
        FireTransitions();
    }

    /// <summary>
    /// Called after Extend/Set to re-evaluate transition flags.
    /// </summary>
    public void OnTimeLimitChanged()
    {
        _transitionTracker?.ResetFlags();

        // An extend past an already-reached limit means "keep playing" —
        // otherwise the pending round-end transition would fire anyway and
        // the extend would be silently ineffective.
        if (_internalTimeLimitManager is { IsLimitReached: false })
            _mapTransitionManager.ChangeMapOnNextRoundEnd = false;
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
                                Plugin, this, _internalTimeLimitManager.TimeLimitType)));
                    break;

                case TimeLimitTransitionState.LimitReached:
                    _eventManager.Fire<IMapCycleEventListener>(
                        l => l.OnTimeLimitReached(
                            new EventManager.Events.MapCycle.TimeLimitReachedParams(
                                Plugin, this, _internalTimeLimitManager.TimeLimitType)));

                    if (_mapTransitionManager.IsNextMapConfirmed)
                        _mapTransitionManager.ChangeMapOnNextRoundEnd = true;
                    break;
            }
        }
    }
}
