using System;
using MapChooserSharp.Modules.MapVote.Countdown;
using MapChooserSharpMS.Modules.Ui.Countdown.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Extensions.Client;

namespace MapChooserSharpMS.Modules.Ui.Countdown;

public class ChatCountdownUi(IServiceProvider provider): IMcsCountdownUi
{
    private readonly TnmsPlugin _plugin = provider.GetRequiredService<TnmsPlugin>();

    private bool _isFirstNotificationNotified;

    public void ShowCountdownToPlayer(IGameClient client, int secondsLeft, McsCountdownType countdownType)
    {
        switch (countdownType)
        {
            case McsCountdownType.VoteStart:
                if (!_isFirstNotificationNotified || secondsLeft <= 10)
                {
                    client.GetPlayerController()?.PrintToChat(
                        $" {_plugin.GetPluginPrefix(client)} {_plugin.LocalizeStringForPlayer(client, "MapVote.Broadcast.Countdown", secondsLeft)}");
                    _isFirstNotificationNotified = true;
                }
                break;

            case McsCountdownType.Voting:
                client.GetPlayerController()?.PrintToChat(
                    $" {_plugin.GetPluginPrefix(client)} {_plugin.LocalizeStringForPlayer(client, "MapVote.Broadcast.Voting.VoteEndCountdown", secondsLeft)}");
                break;
        }
    }

    public void Close(IGameClient client)
    {
        _isFirstNotificationNotified = false;
    }
}