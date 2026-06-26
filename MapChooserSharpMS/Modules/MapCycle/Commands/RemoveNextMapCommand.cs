using System;
using MapChooserSharpMS.Modules.Commands;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;

namespace MapChooserSharpMS.Modules.MapCycle.Commands;

internal sealed class RemoveNextMapCommand(IServiceProvider provider) : McsCommandBase(provider)
{
    public override string CommandName => "removenextmap";
    public override string CommandDescription => "Admin: remove the confirmed next map";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMapCycleController _controller = null!;
    private IMcsMapConfigProvider _mapConfigProvider = null!;

    protected override ICommandValidator? GetValidator()
        => new PermissionValidator("mcs.admin.command.mapcycle.removenextmap");

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _controller ??= ServiceProvider.GetRequiredService<IMapCycleController>();
        _mapConfigProvider ??= ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();

        var transitionManager = _controller.MapTransitionManager;
        var nextMap = transitionManager.NextMap;

        if (nextMap is null)
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "MapCycle.Command.Admin.RemoveNextMap.NotSet"));
            return;
        }

        string mapDisplay = _mapConfigProvider.ToolingService.ResolveMapDisplayName(nextMap.MapConfig);
        string mapActualName = nextMap.MapConfig.MapName;

        if (!transitionManager.TryRemoveNextMap())
            return;

        string executorName = client?.Name ?? "Console";

        PrintLocalizedChatToAll("MapCycle.Broadcast.Admin.RemoveNextMap", executorName, mapDisplay);
        Logger.LogInformation("Admin {Executor} removed {Map} from next map", executorName, mapActualName);
    }
}
