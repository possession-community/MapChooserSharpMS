using System;
using MapChooserSharpMS.Shared.RockTheVote.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using MapChooserSharpMS.Modules.Commands;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;

namespace MapChooserSharpMS.Modules.RockTheVote.Commands;

internal sealed class ForceRtvCommand(IServiceProvider provider) : McsCommandBase(provider)
{
    public override string CommandName => "forcertv";
    public override string CommandDescription => "Admin: force an RTV vote immediately";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IRtvService _rtvService = null!;

    protected override ICommandValidator? GetValidator()
        => new PermissionValidator("mcs.admin.command.rtv.forcertv");

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _rtvService ??= ServiceProvider.GetRequiredService<IRtvService>();
        _rtvService.InitiateForceRtvVote(client);
    }
}
