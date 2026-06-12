using System;
using MapChooserSharpMS.Shared.MapCycle;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;

namespace MapChooserSharpMS.Modules.MapCycle.Commands;

internal sealed class DisableExtCommand(IServiceProvider provider) : TnmsAbstractCommandBase(provider)
{
    public override string CommandName => "disableext";
    public override string CommandDescription => "Admin: disable the !ext command";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMapCycleExtendController _extendController = null!;

    protected override ICommandValidator? GetValidator()
        => new PermissionValidator("mcs.admin.command.mapcycle.disableext");

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _extendController ??= ServiceProvider.GetRequiredService<IMapCycleExtendController>();
        _extendController.DisableExt();

        PrintMessageToServerOrPlayerChat(client,
            LocalizeWithPluginPrefix(client, "MapCycle.ExtCommand.Notification.Disabled"));
    }
}
