using System;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.Commands;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Modules.WorkshopSync;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Managers.MapTransition;
using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.WorkshopManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;

namespace MapChooserSharpMS.Modules.Nomination.Commands;

internal sealed class AdminNominateWorkshopCommand(IServiceProvider provider) : NominationCommandBase(provider), IDisposable
{
    public override string CommandName => "nominate_addwsmap";
    public override string CommandDescription => "Admin: force-add a workshop map to nomination";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMcsInternalNominationController _controller = null!;
    private IMcsMapConfigProvider _mapConfigProvider = null!;
    private IMcsReadOnlyVoteState _voteState = null!;
    private IMapTransitionManager _transitionManager = null!;
    private WorkshopProvisioningService? _workshopProvisioning;
    private bool _workshopProvisioningResolved;

    protected override ICommandValidator? GetValidator()
        => new CompositeValidator()
            .Add(new PermissionValidator("mcs.admin.command.nomination.addwsmap"))
            .Add(new ArgumentCountValidator(1));

    protected override string GetUsageTranslationKey() => "NominationAddWsMap.Command.Notification.Usage";

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _controller ??= ServiceProvider.GetRequiredService<IMcsInternalNominationController>();
        _mapConfigProvider ??= ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();
        _voteState ??= ServiceProvider.GetRequiredService<IMcsReadOnlyVoteState>();
        _transitionManager ??= ServiceProvider.GetRequiredService<IMapTransitionManager>();

        if (!_workshopProvisioningResolved)
        {
            _workshopProvisioning = ResolveWorkshopProvisioning();
            _workshopProvisioningResolved = true;
        }

        if (_voteState.CurrentVoteState == McsMapVoteState.NextMapConfirmed)
        {
            string nextMapDisplay = _transitionManager.NextMap is { } nextMap
                ? _mapConfigProvider.ToolingService.ResolveMapDisplayName(nextMap.MapConfig)
                : LocalizeString(client, "Word.VotePending");
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithNominationPrefix(client, "MapCycle.Command.Notification.NextMap", nextMapDisplay));
            return;
        }

        string arg = commandInfo[1];

        if (!long.TryParse(arg, out long workshopId) || workshopId <= 0)
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithNominationPrefix(client, "NominationAddWsMap.Command.Notification.Usage"));
            return;
        }

        if (_mapConfigProvider.TryGetMapConfig(workshopId, out var mapConfig))
        {
            var nominateResult = _controller.NominationService.TryAdminNominateMap(client, mapConfig);
            if (nominateResult.Count > 0)
                _controller.NotifyNominationFailure(client, mapConfig, nominateResult);
            return;
        }

        if (_workshopProvisioning is null || !_workshopProvisioning.IsAvailable)
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithNominationPrefix(client, "MapCycle.Command.Admin.SetNextMap.WorkshopNotAvailable",
                    workshopId, "no API key"));
            return;
        }

        PrintMessageToServerOrPlayerChat(client,
            LocalizeWithNominationPrefix(client, "MapCycle.Command.Admin.SetNextMap.FetchingWorkshop", workshopId));

        _ = Task.Run(async () =>
        {
            try
            {
                var provision = await _workshopProvisioning.TryProvisionAsync(workshopId);

                SharedSystem.GetModSharp().InvokeFrameAction(() =>
                {
                    if (provision.MapConfig is null)
                    {
                        string reason = provision.Status switch
                        {
                            ExistenceStatus.NotAvailableInWorkshop => "private/deleted",
                            ExistenceStatus.FailedToFetchHttpError => "HTTP error",
                            _ => "unknown",
                        };

                        if (client is null || client.IsValid)
                        {
                            PrintMessageToServerOrPlayerChat(client,
                                LocalizeWithNominationPrefix(client, "MapCycle.Command.Admin.SetNextMap.WorkshopNotAvailable",
                                    workshopId, reason));
                        }
                        return;
                    }

                    if (client is not null && !client.IsValid)
                        return;

                    var nominateResult = _controller.NominationService.TryAdminNominateMap(client, provision.MapConfig);
                    if (nominateResult.Count > 0)
                        _controller.NotifyNominationFailure(client, provision.MapConfig, nominateResult);
                });
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Workshop fetch failed for nomination {Id}", workshopId);
            }
        });
    }

    public void Dispose()
    {
        _workshopProvisioning?.Dispose();
        _workshopProvisioning = null;
    }

    private WorkshopProvisioningService? ResolveWorkshopProvisioning()
    {
        try
        {
            var configProvider = ServiceProvider.GetRequiredService<IMcsPluginConfigProvider>();
            string apiKey = configProvider.PluginConfig.GeneralConfig.SteamWebApiKey;
            if (string.IsNullOrEmpty(apiKey))
                return null;

            var apiService = new SteamWorkshopApiService();
            apiService.SetApiKey(apiKey);
            return new WorkshopProvisioningService(apiService);
        }
        catch
        {
            return null;
        }
    }
}
