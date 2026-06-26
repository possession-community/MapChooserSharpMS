using System;
using MapChooserSharpMS.Shared.MapCycle;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Command;

namespace MapChooserSharpMS.Modules.MapCycle.Commands;

internal sealed class CurrentMapCommand(IServiceProvider provider) : TnmsAbstractCommandBase(provider)
{
    internal const string ChatTrigger = "currentmap";

    public override string CommandName => ChatTrigger;
    public override string CommandDescription => "Show the current map";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMapCycleController _controller = null!;

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _controller ??= ServiceProvider.GetRequiredService<IMapCycleController>();

        var currentMap = _controller.MapTransitionManager.CurrentMap;
        string mapDisplay = currentMap?.MapConfig.MapName
            ?? Plugin.SharedSystem.GetModSharp().GetMapName()
            ?? "Unknown";

        PrintMessageToServerOrPlayerChat(client,
            LocalizeWithPluginPrefix(client, "MapCycle.Command.Notification.CurrentMap", mapDisplay));
    }
}
