using System;
using System.Collections.Generic;
using MapChooserSharpMS.Modules.Commands;
using MapChooserSharpMS.Shared.MapCycle;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;
using TnmsPluginFoundation.Models.Command.Validators.RangedValidators;

namespace MapChooserSharpMS.Modules.MapCycle.Commands;

internal sealed class VoteExtendCommand(IServiceProvider provider) : McsCommandBase(provider)
{
    public override string CommandName => "voteextend";
    public override List<string> CommandAliases => ["ve"];
    public override string CommandDescription => "Admin: start a native yes/no vote to extend the current map";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMapCycleExtendController _extendController = null!;

    protected override ICommandValidator? GetValidator()
        => new CompositeValidator()
            .Add(new PermissionValidator("mcs.admin.command.mapcycle.voteextend"))
            .Add(new ArgumentCountValidator(1))
            .Add(new RangedArgumentValidator<int>(1, 120, 1, true));

    protected override string GetUsageTranslationKey() => "MapCycle.ExtendVote.Usage";

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _extendController ??= ServiceProvider.GetRequiredService<IMapCycleExtendController>();
        var minutes = validatedArguments!.GetArgument<int>(1);
        var result = _extendController.StartExtendVote(client, minutes);

        PrintMessageToServerOrPlayerChat(client,
            LocalizeWithPluginPrefix(client, $"MapCycle.ExtendVote.Notification.{result}"));
    }
}
