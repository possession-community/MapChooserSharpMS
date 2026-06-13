using System;
using MapChooserSharpMS.Modules.Commands;
using MapChooserSharpMS.Shared.MapCycle;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;
using TnmsPluginFoundation.Models.Command.Validators.RangedValidators;

namespace MapChooserSharpMS.Modules.MapCycle.Commands;

internal sealed class ExtendMapCommand(IServiceProvider provider) : McsCommandBase(provider)
{
    public override string CommandName => "extend";
    public override string CommandDescription => "Admin: extend the current map by specified minutes/rounds";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMapCycleExtendController _extendController = null!;

    protected override ICommandValidator? GetValidator()
        => new CompositeValidator()
            .Add(new PermissionValidator("mcs.admin.command.mapcycle.extend"))
            .Add(new ArgumentCountValidator(1))
            .Add(new RangedArgumentValidator<int>(int.MinValue, int.MaxValue, 1));

    protected override string GetUsageTranslationKey() => "MapCycle.Extend.Usage";

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _extendController ??= ServiceProvider.GetRequiredService<IMapCycleExtendController>();

        var amount = validatedArguments!.GetArgument<int>(1);
        var result = _extendController.TryExtendCurrentMap(amount);

        if (result != McsMapExtendResult.Extended)
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, $"MapCycle.Extend.Notification.{result}"));
        }
    }
}
