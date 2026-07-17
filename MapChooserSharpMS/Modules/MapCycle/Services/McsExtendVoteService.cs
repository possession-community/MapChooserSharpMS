using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.MapCycle;
using MapChooserSharpMS.Modules.MapCycle.Handlers;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;
using MapChooserSharpMS.Modules.MapVote.Interfaces;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle;
using MapChooserSharpMS.Shared.MapVote;
using Microsoft.Extensions.Logging;
using NativeVoteManagerMS.Shared;
using NativeVoteManagerMS.Shared.Types;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.MapCycle.Services;

/// <summary>
/// Admin-triggered extend vote (!ve / !voteextend) backed by NativeVoteManager's
/// yes/no native vote. On pass, extends via <see cref="IMcsInternalMapExtendService"/>
/// through the admin path — no extend budget is consumed.
/// Writes to <see cref="IMcsInternalExtendVoteState"/> so that
/// <c>IsVotingPeriod()</c> on the combined state manager returns true while
/// an admin extend vote is active.
/// </summary>
internal sealed class McsExtendVoteService
{
    private readonly TnmsPlugin _plugin;
    private readonly PluginModuleBase _moduleBase;
    private readonly ILogger _logger;
    private readonly IInternalEventManager _eventManager;
    private readonly IMcsInternalMapExtendService _extendService;
    private readonly IMcsInternalExtendVoteState _extendVoteState;
    private readonly MapCycleConVars _conVars;
    private readonly System.Func<IMapConfig?> _currentMapProvider;

    /// <summary>
    /// Resolved by the controller in OnAllModulesLoaded.
    /// </summary>
    internal INativeVoteManager? NativeVoteManager { get; set; }

    // Incremented every time a vote session ends — handler callbacks carry the
    // generation they were created with, so callbacks from an already-finished
    // session are ignored.
    private int _generation;
    private int? _pendingOverrideAmount;

    public bool IsExtendVoteInProgress { get; private set; }

    internal McsExtendVoteService(
        TnmsPlugin plugin,
        PluginModuleBase moduleBase,
        ILogger logger,
        IInternalEventManager eventManager,
        IMcsInternalMapExtendService extendService,
        IMcsInternalExtendVoteState extendVoteState,
        MapCycleConVars conVars,
        System.Func<IMapConfig?> currentMapProvider)
    {
        _plugin = plugin;
        _moduleBase = moduleBase;
        _logger = logger;
        _eventManager = eventManager;
        _extendService = extendService;
        _extendVoteState = extendVoteState;
        _conVars = conVars;
        _currentMapProvider = currentMapProvider;
    }

    public McsExtendVoteStartResult StartExtendVote(IGameClient? initiator, int? overrideAmount = null)
    {
        if (NativeVoteManager is null)
        {
            _logger.LogWarning("[MapCycle] Extend vote requested but NativeVoteManager is not available yet");
            return McsExtendVoteStartResult.FailedToInitiateNativeVote;
        }

        if (IsExtendVoteInProgress)
            return McsExtendVoteStartResult.ExtendVoteAlreadyInProgress;

        // Gate only on NVM's own vote state — map-vote result states
        // (e.g. NextMapConfirmed) are independent and must not block an
        // extend vote (old MCS behaviour: !ve works after next map is decided).
        if (NativeVoteManager.IsAnyVoteInProgress)
        {
            return McsExtendVoteStartResult.AnotherVoteInProgress;
        }

        if (!_extendService.CanExtendNow)
            return McsExtendVoteStartResult.TimeLimitNotActive;

        float duration = _conVars.VoteExtendVoteTime.GetFloat();
        float threshold = _conVars.VoteExtendSuccessThreshold.GetFloat();

        var handler = new ExtendVoteNativeHandler(this, _generation);
        int displayAmount = overrideAmount ?? 0;
        var options = new YesNoVoteOptions
        {
            Title = "#SFUI_vote_passed_nextlevel_extend",
            Description = LocalizedString.From(c =>
                _plugin.Localizer.ForCulture("MapCycle.ExtendVote.DetailsString", c ?? System.Globalization.CultureInfo.CurrentCulture, displayAmount)),
            VoteDuration = duration,
            VoteHandler = handler,
            VoteInitiator = initiator?.Slot ?? 99,
            PassCondition = VotePassConditions.Default(threshold),
            Participants = GetParticipants(),
        };

        var result = NativeVoteManager.InitiateYesNoVote(options);
        if (result != VoteInitiateResult.Success)
        {
            _logger.LogWarning("[MapCycle] Failed to initiate extend vote: {Result}", result);
            return McsExtendVoteStartResult.FailedToInitiateNativeVote;
        }

        IsExtendVoteInProgress = true;
        _pendingOverrideAmount = overrideAmount;
        _extendVoteState.SetState(McsMapVoteState.Voting);

        var startedParams = new ExtendVoteStartedParams(
            _plugin, _moduleBase, _currentMapProvider(), initiator, duration);
        _eventManager.Fire<IMapCycleEventListener>(e => e.OnExtendVoteStarted(startedParams));

        return McsExtendVoteStartResult.Started;
    }

