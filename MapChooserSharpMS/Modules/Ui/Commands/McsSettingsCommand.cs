using System;
using System.Collections.Generic;
using MapChooserSharp.Modules.MapVote.Countdown;
using MapChooserSharpMS.Modules.Commands;
using MapChooserSharpMS.Modules.Ui.Countdown;
using MapChooserSharpMS.Modules.Ui.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Command;

namespace MapChooserSharpMS.Modules.Ui.Commands;

internal sealed class McsSettingsCommand(IServiceProvider provider) : McsCommandBase(provider)
{
    public override string CommandName => "mcs_settings";
    public override List<string> CommandAliases => ["mcss"];
    public override string CommandDescription => "Change your MCS display preferences";
    public override TnmsCommandRegistrationType CommandRegistrationType => TnmsCommandRegistrationType.Client;

    private McsPlayerPreferenceService? _preferenceService;

    protected override string GetUsageTranslationKey() => "Ui.Command.Settings.Usage";

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        if (client is null) return;

        _preferenceService ??= ServiceProvider.GetRequiredService<McsCountdownUiController>().PreferenceService;

        if (commandInfo.ArgCount < 1)
        {
            ShowCurrentSettings(client);
            return;
        }

        string subCommand = commandInfo[1].ToLowerInvariant();

        switch (subCommand)
        {
            case "volume":
            case "vol":
                HandleVolume(client, commandInfo);
                break;
            case "countdown":
            case "cd":
                HandleCountdown(client, commandInfo);
                break;
            default:
                client.GetPlayerController()?.PrintToChat(
                    LocalizeWithPluginPrefix(client, "Ui.Command.Settings.Usage"));
                break;
        }
    }

    private void ShowCurrentSettings(IGameClient client)
    {
        var uiType = _preferenceService!.GetCountdownUiType(client.Slot);
        float volume = _preferenceService.GetVolume(client.Slot);
        int volumePercent = (int)(volume * 100f);

        client.GetPlayerController()?.PrintToChat(
            LocalizeWithPluginPrefix(client, "Ui.Command.Settings.Current", uiType, volumePercent));
    }

    private void HandleVolume(IGameClient client, StringCommand commandInfo)
    {
        if (commandInfo.ArgCount < 2)
        {
            float current = _preferenceService!.GetVolume(client.Slot);
            int currentPercent = (int)(current * 100f);
            client.GetPlayerController()?.PrintToChat(
                LocalizeWithPluginPrefix(client, "Ui.Command.Settings.Volume.Current", currentPercent));
            return;
        }

        if (!int.TryParse(commandInfo[2], out int percent) || percent < 0 || percent > 100)
        {
            client.GetPlayerController()?.PrintToChat(
                LocalizeWithPluginPrefix(client, "Ui.Command.Settings.Volume.InvalidRange"));
            return;
        }

        float volume = percent / 100f;
        _preferenceService!.SetVolume(client, volume);

        client.GetPlayerController()?.PrintToChat(
            LocalizeWithPluginPrefix(client, "Ui.Command.Settings.Volume.Set", percent));
    }

    private void HandleCountdown(IGameClient client, StringCommand commandInfo)
    {
        if (commandInfo.ArgCount < 2)
        {
            var current = _preferenceService!.GetCountdownUiType(client.Slot);
            client.GetPlayerController()?.PrintToChat(
                LocalizeWithPluginPrefix(client, "Ui.Command.Settings.Countdown.Current", current));
            client.GetPlayerController()?.PrintToChat(
                LocalizeWithPluginPrefix(client, "Ui.Command.Settings.Countdown.Available"));
            return;
        }

        if (!Enum.TryParse<McsCountdownUiType>(commandInfo[2], true, out var uiType)
            || uiType == McsCountdownUiType.None)
        {
            client.GetPlayerController()?.PrintToChat(
                LocalizeWithPluginPrefix(client, "Ui.Command.Settings.Countdown.Invalid"));
            client.GetPlayerController()?.PrintToChat(
                LocalizeWithPluginPrefix(client, "Ui.Command.Settings.Countdown.Available"));
            return;
        }

        _preferenceService!.SetCountdownUiType(client, uiType);

        client.GetPlayerController()?.PrintToChat(
            LocalizeWithPluginPrefix(client, "Ui.Command.Settings.Countdown.Set", uiType));
    }
}
