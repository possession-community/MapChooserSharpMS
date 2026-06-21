using System;
using MapChooserSharpMS.Modules.Commands;
using MapChooserSharpMS.Modules.MapCycle.Managers.MapTransition;
using MapChooserSharpMS.Modules.MapCycle.Managers.MapTransition.Interfaces;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;

namespace MapChooserSharpMS.Modules.MapCycle.Commands;

internal sealed class ChangeMapCommand(IServiceProvider provider) : McsCommandBase(provider)
{
    public override string CommandName => "map";
    public override string CommandDescription => "Admin: change map immediately";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMapCycleController _controller = null!;
    private IMcsMapConfigProvider _mapConfigProvider = null!;

    protected override ICommandValidator? GetValidator()
        => new CompositeValidator()
            .Add(new PermissionValidator("mcs.admin.command.mapcycle.map"))
            .Add(new ArgumentCountValidator(1));

    protected override string GetUsageTranslationKey() => "MapCycle.Command.Admin.Map.Usage";

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _controller ??= ServiceProvider.GetRequiredService<IMapCycleController>();
        _mapConfigProvider ??= ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();

        string mapName = commandInfo[1];

        if (!_mapConfigProvider.TryGetMapConfig(mapName, out var mapConfig))
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "General.Notification.MapNotFound", mapName));
            return;
        }

        string executorName = client?.Name ?? "Console";
        string mapDisplay = _mapConfigProvider.ToolingService.ResolveMapDisplayName(mapConfig);

        var transitionManager = ServiceProvider.GetRequiredService<IMcsInternalMapTransitionManager>();
        transitionManager.TrySetNextMap(mapConfig);

        PrintLocalizedChatToAll("MapCycle.Broadcast.Admin.ChangingMap", executorName, mapDisplay);

        Logger.LogInformation("Admin {Executor} changing map to {Map} (Workshop ID: {WorkshopId})",
            executorName, mapConfig.MapName, mapConfig.WorkshopId);

        transitionManager.BeginMapTransition(MapTransitionTrigger.AdminForceEnd);
    }
}
