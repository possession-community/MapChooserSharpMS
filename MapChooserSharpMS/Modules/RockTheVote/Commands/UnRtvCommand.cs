using System;
using MapChooserSharpMS.Shared.RockTheVote.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Command;

namespace MapChooserSharpMS.Modules.RockTheVote.Commands;

internal sealed class UnRtvCommand(IServiceProvider provider) : TnmsAbstractCommandBase(provider)
{
    public override string CommandName => "unrtv";
    public override string CommandDescription => "Withdraw your RTV vote";
    public override TnmsCommandRegistrationType CommandRegistrationType => TnmsCommandRegistrationType.Client;

    private IRtvService _rtvService = null!;

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        if (client is null)
            return;

        _rtvService ??= ServiceProvider.GetRequiredService<IRtvService>();

        bool removed = _rtvService.RemoveClientFromRtv(client);

        client.GetPlayerController()?.PrintToChat(
            LocalizeWithPluginPrefix(client,
                removed
                    ? "Rtv.Notification.Unrtv.Success"
                    : "Rtv.Notification.Unrtv.NotParticipating"));
    }
}
