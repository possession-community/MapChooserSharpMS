using System;
using MapChooserSharpMS.Shared.MapCycle;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Command;

namespace MapChooserSharpMS.Modules.MapCycle.Commands;

internal sealed class NextMapCommand(IServiceProvider provider) : TnmsAbstractCommandBase(provider)
{
    internal const string ChatTrigger = "nextmap";

    public override string CommandName => ChatTrigger;
    public override string CommandDescription => "Show the next map";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMapCycleController _controller = null!;

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _controller ??= ServiceProvider.GetRequiredService<IMapCycleController>();

        var nextMap = _controller.MapTransitionManager.NextMap;
        string mapDisplay = nextMap?.MapName ?? "Pending vote";

        PrintMessageToServerOrPlayerChat(client,
            LocalizeWithPluginPrefix(client, "MapCycle.Command.Notification.NextMap", mapDisplay));
    }
}
