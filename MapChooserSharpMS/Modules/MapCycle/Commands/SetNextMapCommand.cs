using System;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.Commands;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle;
using MapChooserSharpMS.Shared.WorkshopManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;

namespace MapChooserSharpMS.Modules.MapCycle.Commands;

internal sealed class SetNextMapCommand(IServiceProvider provider) : McsCommandBase(provider)
{
    public override string CommandName => "setnextmap";
    public override string CommandDescription => "Admin: set the next map";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMapCycleController _controller = null!;
    private IMcsMapConfigProvider _mapConfigProvider = null!;

    protected override ICommandValidator? GetValidator()
        => new CompositeValidator()
            .Add(new PermissionValidator("mcs.admin.command.mapcycle.setnextmap"))
            .Add(new ArgumentCountValidator(1));

    protected override string GetUsageTranslationKey() => "MapCycle.Command.Admin.SetNextMap.Usage";

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _controller ??= ServiceProvider.GetRequiredService<IMapCycleController>();
        _mapConfigProvider ??= ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();

        string mapName = commandInfo[1];

        if (_mapConfigProvider.TryGetMapConfig(mapName, out var mapConfig))
        {
            ApplyNextMap(client, mapConfig);
            return;
        }

        if (long.TryParse(mapName, out long workshopId) && workshopId > 0)
        {
            FetchAndSetFromWorkshop(client, workshopId);
            return;
        }

        PrintMessageToServerOrPlayerChat(client,
            LocalizeWithPluginPrefix(client, "General.Notification.MapNotFound", mapName));
    }

    private void ApplyNextMap(IGameClient? client, IMapConfig mapConfig)
    {
        var transitionManager = _controller.MapTransitionManager;
        var previousNextMap = transitionManager.NextMap;

        string executorName = client?.Name ?? "Console";
        string mapDisplay = _mapConfigProvider.ToolingService.ResolveMapDisplayName(mapConfig);

        if (!transitionManager.TrySetNextMap(mapConfig))
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "MapCycle.Command.Admin.SetNextMap.Failed", mapDisplay));
            return;
        }

        PrintLocalizedChatToAll(
            previousNextMap is not null
                ? "MapCycle.Broadcast.Admin.ChangedNextMap"
                : "MapCycle.Broadcast.Admin.SetNextMap",
            executorName, mapDisplay);

        Logger.LogInformation(
            "Admin {Executor} set next map to {Map} (Workshop ID: {WorkshopId})",
            executorName, mapConfig.MapName, mapConfig.WorkshopId);
    }

    private void FetchAndSetFromWorkshop(IGameClient? client, long workshopId)
    {
        var transitionManager = _controller.MapTransitionManager;

        PrintMessageToServerOrPlayerChat(client,
            LocalizeWithPluginPrefix(client, "MapCycle.Command.Admin.SetNextMap.FetchingWorkshop", workshopId));

        _ = Task.Run(async () =>
        {
            var (success, fetchResult) = await transitionManager.TrySetNextMap(workshopId);

            SharedSystem.GetModSharp().InvokeFrameAction(() =>
            {
                if (!success)
                {
                    string reason = fetchResult.ExistenceStatus switch
                    {
                        ExistenceStatus.NotAvailableInWorkshop => "private/deleted",
                        ExistenceStatus.FailedToFetchHttpError => "HTTP error",
                        _ => "unknown",
                    };
                    Logger.LogWarning("SetNextMap workshop {Id}: {Reason}", workshopId, reason);

                    if (client is null || client.IsValid)
                    {
                        PrintMessageToServerOrPlayerChat(client,
                            LocalizeWithPluginPrefix(client, "MapCycle.Command.Admin.SetNextMap.WorkshopNotAvailable",
                                workshopId, reason));
                    }
                    return;
                }

                string executorName = client is not null && client.IsValid ? client.Name : "Console";
                string mapDisplay = fetchResult.MapName ?? workshopId.ToString();

                PrintLocalizedChatToAll(
                    "MapCycle.Broadcast.Admin.SetNextMap",
                    executorName, mapDisplay);

                Logger.LogInformation(
                    "Admin {Executor} set next map from workshop: {Map} (Workshop ID: {WorkshopId})",
                    executorName, mapDisplay, workshopId);
            });
        });
    }
}
