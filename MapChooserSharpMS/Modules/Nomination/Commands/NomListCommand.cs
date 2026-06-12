using System;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapConfig.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Extensions.Client;

namespace MapChooserSharpMS.Modules.Nomination.Commands;

internal sealed class NomListCommand(IServiceProvider provider) : TnmsAbstractCommandBase(provider)
{
    public override string CommandName => "nomlist";
    public override string CommandDescription => "Shows current nomination list";
    public override TnmsCommandRegistrationType CommandRegistrationType => TnmsCommandRegistrationType.Client;

    private IMcsInternalNominationController _controller = null!;
    private IMapConfigToolingService _toolingService = null!;

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        if (client is null) return;

        _controller ??= ServiceProvider.GetRequiredService<IMcsInternalNominationController>();
        _toolingService ??= ServiceProvider.GetRequiredService<IMcsMapConfigProvider>().ToolingService;

        var nominations = _controller.NominationManager.NominatedMaps;
        if (nominations.Count == 0)
        {
            client.GetPlayerController()?.PrintToChat(
                LocalizeWithPluginPrefix(client, "NominationList.Command.Notification.ThereIsNoNomination"));
            return;
        }

        client.GetPlayerController()?.PrintToChat(
            LocalizeWithPluginPrefix(client, "NominationList.Command.Notification.ListHeader"));

        int index = 1;
        foreach (var (_, nomination) in nominations)
        {
            string mapDisplay = _toolingService.ResolveMapDisplayName(nomination.MapConfig);
            string info;
            if (nomination.IsForceNominated)
                info = LocalizeString(client, "NominationList.Command.Notification.AdminNomination");
            else
                info = nomination.NominationParticipants.Count.ToString();

            client.GetPlayerController()?.PrintToChat(
                GetTextWithPluginPrefix(client,
                    LocalizeString(client, "NominationList.Command.Notification.Content", index, mapDisplay, info)));
            index++;
        }
    }

}