    public bool CancelExtendVote(IGameClient? canceller)
    {
        if (!IsExtendVoteInProgress)
            return false;

        // Finish the local session BEFORE asking NVM to cancel — NVM invokes
        // OnVoteCancelled synchronously and the generation bump makes that
        // callback stale, preventing a double-fired cancelled event.
        FinishVoteSession();

        NativeVoteManager?.CancelVote();

        FireCancelledEvent(canceller);
        return true;
    }

    /// <summary>
    /// Clears local vote state on map deactivation without touching NVM
    /// (its vote dies with the map anyway). Fires the cancelled event so
    /// listeners pairing Started/Cancelled/Finished get closure.
    /// </summary>
    public void ResetOnMapEnd()
    {
        if (!IsExtendVoteInProgress)
            return;

        FinishVoteSession();
        FireCancelledEvent(null);
    }

    internal void HandleVotePassed(int generation, VoteResult result)
    {
        if (IsStale(generation))
            return;

        var overrideAmount = _pendingOverrideAmount;
        FinishVoteSession();

        var extendResult = _extendService.TryExtend(McsExtendTrigger.AdminOrApi, overrideAmount);
        if (extendResult != McsMapExtendResult.Extended)
            _logger.LogWarning("[MapCycle] Extend vote passed but extend failed: {Result}", extendResult);

        BroadcastToAll("MapCycle.ExtendVote.Broadcast.Passed");
        FireFinishedEvent(passed: true, result);
    }

    internal void HandleVoteFailed(int generation, VoteResult result)
    {
        if (IsStale(generation))
            return;

        _logger.LogInformation("[MapCycle] Extend vote failed");
        FinishVoteSession();

        BroadcastToAll("MapCycle.ExtendVote.Broadcast.Failed");
        FireFinishedEvent(passed: false, result);
    }

    private void BroadcastToAll(string key, params object[] args)
    {
        var clients = _plugin.SharedSystem.GetModSharp().GetIServer().GetGameClients(true);
        foreach (var c in clients)
        {
            if (c.IsFakeClient || c.IsHltv) continue;
            c.GetPlayerController()?.PrintToChat(
                $" {_plugin.GetPluginPrefix(c)} {_plugin.LocalizeStringForPlayer(c, key, args)}");
        }
    }

    internal void HandleVoteCancelled(int generation)
    {
        if (IsStale(generation))
            return;

        // External cancellation (not via CancelExtendVote — that path is stale here).
        _logger.LogInformation("[MapCycle] Extend vote cancelled externally");
        FinishVoteSession();
        FireCancelledEvent(null);
    }

    private bool IsStale(int generation)
        => generation != _generation || !IsExtendVoteInProgress;

    private void FinishVoteSession()
    {
        IsExtendVoteInProgress = false;
        _pendingOverrideAmount = null;
        _generation++;
        _extendVoteState.Reset();
    }

    private void FireCancelledEvent(IGameClient? cancelledBy)
    {
        var cancelledParams = new ExtendVoteCancelledParams(
            _plugin, _moduleBase, _currentMapProvider(), cancelledBy);
        _eventManager.Fire<IMapCycleEventListener>(e => e.OnExtendVoteCancelled(cancelledParams));
    }

    private void FireFinishedEvent(bool passed, VoteResult result)
    {
        var (yesCount, noCount) = CountVotes(result);
        var finishedParams = new ExtendVoteFinishedParams(
            _plugin, _moduleBase, _currentMapProvider(), passed, yesCount, noCount);
        _eventManager.Fire<IMapCycleEventListener>(e => e.OnExtendVoteFinished(finishedParams));
    }

    private static (int YesCount, int NoCount) CountVotes(VoteResult result)
    {
        int yesCount = 0;
        int noCount = 0;

        foreach (var choice in result.Choices)
        {
            if (string.Equals(choice.Content.InternalName, "yes", System.StringComparison.OrdinalIgnoreCase))
                yesCount = choice.Voters.Count;
            else if (string.Equals(choice.Content.InternalName, "no", System.StringComparison.OrdinalIgnoreCase))
                noCount = choice.Voters.Count;
        }

        return (yesCount, noCount);
    }

    private List<IGameClient> GetParticipants()
    {
        return _plugin.SharedSystem.GetModSharp().GetIServer()
            .GetGameClients(true)
            .Where(c => !c.IsFakeClient && !c.IsHltv)
            .ToList();
    }
}
