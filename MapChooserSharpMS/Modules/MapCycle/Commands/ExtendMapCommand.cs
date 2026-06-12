using System;
using MapChooserSharpMS.Shared.MapCycle;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;

namespace MapChooserSharpMS.Modules.MapCycle.Commands;

internal sealed class ExtendMapCommand(IServiceProvider provider) : TnmsAbstractCommandBase(provider)
{
    public override string CommandName => "extend";
    public override string CommandDescription => "Admin: extend the current map by the configured amount";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMapCycleExtendController _extendController = null!;

    protected override ICommandValidator? GetValidator()
        => new PermissionValidator("mcs.admin.command.mapcycle.extend");

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _extendController ??= ServiceProvider.GetRequiredService<IMapCycleExtendController>();

        var result = _extendController.TryExtendCurrentMap();

        PrintMessageToServerOrPlayerChat(client,
            LocalizeWithPluginPrefix(client, $"MapCycle.Extend.Notification.{result}"));
    }
}
