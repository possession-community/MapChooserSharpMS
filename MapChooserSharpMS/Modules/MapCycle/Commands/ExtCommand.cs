using System;
using MapChooserSharpMS.Modules.MapCycle.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Extensions.Client;

namespace MapChooserSharpMS.Modules.MapCycle.Commands;

internal sealed class ExtCommand(IServiceProvider provider) : TnmsAbstractCommandBase(provider)
{
    public override string CommandName => "ext";
    public override string CommandDescription => "Vote to extend the current map";
    public override TnmsCommandRegistrationType CommandRegistrationType => TnmsCommandRegistrationType.Client;

    private McsExtCommandService _extCommandService = null!;

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        if (client is null)
            return;

        _extCommandService ??= ServiceProvider.GetRequiredService<McsExtCommandService>();

        var result = _extCommandService.AddParticipant(client, commandInfo);

        switch (result)
        {
            case McsExtCommandResult.Added:
            case McsExtCommandResult.Extended:
                // Progress / extend announcements are broadcast by the services.
                break;

            case McsExtCommandResult.AlreadyParticipating:
                client.GetPlayerController()?.PrintToChat(
                    LocalizeWithPluginPrefix(client, "MapCycle.ExtCommand.Notification.AlreadyParticipating"));
                break;

            case McsExtCommandResult.NoUsesLeft:
                client.GetPlayerController()?.PrintToChat(
                    LocalizeWithPluginPrefix(client, "MapCycle.ExtCommand.Notification.NoUsesLeft"));
                break;

            case McsExtCommandResult.NotAvailable:
                client.GetPlayerController()?.PrintToChat(
                    LocalizeWithPluginPrefix(client, "MapCycle.ExtCommand.Notification.NotAvailable"));
                break;

            case McsExtCommandResult.CancelledByListener:
                // External listener rejected the cast; stay silent like RTV does.
                break;

            case McsExtCommandResult.FailedToExtend:
                client.GetPlayerController()?.PrintToChat(
                    LocalizeWithPluginPrefix(client, "MapCycle.ExtCommand.Notification.FailedToExtend"));
                break;
        }
    }
}
