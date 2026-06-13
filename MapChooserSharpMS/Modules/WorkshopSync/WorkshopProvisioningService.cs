using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.MapConfig.Extra;
using MapChooserSharpMS.Modules.MapConfig.Models;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.WorkshopManagement;

namespace MapChooserSharpMS.Modules.WorkshopSync;

internal sealed class WorkshopProvisioningService
{
    private readonly SteamWorkshopApiService? _apiService;

    public WorkshopProvisioningService(SteamWorkshopApiService? apiService)
    {
        _apiService = apiService;
    }

    public bool IsAvailable => _apiService is not null && !string.IsNullOrEmpty(_apiService.ApiKey);

    public async Task<WorkshopProvisionResult> TryProvisionAsync(long workshopId, CancellationToken ct = default)
    {
        if (_apiService is null || string.IsNullOrEmpty(_apiService.ApiKey))
        {
            return new WorkshopProvisionResult(null, ExistenceStatus.FailedToFetchUnknown, null, workshopId);
        }

        var details = await _apiService.GetPublishedFileDetails([workshopId], ct);
        if (details.Count == 0)
        {
            return new WorkshopProvisionResult(null, ExistenceStatus.NotAvailableInWorkshop, null, workshopId);
        }

        var item = details[0];
        if (item.Status is not (WorkshopItemStatus.Public or WorkshopItemStatus.Unlisted or WorkshopItemStatus.FriendsOnly))
        {
            return new WorkshopProvisionResult(null, ExistenceStatus.NotAvailableInWorkshop, item.Title, workshopId);
        }

        var config = BuildProvisionalMapConfig(item);
        return new WorkshopProvisionResult(config, ExistenceStatus.FoundInWorkshop, config.MapNameAlias, workshopId);
    }

    internal static MapConfig.Models.MapConfig BuildProvisionalMapConfig(WorkshopItemInfo item)
    {
        string mapName = item.Title ?? $"workshop_{item.PublishedFileId}";

        return new MapConfig.Models.MapConfig(
            MapName: mapName,
            MapNameAlias: "",
            MapDescription: "",
            WorkshopId: item.PublishedFileId,
            GroupSettings: [],
            IsDisabled: false,
            MaxExtends: 3,
            MaxExtCommandUses: 1,
            MapTime: 20,
            ExtendTimePerExtends: 15,
            MapRounds: 10,
            ExtendRoundsPerExtends: 5,
            RandomPickConfig: new RandomPickConfig(
                MapSelectionWeight: 1,
                IsPickable: false,
                BypassNominationRestriction: false),
            NominationConfig: new NominationConfig(
                MaxPlayers: 0,
                MinPlayers: 0,
                ProhibitAdminNomination: false,
                DaysAllowed: [],
                AllowedTimeRanges: []),
            CooldownConfig: new CooldownConfig(configCooldown: 0, timedCooldown: TimeSpan.Zero),
            ExtraConfiguration: ExtraConfigAccessor.Empty);
    }
}

internal sealed record WorkshopProvisionResult(
    IMapConfig? MapConfig,
    ExistenceStatus Status,
    string? Title,
    long WorkshopId);
