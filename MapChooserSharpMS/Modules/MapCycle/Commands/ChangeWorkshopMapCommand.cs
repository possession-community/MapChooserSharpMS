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

internal sealed class ChangeWorkshopMapCommand(IServiceProvider provider) : McsCommandBase(provider)
{
    public override string CommandName => "wsmap";
    public override string CommandDescription => "Admin: change to a workshop map immediately";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMapCycleController _controller = null!;
    private IMcsMapConfigProvider _mapConfigProvider = null!;

    protected override ICommandValidator? GetValidator()
        => new CompositeValidator()
            .Add(new PermissionValidator("mcs.admin.command.mapcycle.wsmap"))
            .Add(new ArgumentCountValidator(1));

    protected override string GetUsageTranslationKey() => "MapCycle.Command.Admin.WsMap.Usage";

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _controller ??= ServiceProvider.GetRequiredService<IMapCycleController>();
        _mapConfigProvider ??= ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();

        string arg = commandInfo[1];

        if (!long.TryParse(arg, out long workshopId) || workshopId <= 0)
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "MapCycle.Command.Admin.WsMap.Usage"));
            return;
        }

        if (_mapConfigProvider.TryGetMapConfig(workshopId, out var mapConfig))
        {
            string executorName = client?.Name ?? "Console";
            string mapDisplay = _mapConfigProvider.ToolingService.ResolveMapDisplayName(mapConfig);

            var transitionManager = _controller.MapTransitionManager;
            transitionManager.TrySetNextMap(mapConfig);

            PrintLocalizedChatToAll("MapCycle.Broadcast.Admin.ChangingMap", executorName, mapDisplay);

            Logger.LogInformation("Admin {Executor} changing to workshop map {Map} (ID: {Id})",
                executorName, mapConfig.MapName, workshopId);

            transitionManager.TransitionToNextMap(0f);
            return;
        }

        PrintMessageToServerOrPlayerChat(client,
            LocalizeWithPluginPrefix(client, "MapCycle.Command.Admin.SetNextMap.FetchingWorkshop", workshopId));

        _ = Task.Run(async () =>
        {
            try
            {
                var transitionManager = _controller.MapTransitionManager;
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
                        Logger.LogWarning("WsMap workshop {Id}: {Reason}", workshopId, reason);

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

                    PrintLocalizedChatToAll("MapCycle.Broadcast.Admin.ChangingMap", executorName, mapDisplay);

                    Logger.LogInformation("Admin {Executor} changing to workshop map {Map} (ID: {Id})",
                        executorName, mapDisplay, workshopId);

                    transitionManager.TransitionToNextMap(0f);
                });
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "WsMap fetch failed for {Id}", workshopId);
            }
        });
    }
}
