using System;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.MapCycle;
using MapChooserSharpMS.Modules.MapCycle.Managers.MapTransition.Interfaces;
using MapChooserSharpMS.Modules.WorkshopSync;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Managers.MapTransition;
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
    private readonly Func<bool> _shouldStopSourceTv;
    private readonly WorkshopProvisioningService? _workshopProvisioning;

    private readonly unsafe delegate* unmanaged<nint, void> _beginIntermission;

    private IMapInformation? _currentMap;
    private IMapInformation? _nextMap;
    private bool _isNextMapConfirmed;
    private bool _intermissionFired;
    private Guid _retryTimerId;
    private int _changeAttemptsUsed;

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
        Func<bool> shouldStopSourceTv,
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
        _shouldStopSourceTv = shouldStopSourceTv;
        _workshopProvisioning = workshopProvisioning;

        unsafe
        {
            var server = sharedSystem.GetLibraryModuleManager().Server;
            nint fnAddr = server.FindFunction("GMR_BeginIntermission\n");
            if (fnAddr == nint.Zero)
            {
                logger.LogError("[MapTransition] Failed to resolve BeginIntermission function address");
            }
            else
            {
                logger.LogInformation("[MapTransition] Resolved BeginIntermission at 0x{Addr:X}", fnAddr);
            }
            _beginIntermission = (delegate* unmanaged<nint, void>)fnAddr;
        }
    }

    public IMapInformation? NextMap => _nextMap;

    public IMapInformation? CurrentMap => _currentMap;

    public bool IsNextMapConfirmed => _isNextMapConfirmed;

    public bool ChangeMapOnNextRoundEnd { get; set; }

    public bool IsIntermissionFired => _intermissionFired;

    public bool TrySetNextMap(IMapInformation mapInformation)
    {
        var oldNextMap = _nextMap;
        _nextMap = mapInformation;
        _isNextMapConfirmed = true;

        if (oldNextMap?.MapConfig is not { } oldConfig
            || !ReferenceEquals(oldConfig, mapInformation.MapConfig))
        {
            var confirmedParams = new NextMapConfirmedParams(
                _plugin, _moduleBase, mapInformation.MapConfig, oldNextMap?.MapConfig);
            _eventManager.Fire<IMapCycleEventListener>(e => e.OnNextMapConfirmed(confirmedParams));
        }

        return true;
    }

    public bool TrySetNextMap(IMapConfig mapConfig)
    {
        return TrySetNextMap(MapInformation.For(mapConfig).Build());
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

        var removedParams = new NextMapRemovedParams(_plugin, _moduleBase, previousNextMap.MapConfig);
        _eventManager.Fire<IMapCycleEventListener>(e => e.OnNextMapRemoved(removedParams));

        return true;
    }

    public void TransitionToNextMap(float seconds)
    {
        if (_nextMap is null)
            return;

        var targetConfig = _nextMap.MapConfig;

        void ChangeNow()
        {
            _changeAttemptsUsed = 1;
            IssueMapChange(targetConfig);
            ArmEarlyTransitionNotice(targetConfig);
            ArmTransitionRetryWatchdog(targetConfig);
        }

        if (seconds <= 0f)
        {
            ChangeNow();
            return;
        }

        _sharedSystem.GetModSharp().PushTimer(ChangeNow, seconds, GameTimerFlags.None);
    }

    private void IssueMapChange(IMapConfig target)
    {
        if (_shouldStopSourceTv())
            _sharedSystem.GetModSharp().ServerCommand("tv_stoprecord");

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
        string? addonIds = _sharedSystem.GetModSharp().GetAddonName();
        if (addonIds is not null)
        {
            foreach (string segment in addonIds.Split(','))
            {
                if (long.TryParse(segment.Trim(), out long addonId) && addonId > 0
                    && _mapConfigProvider.TryGetMapConfig(addonId, out var byWorkshop))
                {
                    _logger.LogInformation(
                        "[MapTransition] Matched current map by addon ID {AddonId} -> '{ConfigMapName}'",
                        addonId, byWorkshop.MapName);
                    _currentMap = MapInformation.For(byWorkshop).Build();
                    return;
                }
            }
        }

        if (_mapConfigProvider.TryGetMapConfig(mapName, out var found))
        {
            _currentMap = MapInformation.For(found).Build();
            return;
        }

        _logger.LogInformation(
            "[MapTransition] No map config found for current map '{MapName}'; CurrentMap is null",
            mapName);
        _currentMap = null;
    }

    public unsafe void ForceEndMatch()
    {
        if (_intermissionFired || _nextMap is null)
            return;

        _intermissionFired = true;

        var intermissionParams = new McsIntermissionParams(_plugin, _moduleBase, _nextMap.MapConfig);
        _eventManager.Fire<IMapCycleEventListener>(e => e.OnMcsIntermission(intermissionParams));

        var modSharp = _sharedSystem.GetModSharp();

        if (_conVars.EndMatchImmediately.GetInt32() != 0 && _beginIntermission != null)
        {
            modSharp.GetGameRules().TerminateRound(0.0f, RoundEndReason.RoundDraw);
            modSharp.InvokeFrameAction(() =>
            {
                nint gameRulesPtr = modSharp.GetGameRules().GetAbsPtr();
                _beginIntermission(gameRulesPtr);
            });
            _logger.LogInformation("[MapTransition] TerminateRound -> BeginIntermission scheduled");
        }
        else
        {
            ChangeMapOnNextRoundEnd = true;
            _logger.LogInformation("[MapTransition] ForceEndMatch deferred to round end");
        }

        float extraTime = _sharedSystem.GetConVarManager()
            .FindConVar("mp_competitive_endofmatch_extra_time")?.GetFloat() ?? 5.0f;
        float delay = Math.Max(extraTime - 1.0f, 0f);
        TransitionToNextMap(delay);
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
        _intermissionFired = false;
        ChangeMapOnNextRoundEnd = false;
        StopRetryWatchdog(resetAttempts: true);
    }

    public void OnRoundEnd()
    {
        if (!ChangeMapOnNextRoundEnd)
            return;

        if (_nextMap is null || _intermissionFired)
            return;

        _intermissionFired = true;

        var intermissionParams = new McsIntermissionParams(_plugin, _moduleBase, _nextMap.MapConfig);
        _eventManager.Fire<IMapCycleEventListener>(e => e.OnMcsIntermission(intermissionParams));

        float delay = _conVars.TransitionDelay.GetFloat();
        TransitionToNextMap(delay);
    }

    public void TerminateAndTransition(float? terminateDelay = null)
    {
        if (_nextMap is null || _intermissionFired)
            return;

        _intermissionFired = true;

        float delay = terminateDelay ?? _conVars.TransitionDelay.GetFloat();

        var intermissionParams = new McsIntermissionParams(_plugin, _moduleBase, _nextMap.MapConfig);
        _eventManager.Fire<IMapCycleEventListener>(e => e.OnMcsIntermission(intermissionParams));

        string mapDisplay = ResolveDisplayName(_nextMap.MapConfig);
        for (int i = 0; i < 3; i++)
            BroadcastToAll("MapCycle.Broadcast.MapChanging", mapDisplay);

        if (TnmsPluginFoundation.Utils.Entity.GameRulesUtil.IsWarmup())
        {
            _logger.LogInformation("[MapTransition] Ending warmup before map transition");
            _sharedSystem.GetModSharp().ServerCommand("mp_warmup_end");

            _sharedSystem.GetModSharp().PushTimer(() =>
            {
                ForceTerminateRound(delay);
            }, 1.0, GameTimerFlags.StopOnMapEnd);
            TransitionToNextMap(delay + 1.0f);
            return;
        }

        ForceTerminateRound(delay);
        TransitionToNextMap(delay);
    }

    private void ForceTerminateRound(float terminateDelay)
    {
        _logger.LogInformation("[MapTransition] Forcing round termination in {Delay}s", terminateDelay);

        TnmsPluginFoundation.Utils.Entity.GameRulesUtil.TerminateRound(terminateDelay, RoundEndReason.RoundDraw);
    }
}
