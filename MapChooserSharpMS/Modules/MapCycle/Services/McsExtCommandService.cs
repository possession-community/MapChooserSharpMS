using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.MapCycle;
using McsCancellableEvent = MapChooserSharpMS.Shared.Events.McsCancellableEvent;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.MapCycle;
using MapChooserSharpMS.Shared.MapVote;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.MapCycle.Services;

internal enum McsExtCommandResult
{
    /// <summary>
    /// Participation registered; threshold not reached yet.
    /// </summary>
    Added = 0,

    /// <summary>
    /// Threshold reached and the map got extended.
    /// </summary>
    Extended,

    /// <summary>
    /// This player already participated.
    /// </summary>
    AlreadyParticipating,

    /// <summary>
    /// No !ext extends left for this map (MaxExtCommandUses exhausted).
    /// </summary>
    NoUsesLeft,

    /// <summary>
    /// A vote is in progress or next map is already confirmed.
    /// </summary>
    NotAvailable,

    /// <summary>
    /// Blocked by an external event listener (OnExtCommandExecute).
    /// </summary>
    CancelledByListener,

    /// <summary>
    /// Threshold reached but the extend failed (e.g. no active time limit).
    /// </summary>
    FailedToExtend,
}

/// <summary>
/// RTV-style participation counting for the player-driven !ext extend.
/// When the participation ratio reaches mcs_ext_user_vote_threshold,
/// the map is extended immediately (no secondary vote).
/// </summary>
internal sealed class McsExtCommandService
{
    private readonly TnmsPlugin _plugin;
    private readonly PluginModuleBase _moduleBase;
    private readonly ILogger _logger;
    private readonly IInternalEventManager _eventManager;
    private readonly IMcsInternalMapExtendService _extendService;
    private readonly IMcsReadOnlyVoteState _voteState;
    private readonly MapCycleConVars _conVars;

    private readonly HashSet<int> _participants = new();

    internal McsExtCommandService(
        TnmsPlugin plugin,
        PluginModuleBase moduleBase,
        ILogger logger,
        IInternalEventManager eventManager,
        IMcsInternalMapExtendService extendService,
        IMcsReadOnlyVoteState voteState,
        MapCycleConVars conVars)
    {
        _plugin = plugin;
        _moduleBase = moduleBase;
        _logger = logger;
        _eventManager = eventManager;
        _extendService = extendService;
        _voteState = voteState;
        _conVars = conVars;
    }

    internal bool IsEnabled { get; set; } = true;

    public int CurrentExtVotes => _participants.Count;

    /// <summary>
    /// Number of distinct real-player !ext casts needed to extend.
    /// Computed live from current headcount × mcs_ext_user_vote_threshold,
    /// ceil-rounded, always at least 1. Bots / HLTV are excluded.
    /// </summary>
    public int RequiredExtVotes
    {
        get
        {
            int realPlayers = _plugin.SharedSystem.GetModSharp().GetIServer()
                .GetGameClients(true)
                .Count(c => !c.IsFakeClient && !c.IsHltv);

            float ratio = _conVars.ExtUserVoteThreshold.GetFloat();
            return Math.Max((int)Math.Ceiling(realPlayers * ratio), 1);
        }
    }

    public McsExtCommandResult AddParticipant(IGameClient client, StringCommand command)
    {
        if (!IsEnabled)
            return McsExtCommandResult.NotAvailable;

        // Block only while a vote is actually running (Extend is on the
        // ballot there) — result states like NextMapConfirmed don't block
        // !ext, same independence as the extend vote.
        if (_voteState.IsVotingPeriod()
            || !_extendService.CanExtendNow)
        {
            return McsExtCommandResult.NotAvailable;
        }

        if (_extendService.ExtCommandUsesLeft <= 0)
            return McsExtCommandResult.NoUsesLeft;

        if (_participants.Contains(client.Slot))
            return McsExtCommandResult.AlreadyParticipating;

        var @params = new ExtCommandExecuteParams(
            _plugin, _moduleBase, client, command, RequiredExtVotes, CurrentExtVotes);
        if (_eventManager.FireCancellable<IMapCycleEventListener>(
                e => e.OnExtCommandExecute(@params)) == McsCancellableEvent.Stop)
            return McsExtCommandResult.CancelledByListener;

        _participants.Add(client.Slot);

        int current = CurrentExtVotes;
        int required = RequiredExtVotes;

        if (current < required)
        {
            BroadcastProgress(client, current, required);
            return McsExtCommandResult.Added;
        }

        var result = _extendService.TryExtend(McsExtendTrigger.ExtCommand);
        if (result != McsMapExtendResult.Extended)
        {
            // Keep accumulated participation — the next cast retries.
            _logger.LogWarning("[MapCycle] !ext threshold reached but extend failed: {Result}", result);
            return McsExtCommandResult.FailedToExtend;
        }

        _participants.Clear();
        return McsExtCommandResult.Extended;
    }

    public void RemoveParticipant(int slot)
    {
        _participants.Remove(slot);
    }

    public void ClearParticipants()
    {
        _participants.Clear();
    }

    private void BroadcastProgress(IGameClient caster, int current, int required)
    {
        var clients = _plugin.SharedSystem.GetModSharp().GetIServer().GetGameClients(true);
        foreach (var client in clients)
        {
            if (client.IsFakeClient || client.IsHltv)
                continue;

            client.GetPlayerController()?.PrintToChat(
                _plugin.LocalizeStringForPlayer(
                    client, "MapCycle.ExtCommand.Notification.Progress",
                    caster.Name, current, required));
        }
    }
}
