using System;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.MapCycle;
using MapChooserSharpMS.Modules.MapCycle.Managers.MapTransition.Interfaces;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;
using MapChooserSharpMS.Shared.WorkshopManagement;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Enums;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;
using TnmsPluginFoundation.Utils.Other;

namespace MapChooserSharpMS.Modules.MapCycle.Managers.MapTransition;

internal sealed class McsMapTransitionManager : IMcsInternalMapTransitionManager
{
    private readonly ISharedSystem _sharedSystem;
    private readonly IMcsMapConfigProvider _mapConfigProvider;
    private readonly ILogger _logger;
    private readonly TnmsPlugin _plugin;
    private readonly PluginModuleBase _moduleBase;
    private readonly IInternalEventManager _eventManager;
    private readonly Func<bool> _isTimeLimitReached;
    private readonly Func<TimeLimitType?> _timeLimitTypeProvider;

    private IMapConfig? _currentMap;
    private IMapConfig? _nextMap;
    private bool _isNextMapConfirmed;

    private Sharp.Shared.Objects.IConVar? _forcedLimitConVar;
    private float _forcedLimitOriginalValue;
    private bool _forcedLimitIsRoundBased;

    public McsMapTransitionManager(
        ISharedSystem sharedSystem,
        IMcsMapConfigProvider mapConfigProvider,
        ILogger logger,
        TnmsPlugin plugin,
        PluginModuleBase moduleBase,
        IInternalEventManager eventManager,
        Func<bool> isTimeLimitReached,
        Func<TimeLimitType?> timeLimitTypeProvider)
    {
        _sharedSystem = sharedSystem;
        _mapConfigProvider = mapConfigProvider;
        _logger = logger;
        _plugin = plugin;
        _moduleBase = moduleBase;
        _eventManager = eventManager;
        _isTimeLimitReached = isTimeLimitReached;
        _timeLimitTypeProvider = timeLimitTypeProvider;
    }

    public IMapConfig? NextMap => _nextMap;

    public IMapConfig? CurrentMap => _currentMap;

    public bool IsNextMapConfirmed => _isNextMapConfirmed;

    public bool ChangeMapOnNextRoundEnd { get; set; }

    public bool TrySetNextMap(IMapConfig mapConfig)
    {
        var oldNextMap = _nextMap;
        _nextMap = mapConfig;
        _isNextMapConfirmed = true;

        // A next map confirmed after the limit already ran out (vote finished
        // late, admin set it manually, external API) must still transition —
        // LimitReached has already fired and will not re-fire.
        if (_isTimeLimitReached())
            ChangeMapOnNextRoundEnd = true;

        if (!ReferenceEquals(oldNextMap, mapConfig))
        {
            var confirmedParams = new NextMapConfirmedParams(_plugin, _moduleBase, mapConfig, oldNextMap);
            _eventManager.Fire<IMapCycleEventListener>(e => e.OnNextMapConfirmed(confirmedParams));
        }

        return true;
    }

    public bool TrySetNextMap(string mapName)
    {
        if (!_mapConfigProvider.TryGetMapConfig(mapName, out var found))
            return false;

        return TrySetNextMap(found);
    }

    public Task<(bool Success, IWorkshopFetchResult FetchResult)> TrySetNextMap(long workshopId)
    {
        // First: try to find in already-loaded map configs.
        if (_mapConfigProvider.TryGetMapConfig(workshopId, out var found))
        {
            TrySetNextMap(found);
            IWorkshopFetchResult hit = new WorkshopFetchResult
            {
                ExistenceStatus = ExistenceStatus.FoundInMemoryConfig,
                MapName = found.MapName,
                WorkshopId = workshopId,
            };
            return Task.FromResult((true, hit));
        }

        // Second: remote workshop fetch is deferred (planned: SteamApi.Net based).
        // Until that lands, surface a clear "unknown" result so callers can decide.
        IWorkshopFetchResult miss = new WorkshopFetchResult
        {
            ExistenceStatus = ExistenceStatus.FailedToFetchUnknown,
            MapName = null,
            WorkshopId = workshopId,
        };
        return Task.FromResult((false, miss));
    }

    public bool TryRemoveNextMap()
    {
        if (_nextMap is null)
            return false;

        var previousNextMap = _nextMap;
        _nextMap = null;
        _isNextMapConfirmed = false;

        var removedParams = new NextMapRemovedParams(_plugin, _moduleBase, previousNextMap);
        _eventManager.Fire<IMapCycleEventListener>(e => e.OnNextMapRemoved(removedParams));

        return true;
    }

    public void TransitionToNextMap(float seconds)
    {
        if (_nextMap is null)
            return;

        var target = _nextMap;

        void ChangeNow()
        {
            if (target.WorkshopId > 0)
            {
                MapUtil.ChangeToWorkshopMap(target.WorkshopId);
                return;
            }

            MapUtil.ChangeMap(target.MapName);
        }

        if (seconds <= 0f)
        {
            ChangeNow();
            return;
        }

        _sharedSystem.GetModSharp().PushTimer(ChangeNow, seconds, GameTimerFlags.None);
    }

    public void SetCurrentMap(string mapName)
    {
        if (_mapConfigProvider.TryGetMapConfig(mapName, out var found))
        {
            _currentMap = found;
            return;
        }

        _logger.LogInformation(
            "[MapTransition] No map config found for current map '{MapName}'; CurrentMap is null",
            mapName);
        _currentMap = null;
    }

    public void ForceEndMatch()
    {
        var limitType = _timeLimitTypeProvider();
        bool roundBased = limitType == TimeLimitType.Round;

        // One-shot mp_* write — a deliberate, scoped exception to the
        // "internal TimeLimit only" rule: the game must see its own
        // game-over condition met to run the native end match screen.
        // The original value is restored in ClearState() on map end.
        var conVar = _sharedSystem.GetConVarManager()
            .FindConVar(roundBased ? "mp_maxrounds" : "mp_timelimit");

        if (conVar is not null)
        {
            if (_forcedLimitConVar is null)
            {
                _forcedLimitConVar = conVar;
                _forcedLimitOriginalValue = conVar.GetFloat();
                _forcedLimitIsRoundBased = roundBased;
            }

            if (roundBased)
                conVar.Set(1);
            else
                conVar.Set(1.0f);
        }

        var modSharp = _sharedSystem.GetModSharp();
        modSharp.InvokeFrameAction(() =>
        {
            if (modSharp.GetGameRules().IsWarmupPeriod)
                modSharp.ServerCommand("mp_warmup_end");

            modSharp.InvokeFrameAction(() =>
            {
                modSharp.GetGameRules().TerminateRound(0.0f, RoundEndReason.RoundDraw);
            });
        });
    }

    public void ClearState()
    {
        _currentMap = null;
        _nextMap = null;
        _isNextMapConfirmed = false;
        ChangeMapOnNextRoundEnd = false;

        if (_forcedLimitConVar is not null)
        {
            if (_forcedLimitIsRoundBased)
                _forcedLimitConVar.Set((int)_forcedLimitOriginalValue);
            else
                _forcedLimitConVar.Set(_forcedLimitOriginalValue);

            _forcedLimitConVar = null;
        }
    }

    public void OnRoundEnd()
    {
        if (!ChangeMapOnNextRoundEnd)
            return;

        if (_nextMap is null)
            return;

        var intermissionParams = new McsIntermissionParams(_plugin, _moduleBase, _nextMap);
        _eventManager.Fire<IMapCycleEventListener>(e => e.OnMcsIntermission(intermissionParams));

        TransitionToNextMap(0f);
    }
}
