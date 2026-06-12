using System;
using MapChooserSharpMS.Shared.RockTheVote.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;

namespace MapChooserSharpMS.Modules.RockTheVote.Commands;

internal sealed class EnableRtvCommand(IServiceProvider provider) : TnmsAbstractCommandBase(provider)
{
    public override string CommandName => "enablertv";
    public override string CommandDescription => "Admin: enable the RTV command";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IRtvService _rtvService = null!;

    protected override ICommandValidator? GetValidator()
        => new PermissionValidator("mcs.admin.command.rtv.enablertv");

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _rtvService ??= ServiceProvider.GetRequiredService<IRtvService>();
        _rtvService.EnableRtvCommand(client);
    }
}
