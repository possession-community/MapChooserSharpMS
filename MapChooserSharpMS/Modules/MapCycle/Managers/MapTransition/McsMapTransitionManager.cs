using System;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.MapCycle.Managers.MapTransition.Interfaces;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.WorkshopManagement;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Enums;
using TnmsPluginFoundation.Utils.Other;

namespace MapChooserSharpMS.Modules.MapCycle.Managers.MapTransition;

internal sealed class McsMapTransitionManager : IMcsInternalMapTransitionManager
{
    private readonly ISharedSystem _sharedSystem;
    private readonly IMcsMapConfigProvider _mapConfigProvider;
    private readonly ILogger _logger;

    private IMapConfig? _currentMap;
    private IMapConfig? _nextMap;
    private bool _isNextMapConfirmed;

    public McsMapTransitionManager(
        ISharedSystem sharedSystem,
        IMcsMapConfigProvider mapConfigProvider,
        ILogger logger)
    {
        _sharedSystem = sharedSystem;
        _mapConfigProvider = mapConfigProvider;
        _logger = logger;
    }

    public IMapConfig? NextMap => _nextMap;

    public IMapConfig? CurrentMap => _currentMap;

    public bool IsNextMapConfirmed => _isNextMapConfirmed;

    public bool ChangeMapOnNextRoundEnd { get; set; }

    public bool TrySetNextMap(IMapConfig mapConfig)
    {
        _nextMap = mapConfig;
        _isNextMapConfirmed = true;
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

        _nextMap = null;
        _isNextMapConfirmed = false;
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

    public void ClearState()
    {
        _currentMap = null;
        _nextMap = null;
        _isNextMapConfirmed = false;
        ChangeMapOnNextRoundEnd = false;
    }

    public void OnRoundEnd()
    {
        if (!ChangeMapOnNextRoundEnd)
            return;

        if (_nextMap is null)
            return;

        TransitionToNextMap(0f);
    }
}
