using System;
using System.Text;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapConfig.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;
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

        bool isVerbose = commandInfo.ArgCount >= 1
                         && commandInfo[1].Equals("full", StringComparison.OrdinalIgnoreCase)
                         && TnmsPluginFoundation.TnmsPlugin.AdminManager.PlayerHasPermission(client.SteamId, "mcs.admin.command.nomination.nomlist.verbose");

        client.GetPlayerController()?.PrintToChat(
            LocalizeWithPluginPrefix(client, "NominationList.Command.Notification.ListHeader"));

        int index = 1;
        foreach (var (_, nomination) in nominations)
        {
            string mapDisplay = _toolingService.ResolveMapDisplayName(nomination.MapConfig);

            string info;
            if (nomination.IsForceNominated)
            {
                info = Plugin.LocalizeStringForPlayer(client, "NominationList.Command.Notification.AdminNomination");
            }
            else if (isVerbose)
            {
                info = BuildVerboseNominators(nomination);
            }
            else
            {
                info = Plugin.LocalizeStringForPlayer(client, "NominationList.Command.Notification.VoteCount", nomination.NominationParticipants.Count);
            }

            client.GetPlayerController()?.PrintToChat(
                $" {Plugin.GetPluginPrefix(client)} {index}: {mapDisplay} | {info}");
            index++;
        }
    }

    private string BuildVerboseNominators(Shared.Nomination.IMcsNominationData nomination)
    {
        var sb = new StringBuilder();
        var clientManager = SharedSystem.GetClientManager();

        foreach (int slot in nomination.NominationParticipants)
        {
            IGameClient? participant;
            try
            {
                participant = clientManager.GetGameClient(new PlayerSlot(slot));
            }
            catch
            {
                continue;
            }
            if (participant is null) continue;

            if (sb.Length > 0)
                sb.Append(", ");
            sb.Append(participant.Name);
        }

        return sb.Length > 0 ? sb.ToString() : "-";
    }
}
