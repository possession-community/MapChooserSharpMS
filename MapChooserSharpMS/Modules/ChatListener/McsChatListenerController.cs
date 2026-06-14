using System;
using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapVote;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.ChatListener;

internal sealed class McsChatListenerController : PluginModuleBase, IClientListener
{
    public override string PluginModuleName => "McsChatListenerController";
    public override string ModuleChatPrefix => "Prefix.ChatListener";
    protected override bool UseTranslationKeyInModuleChatPrefix => true;

    public int ListenerVersion => 1;
    public int ListenerPriority => 0;

    private const string CommandPrefix = "ms_";

    private readonly Dictionary<string, string> _triggers = new(StringComparer.OrdinalIgnoreCase);

    private readonly IConVar _blockChatDuringVote;
    private IMcsReadOnlyVoteState _voteState = null!;

    public McsChatListenerController(IServiceProvider serviceProvider, bool hotReload)
        : base(serviceProvider, hotReload)
    {
        _blockChatDuringVote = SharedSystem.GetConVarManager()
            .CreateConVar("mcs_block_chat_during_vote", 0, 0, 1, "Block player chat messages during map vote", ConVarFlags.None)!;
        TrackConVar(_blockChatDuringVote);
    }

    protected override void OnAllModulesLoaded()
    {
        SharedSystem.GetClientManager().InstallClientListener(this);
        _voteState = ServiceProvider.GetRequiredService<IMcsReadOnlyVoteState>();

        RegisterTrigger(RockTheVote.Commands.RtvCommand.ChatTrigger);
        RegisterTrigger(MapCycle.Commands.NextMapCommand.ChatTrigger);
        RegisterTrigger(MapCycle.Commands.TimeLeftCommand.ChatTrigger);
        RegisterTrigger(MapCycle.Commands.CurrentMapCommand.ChatTrigger);
        RegisterTrigger(MapCycle.Commands.TheTimeCommand.ChatTrigger);
    }

    protected override void OnUnloadModule()
    {
        SharedSystem.GetClientManager().RemoveClientListener(this);
    }

    private void RegisterTrigger(string commandName)
    {
        _triggers[commandName] = CommandPrefix + commandName;
    }

    // TODO: FakeCommand does not dispatch to ModSharp's StringCommand callbacks.
    //       Investigate MS-side routing and fix the dispatch method.
    public ECommandAction OnClientSayCommand(
        IGameClient client, bool teamOnly, bool isCommand, string commandName, string message)
    {
        if (isCommand)
            return ECommandAction.Skipped;

        string trimmed = message.Trim();
        if (_triggers.TryGetValue(trimmed, out var fullCommand))
        {
            client.FakeCommand(fullCommand);
            return ECommandAction.Handled;
        }

        if (_blockChatDuringVote.GetInt32() != 0 && _voteState.IsVotingPeriod())
        {
            client.GetPlayerController()?.PrintToChat(
                $" {Plugin.GetPluginPrefix(client)} {Plugin.LocalizeStringForPlayer(client, "Chat.Notification.MutedDuringVote")}");
            return ECommandAction.Handled;
        }

        return ECommandAction.Skipped;
    }
}
