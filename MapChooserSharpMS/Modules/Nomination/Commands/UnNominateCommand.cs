using System;
using System.Collections.Generic;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Shared.Nomination.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.Nomination.Commands;

internal sealed class UnNominateCommand(IServiceProvider provider) : TnmsAbstractCommandBase(provider)
{
    public override string CommandName => "unnominate";
    public override List<string> CommandAliases => ["unnom"];
    public override string CommandDescription => "Remove your nomination";
    public override TnmsCommandRegistrationType CommandRegistrationType => TnmsCommandRegistrationType.Client;

    private IMapNominationService _nominationService = null!;
    private PluginModuleBase _module = null!;

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        if (client is null) return;

        _nominationService ??= ServiceProvider.GetRequiredService<IMapNominationService>();
        _module ??= (PluginModuleBase)ServiceProvider.GetRequiredService<IMcsInternalNominationController>();

        bool success = _nominationService.TryUnNominate(client);
        string message = LocalizeString(client, success
            ? "Nomination.Notification.UnNominate.Success"
            : "Nomination.Notification.UnNominate.NotParticipating");

        client.GetPlayerController()?.PrintToChat(_module.GetTextWithModulePrefix(client, message));
    }
}
