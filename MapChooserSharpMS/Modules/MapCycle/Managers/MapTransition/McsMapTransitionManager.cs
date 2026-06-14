using System;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.MapCycle;
using MapChooserSharpMS.Modules.MapCycle.Managers.MapTransition.Interfaces;
using MapChooserSharpMS.Modules.WorkshopSync;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;
using MapChooserSharpMS.Shared.WorkshopManagement;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Enums;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Extensions.Client;
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
    private readonly MapCycleConVars _conVars;
    private readonly WorkshopProvisioningService? _workshopProvisioning;

    private IMapConfig? _currentMap;
    private IMapConfig? _nextMap;
    private bool _isNextMapConfirmed;
    private bool _transitionStarted;
    private Guid _retryTimerId;
    private int _changeAttemptsUsed;

    private string? _forcedLimitConVarName;
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
        Func<TimeLimitType?> timeLimitTypeProvider,
        MapCycleConVars conVars,
        WorkshopProvisioningService? workshopProvisioning)
    {
        _sharedSystem = sharedSystem;
        _mapConfigProvider = mapConfigProvider;
        _logger = logger;
        _plugin = plugin;
        _moduleBase = moduleBase;
        _eventManager = eventManager;
        _isTimeLimitReached = isTimeLimitReached;
        _timeLimitTypeProvider = timeLimitTypeProvider;
        _conVars = conVars;
        _workshopProvisioning = workshopProvisioning;
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

    public async Task<(bool Success, IWorkshopFetchResult FetchResult)> TrySetNextMap(long workshopId)
    {
        if (_mapConfigProvider.TryGetMapConfig(workshopId, out var found))
        {
            TrySetNextMap(found);
            IWorkshopFetchResult hit = new WorkshopFetchResult
            {
                ExistenceStatus = ExistenceStatus.FoundInMemoryConfig,
                MapName = found.MapName,
                WorkshopId = workshopId,
            };
            return (true, hit);
        }

        if (_workshopProvisioning is null || !_workshopProvisioning.IsAvailable)
        {
            IWorkshopFetchResult noApi = new WorkshopFetchResult
            {
                ExistenceStatus = ExistenceStatus.FailedToFetchUnknown,
                MapName = null,
                WorkshopId = workshopId,
            };
            return (false, noApi);
        }

        try
        {
            var provision = await _workshopProvisioning.TryProvisionAsync(workshopId);

            if (provision.MapConfig is null)
            {
                _logger.LogWarning("Workshop item {Id} is not available (status: {Status})", workshopId, provision.Status);
                IWorkshopFetchResult unavailable = new WorkshopFetchResult
                {
                    ExistenceStatus = provision.Status,
                    MapName = provision.Title,
                    WorkshopId = workshopId,
                };
                return (false, unavailable);
            }

            var tcs = new TaskCompletionSource<(bool, IWorkshopFetchResult)>();
            _sharedSystem.GetModSharp().InvokeFrameAction(() =>
            {
                try
                {
                    TrySetNextMap(provision.MapConfig);
                    _logger.LogInformation("Set next map from workshop: {Title} (ID: {Id})", provision.Title, workshopId);

                    IWorkshopFetchResult workshopHit = new WorkshopFetchResult
                    {
                        ExistenceStatus = ExistenceStatus.FoundInWorkshop,
                        MapName = provision.MapConfig.MapName,
                        WorkshopId = workshopId,
                    };
                    tcs.SetResult((true, workshopHit));
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return await tcs.Task;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch workshop item {Id}", workshopId);
            IWorkshopFetchResult error = new WorkshopFetchResult
            {
                ExistenceStatus = ExistenceStatus.FailedToFetchHttpError,
                MapName = null,
                WorkshopId = workshopId,
            };
            return (false, error);
        }
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

        _transitionStarted = true;
        var target = _nextMap;

        void ChangeNow()
        {
            _changeAttemptsUsed = 1;
            IssueMapChange(target);
            ArmEarlyTransitionNotice(target);
            ArmTransitionRetryWatchdog(target);
        }

        if (seconds <= 0f)
        {
            ChangeNow();
            return;
        }

        _sharedSystem.GetModSharp().PushTimer(ChangeNow, seconds, GameTimerFlags.None);
    }

    private static void IssueMapChange(IMapConfig target)
    {
        if (target.WorkshopId > 0)
        {
            MapUtil.ChangeToWorkshopMap(target.WorkshopId);
            return;
        }

        MapUtil.ChangeMap(target.MapName);
    }

    private void ArmEarlyTransitionNotice(IMapConfig target)
    {
        _sharedSystem.GetModSharp().PushTimer(() =>
        {
            BroadcastToAll("MapCycle.Broadcast.MapTransitionPending", ResolveDisplayName(target));
        }, 5.0, GameTimerFlags.StopOnMapEnd);
    }

    /// <summary>
    /// Watches an issued map change: if the map still has not changed after
    /// the configured interval, the change is re-issued up to the configured
    /// attempt count; once exhausted, the server changes to the fallback map
    /// instead of stalling on a map it cannot load (e.g. broken workshop
    /// download). The timer dies with the map, so a successful change
    /// disarms it automatically.
    /// </summary>
    private void ArmTransitionRetryWatchdog(IMapConfig target)
    {
        StopRetryWatchdog(resetAttempts: false);

        float interval = _conVars.TransitionRetryInterval.GetFloat();
        _retryTimerId = _sharedSystem.GetModSharp().PushTimer(() =>
        {
            int maxAttempts = _conVars.TransitionRetryAttempts.GetInt32();

            if (_changeAttemptsUsed < maxAttempts)
            {
                _changeAttemptsUsed++;
                _logger.LogWarning(
                    "[MapTransition] Map change to {Map} did not happen — retrying ({Attempt}/{Max})",
                    target.MapName, _changeAttemptsUsed, maxAttempts);
                BroadcastToAll("MapCycle.Broadcast.MapTransitionRetry",
                    ResolveDisplayName(target), _changeAttemptsUsed, maxAttempts);
                IssueMapChange(target);
                return;
            }

            StopRetryWatchdog(resetAttempts: true);

            string fallbackMap = _conVars.TransitionFallbackMap.GetString();
            _logger.LogError(
                "[MapTransition] Map change to {Map} failed after {Max} attempts — falling back to {Fallback}",
                target.MapName, maxAttempts, fallbackMap);
            BroadcastToAll("MapCycle.Broadcast.MapTransitionFallbackMap",
                ResolveDisplayName(target), fallbackMap);
            MapUtil.ChangeMap(fallbackMap);
        }, interval, GameTimerFlags.Repeatable | GameTimerFlags.StopOnMapEnd);
    }

    private void StopRetryWatchdog(bool resetAttempts)
    {
        if (_retryTimerId != Guid.Empty)
        {
            _sharedSystem.GetModSharp().StopTimer(_retryTimerId);
            _retryTimerId = Guid.Empty;
        }

        if (resetAttempts)
            _changeAttemptsUsed = 0;
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
        string conVarName = roundBased ? "mp_maxrounds" : "mp_timelimit";

        // One-shot mp_* write — a deliberate, scoped exception to the
        // "internal TimeLimit only" rule: the game must see its own
        // game-over condition met to run the native end match screen.
        // The original value is restored in ClearState() on map end.
        // Written via the command path — IConVar.Set did not reliably
        // reach the value the game rules read for the game-over check.
        if (_forcedLimitConVarName is null)
        {
            _forcedLimitConVarName = conVarName;
            _forcedLimitOriginalValue = _sharedSystem.GetConVarManager()
                .FindConVar(conVarName)?.GetFloat() ?? 0f;
            _forcedLimitIsRoundBased = roundBased;
        }

        var modSharp = _sharedSystem.GetModSharp();
        modSharp.ServerCommand($"{conVarName} 1");

        modSharp.InvokeFrameAction(() =>
        {
            if (modSharp.GetGameRules().IsWarmupPeriod)
                modSharp.ServerCommand("mp_warmup_end");

            modSharp.InvokeFrameAction(() =>
            {
                modSharp.GetGameRules().TerminateRound(0.0f, RoundEndReason.RoundDraw);
            });
        });

        ArmForceEndMatchWatchdog();
    }

    /// <summary>
    /// Safety net for ForceEndMatch: if the game never reaches its native
    /// intermission (e.g. round termination suppressed by server settings),
    /// notify players and transition directly instead of stalling forever.
    /// </summary>
    private void ArmForceEndMatchWatchdog()
    {
        float extraTime = _sharedSystem.GetConVarManager()
            .FindConVar("mp_competitive_endofmatch_extra_time")?.GetFloat() ?? 5.0f;
        float restartDelay = _sharedSystem.GetConVarManager()
            .FindConVar("mp_round_restart_delay")?.GetFloat() ?? 5.0f;
        float timeout = extraTime + restartDelay + 10.0f;

        _sharedSystem.GetModSharp().PushTimer(() =>
        {
            if (_transitionStarted || _nextMap is null)
                return;

            _logger.LogWarning(
                "[MapTransition] ForceEndMatch watchdog fired after {Timeout}s — native end match never reached intermission; transitioning directly",
                timeout);

            BroadcastToAll("MapCycle.Broadcast.ForceEndMatchFallback", ResolveDisplayName(_nextMap));

            var intermissionParams = new McsIntermissionParams(_plugin, _moduleBase, _nextMap);
            _eventManager.Fire<IMapCycleEventListener>(e => e.OnMcsIntermission(intermissionParams));

            TransitionToNextMap(0f);
        }, timeout, GameTimerFlags.StopOnMapEnd);
    }

    private string ResolveDisplayName(IMapConfig map)
        => _mapConfigProvider.ToolingService.ResolveMapDisplayName(map);

    private void BroadcastToAll(string key, params object[] args)
    {
        var clients = _sharedSystem.GetModSharp().GetIServer().GetGameClients(true);
        foreach (var client in clients)
        {
            if (client.IsFakeClient || client.IsHltv)
                continue;

            client.GetPlayerController()?.PrintToChat(
                $" {_plugin.GetPluginPrefix(client)} {_plugin.LocalizeStringForPlayer(client, key, args)}");
        }
    }

    public void ClearState()
    {
        _currentMap = null;
        _nextMap = null;
        _isNextMapConfirmed = false;
        _transitionStarted = false;
        ChangeMapOnNextRoundEnd = false;
        StopRetryWatchdog(resetAttempts: true);

        if (_forcedLimitConVarName is not null)
        {
            string restored = _forcedLimitIsRoundBased
                ? ((int)_forcedLimitOriginalValue).ToString(System.Globalization.CultureInfo.InvariantCulture)
                : _forcedLimitOriginalValue.ToString(System.Globalization.CultureInfo.InvariantCulture);

            _sharedSystem.GetModSharp().ServerCommand($"{_forcedLimitConVarName} {restored}");
            _forcedLimitConVarName = null;
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

        float delay = _conVars.TransitionDelay.GetFloat();
        TransitionToNextMap(delay);
    }

    public void TerminateAndTransition(float terminateDelay = 0f)
    {
        if (_nextMap is null)
            return;

        ChangeMapOnNextRoundEnd = true;

        if (TnmsPluginFoundation.Utils.Entity.GameRulesUtil.IsWarmup())
        {
            _logger.LogInformation("[MapTransition] Ending warmup before map transition");
            _sharedSystem.GetModSharp().ServerCommand("mp_warmup_end");
        }

        var cvm = _sharedSystem.GetConVarManager();
        cvm.FindConVar("mp_timelimit")?.Set(1);
        cvm.FindConVar("mp_maxrounds")?.Set(1);

        string mapDisplay = ResolveDisplayName(_nextMap);
        for (int i = 0; i < 3; i++)
            BroadcastToAll("MapCycle.Broadcast.MapChanging", mapDisplay);

        _logger.LogInformation("[MapTransition] Forcing round termination for map transition in {Delay}s", terminateDelay);

        TnmsPluginFoundation.Utils.Entity.GameRulesUtil.TerminateRound(terminateDelay, RoundEndReason.RoundDraw);
    }
}
