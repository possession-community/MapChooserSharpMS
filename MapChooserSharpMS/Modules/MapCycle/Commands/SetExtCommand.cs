using System;
using MapChooserSharpMS.Shared.MapCycle;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using MapChooserSharpMS.Modules.Commands;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;
using TnmsPluginFoundation.Models.Command.Validators.RangedValidators;

namespace MapChooserSharpMS.Modules.MapCycle.Commands;

internal sealed class SetExtCommand(IServiceProvider provider) : McsCommandBase(provider)
{
    public override string CommandName => "setext";
    public override string CommandDescription => "Admin: set the remaining !ext uses";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMapCycleExtendController _extendController = null!;

    protected override ICommandValidator? GetValidator()
        => new CompositeValidator()
            .Add(new PermissionValidator("mcs.admin.command.mapcycle.setext"))
            .Add(new ArgumentCountValidator(1))
            .Add(new RangedArgumentValidator<int>(0, int.MaxValue, 1));

    protected override string GetUsageTranslationKey() => "MapCycle.ExtCommand.Notification.SetUsage";

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _extendController ??= ServiceProvider.GetRequiredService<IMapCycleExtendController>();

        int count = validatedArguments!.GetArgument<int>(1);

        _extendController.SetExtCommandUsesLeft(count);

        PrintMessageToServerOrPlayerChat(client,
            LocalizeWithPluginPrefix(client, "MapCycle.ExtCommand.Notification.SetSuccess", count));
    }
}
