using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.MapVote;
using MapChooserSharpMS.Modules.MapVote.Handlers;
using MapChooserSharpMS.Modules.MapVote.Interfaces;
using MapChooserSharpMS.Modules.MapVote.Managers;
using MapChooserSharpMS.Modules.MapVote.Models;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Shared.Events.MapVote;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.MapVote.Services;
using MapChooserSharpMS.Shared.Nomination.Managers;
using Microsoft.Extensions.Logging;
using NativeVoteManagerMS.Shared;
using NativeVoteManagerMS.Shared.Types;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.MapVote.Services;

internal sealed class MapVoteControllingService : IMapVoteControllingService
{
    private readonly TnmsPlugin _plugin;
    private readonly PluginModuleBase _moduleBase;
    private readonly ILogger _logger;
    private readonly VoteControllingManager _voteManager;
    private readonly IMcsInternalMainVoteState _voteState;
    private readonly IInternalEventManager _eventManager;
    private readonly INativeVoteManager _nativeVoteManager;
    private readonly MapVoteConVars _conVars;
    private readonly IMcsPluginConfigProvider _configProvider;
    private readonly RandomMapPickingService _randomMapPicker;
    private readonly INominationManager _nominationManager;
    private readonly IMcsMapConfigProvider _mapConfigProvider;

    internal Func<float>? CustomWinnerThresholdProvider { get; set; }

    internal MapVoteControllingService(
        TnmsPlugin plugin,
        PluginModuleBase moduleBase,
        ILogger logger,
        VoteControllingManager voteManager,
        IMcsInternalMainVoteState voteState,
        IInternalEventManager eventManager,
        INativeVoteManager nativeVoteManager,
        MapVoteConVars conVars,
        IMcsPluginConfigProvider configProvider,
        RandomMapPickingService randomMapPicker,
        INominationManager nominationManager,
        IMcsMapConfigProvider mapConfigProvider)
    {
        _plugin = plugin;
        _moduleBase = moduleBase;
        _logger = logger;
        _voteManager = voteManager;
        _voteState = voteState;
        _eventManager = eventManager;
        _nativeVoteManager = nativeVoteManager;
        _conVars = conVars;
        _configProvider = configProvider;
        _randomMapPicker = randomMapPicker;
        _nominationManager = nominationManager;
        _mapConfigProvider = mapConfigProvider;
    }

    public McsMapVoteState InitiateVote(bool isActivatedByRtv = false)
    {
        if (_voteManager.CurrentSession is not null)
            return _voteManager.CurrentSession.CurrentState;

        var currentState = _voteState as IMcsReadOnlyVoteState;
        if (currentState?.IsVotingPeriod() == true)
            return currentState.CurrentVoteState ?? McsMapVoteState.NoActiveVote;

        var session = _voteManager.CreateSession(isActivatedByRtv);
        session.CurrentState = McsMapVoteState.Initializing;
        _voteState.SetState(McsMapVoteState.Initializing);

        int maxElements = _configProvider.PluginConfig.VoteConfig.MaxMenuElements;
        var candidates = BuildCandidateList(isActivatedByRtv, maxElements);

        if (candidates.Count < 2)
        {
            _logger.LogWarning("Not enough maps to start vote ({Count} candidates)", candidates.Count);
            session.CurrentState = McsMapVoteState.NotEnoughMapsToStartVote;
            _voteState.SetState(McsMapVoteState.NotEnoughMapsToStartVote);

            var cancelledParams = new MapVoteCancelledParams(_plugin, _moduleBase, null);
            _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapVoteCancelled(cancelledParams));

            _voteState.Reset();
            _voteManager.ClearSession();
            return McsMapVoteState.NotEnoughMapsToStartVote;
        }

        session.SetVoteOptions(candidates);

        var realMapConfigs = candidates
            .Where(c => c.MapConfig is not null)
            .Select(c => c.MapConfig!)
            .ToList();

        var participants = GetVoteParticipants();

        var participantSlots = participants.Select(c => c.Slot).ToList();
        var startParams = new MapVoteStartParams(_plugin, _moduleBase, realMapConfigs, participantSlots);
        bool cancelled = _eventManager.FireCancellable<IMapVoteEventListener>(e => e.OnMapVoteStart(startParams));
        if (cancelled)
        {
            _logger.LogInformation("Vote start cancelled by event listener");
            var cancelledParams = new MapVoteCancelledParams(_plugin, _moduleBase, null);
            _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapVoteCancelled(cancelledParams));
            session.CurrentState = McsMapVoteState.NoActiveVote;
            _voteState.Reset();
            _voteManager.ClearSession();
            return McsMapVoteState.NoActiveVote;
        }

        session.CurrentState = McsMapVoteState.InitializeAccepted;
        _voteState.SetState(McsMapVoteState.InitializeAccepted);

        if (!StartNativeVote(session, candidates, participants, false))
            return McsMapVoteState.NoActiveVote;

        return McsMapVoteState.InitializeAccepted;
    }

    public McsMapVoteState CancelVote(IGameClient? client)
    {
        var session = _voteManager.CurrentSession;
        if (session is null)
            return McsMapVoteState.NoActiveVote;

        session.CurrentState = McsMapVoteState.Cancelling;
        _voteState.SetState(McsMapVoteState.Cancelling);

        // Clear session BEFORE calling NVM — NVM's CancelVote synchronously invokes
        // OnVoteCancelled on the handler, and the stale guard needs to see the session
        // as already cleared to prevent HandleExternalCancel from double-firing.
        _voteState.Reset();
        _voteManager.ClearSession();

        _nativeVoteManager.CancelVote();

        var cancelledParams = new MapVoteCancelledParams(_plugin, _moduleBase, client);
        _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapVoteCancelled(cancelledParams));

        return McsMapVoteState.Cancelling;
    }

    public bool ForceResetVote()
    {
        var session = _voteManager.CurrentSession;
        if (session is null)
            return false;

        _voteState.Reset();
        _voteManager.ClearSession();

        _nativeVoteManager.CancelVote();

        var cancelledParams = new MapVoteCancelledParams(_plugin, _moduleBase, null);
        _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapVoteCancelled(cancelledParams));

        return true;
    }

    internal void HandleExternalCancel(MapVoteInformation session)
    {
        var cancelledParams = new MapVoteCancelledParams(_plugin, _moduleBase, null);
        _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapVoteCancelled(cancelledParams));

        session.CurrentState = McsMapVoteState.NoActiveVote;
        _voteState.Reset();
        _voteManager.ClearSession();
    }

    internal void StartRunoffVote(MapVoteInformation originalSession, List<MapVoteOption> runoffCandidates)
    {
        if (runoffCandidates.Count < 2)
        {
            HandleWinner(originalSession, runoffCandidates.FirstOrDefault());
            return;
        }

        originalSession.CurrentState = McsMapVoteState.RunoffVoting;
        _voteState.SetState(McsMapVoteState.RunoffVoting);

        // Defer to next frame — NVM still holds the active handler during the
        // OnVotePassed/OnVoteFailed callback, so a synchronous InitiateMultiChoiceVote
        // would return VoteAlreadyInProgress.
        _plugin.SharedSystem.GetModSharp().PushTimer(() =>
        {
            if (!ReferenceEquals(_voteManager.CurrentSession, originalSession))
                return;

            var freshOptions = runoffCandidates
                .Select(c => (IMapVoteOption)new MapVoteOption(c.MapName, c.MapConfig))
                .ToList();

            originalSession.SetVoteOptions(freshOptions);

            var participants = GetVoteParticipants();
            if (!StartNativeVote(originalSession, freshOptions, participants, true))
            {
                HandleWinner(originalSession, originalSession.GetTopVotedOption());
            }
        }, 0f, Sharp.Shared.Enums.GameTimerFlags.None);
    }

    internal void HandleVoteResult(MapVoteInformation session, bool isRunoff)
    {
        session.CurrentState = McsMapVoteState.Finalizing;
        _voteState.SetState(McsMapVoteState.Finalizing);

        float winnerThreshold = _conVars.WinnerPickupThreshold.GetFloat();
        float runoffThreshold = _conVars.RunoffMapPickupThreshold.GetFloat();

        var winners = session.GetOptionsAboveThreshold(winnerThreshold);

        if (winners.Count == 0 && !isRunoff)
        {
            var runoffCandidates = session.GetOptionsAboveThreshold(runoffThreshold);

            if (runoffCandidates.Count < 2)
            {
                var top = session.GetTopVotedOption();
                if (top is not null && !runoffCandidates.Contains(top))
                    runoffCandidates.Insert(0, top);
            }

            if (runoffCandidates.Count >= 2)
            {
                _logger.LogInformation("No decisive winner, starting runoff with {Count} candidates", runoffCandidates.Count);
                StartRunoffVote(session, runoffCandidates);
                return;
            }
        }

        MapVoteOption? winner;
        if (winners.Count > 0)
        {
            winner = winners[0];
        }
        else
        {
            winner = session.GetTopVotedOption();
            if (winner is null && session.TotalVoteCount == 0)
            {
                var nonPlaceholders = session.VoteOptions
                    .OfType<MapVoteOption>()
                    .Where(o => o.MapConfig is not null)
                    .ToList();

                if (nonPlaceholders.Count > 0)
                    winner = nonPlaceholders[Random.Shared.Next(nonPlaceholders.Count)];
            }
        }

        HandleWinner(session, winner);
    }

    private void HandleWinner(MapVoteInformation session, MapVoteOption? winner)
    {
        session.SetWinner(winner);

        var finishedParams = new MapVoteFinishedParams(_plugin, _moduleBase, session, session.IsRtvVote);
        _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapVoteFinished(finishedParams));

        if (winner is null)
        {
            _logger.LogWarning("Vote concluded with no winner");
            var notChangedParams = new MapVoteNotChangedParams(_plugin, _moduleBase);
            _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapNotChanged(notChangedParams));
        }
        else if (winner.MapName == MapVoteConstants.ExtendMapInternalName)
        {
            _logger.LogInformation("Vote result: Extend current map");
            // Extend params require TimeLimitType and duration — these are supplied by MapCycle.
            // Fire a placeholder extend event; the MapCycle module handles the actual extension.
            var extendParams = new MapVoteExtendParams(_plugin, _moduleBase, 0, Shared.MapCycle.Managers.TimeLimit.TimeLimitType.Time);
            _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapExtended(extendParams));
        }
        else if (winner.MapName == MapVoteConstants.DontChangeMapInternalName)
        {
            _logger.LogInformation("Vote result: Don't change map");
            var notChangedParams = new MapVoteNotChangedParams(_plugin, _moduleBase);
            _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapNotChanged(notChangedParams));
        }
        else if (winner.MapConfig is not null)
        {
            _logger.LogInformation("Vote result: Next map confirmed as {MapName}", winner.MapName);
            var confirmedParams = new MapVoteMapConfirmedParams(_plugin, _moduleBase, winner.MapConfig);
            _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapConfirmed(confirmedParams));

            session.CurrentState = McsMapVoteState.NextMapConfirmed;
            _voteState.SetState(McsMapVoteState.NextMapConfirmed);
            _voteManager.ClearSession();
            return;
        }

        session.CurrentState = McsMapVoteState.NoActiveVote;
        _voteState.Reset();
        _voteManager.ClearSession();
    }

    private List<IMapVoteOption> BuildCandidateList(bool isRtvVote, int maxElements)
    {
        var candidates = new List<IMapVoteOption>();
        var usedMapNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Placeholder option (slot 0)
        if (isRtvVote)
            candidates.Add(new MapVoteOption(MapVoteConstants.DontChangeMapInternalName, null));
        else
            candidates.Add(new MapVoteOption(MapVoteConstants.ExtendMapInternalName, null));

        int slotsForMaps = maxElements - 1;

        // Admin-nominated maps first
        var nominations = _nominationManager.NominatedMaps;
        var forceNominated = nominations.Values
            .Where(n => n.IsForceNominated)
            .Select(n => n.MapConfig)
            .ToList();

        foreach (var map in forceNominated)
        {
            if (candidates.Count - 1 >= slotsForMaps)
                break;

            if (usedMapNames.Add(map.MapName))
                candidates.Add(new MapVoteOption(map.MapName, map));
        }

        // Community nominations sorted by participant count
        var communityNominated = nominations.Values
            .Where(n => !n.IsForceNominated)
            .OrderByDescending(n => n.NominationParticipants.Count)
            .Select(n => n.MapConfig)
            .ToList();

        foreach (var map in communityNominated)
        {
            if (candidates.Count - 1 >= slotsForMaps)
                break;

            if (usedMapNames.Add(map.MapName))
                candidates.Add(new MapVoteOption(map.MapName, map));
        }

        // Fire OnRandomMapPick — listeners may supply a custom candidate list
        int remaining = slotsForMaps - (candidates.Count - 1);
        if (remaining > 0)
        {
            var allConfigs = _mapConfigProvider.GetMapConfigs()
                .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.First().MapConfig,
                    StringComparer.OrdinalIgnoreCase);

            var pickParams = new MapVoteRandomMapPickParams(
                _plugin, _moduleBase, remaining, allConfigs);

            List<IMapConfig>? overrideList = null;
            _eventManager.Fire<IMapVoteEventListener>(e =>
            {
                var result = e.OnRandomMapPick(pickParams);
                if (result.Count > 0)
                    overrideList = result;
            });

            IEnumerable<IMapConfig> mapsToAdd;
            if (overrideList is not null)
            {
                mapsToAdd = overrideList;
            }
            else
            {
                mapsToAdd = _randomMapPicker.PickRandomMaps(remaining, usedMapNames);
            }

            foreach (var map in mapsToAdd)
            {
                if (candidates.Count - 1 >= slotsForMaps)
                    break;

                if (usedMapNames.Add(map.MapName))
                    candidates.Add(new MapVoteOption(map.MapName, map));
            }
        }

        return candidates;
    }

    private List<IGameClient> GetVoteParticipants()
    {
        bool excludeSpectators = _conVars.ExcludeSpectators.GetInt32() != 0;
        var clients = _plugin.SharedSystem.GetModSharp().GetIServer().GetGameClients(true);

        return clients
            .Where(c => !c.IsFakeClient && !c.IsHltv)
            .Where(c => !excludeSpectators
                        || c.GetPlayerController()?.Team != CStrikeTeam.Spectator)
            .ToList();
    }

    private bool StartNativeVote(
        MapVoteInformation session,
        IReadOnlyList<IMapVoteOption> candidates,
        List<IGameClient> participants,
        bool isRunoff)
    {
        float voteDuration = _conVars.VoteEndTime.GetFloat();
        float winnerThreshold = _conVars.WinnerPickupThreshold.GetFloat();

        var voteContents = new List<VoteContent>();
        for (int i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];

            LocalizedString visibleName;
            if (candidate.MapName == MapVoteConstants.ExtendMapInternalName)
                visibleName = LocalizedString.From(c => _plugin.Localizer.ForCulture("Vote.Option.ExtendMap", c ?? System.Globalization.CultureInfo.CurrentCulture));
            else if (candidate.MapName == MapVoteConstants.DontChangeMapInternalName)
                visibleName = LocalizedString.From(c => _plugin.Localizer.ForCulture("Vote.Option.DontChange", c ?? System.Globalization.CultureInfo.CurrentCulture));
            else
                visibleName = LocalizedString.From(_ => candidate.MapConfig?.MapName ?? candidate.MapName);

            voteContents.Add(new VoteContent
            {
                Index = i,
                InternalName = candidate.MapName,
                VisibleName = visibleName,
            });
        }

        IMultiChoiceVoteHandler handler = isRunoff
            ? new RunoffVoteNativeHandler(this, _voteManager, session, _logger)
            : new InitialVoteNativeHandler(this, _voteManager, session, _logger);

        VotePassCondition passCondition;
        if (isRunoff)
        {
            passCondition = _ => true;
        }
        else
        {
            float threshold = winnerThreshold;
            if (CustomWinnerThresholdProvider is { } provider)
            {
                try { threshold = provider(); }
                catch { /* fall back to ConVar threshold */ }
            }
            passCondition = VotePassConditions.Default(threshold);
        }

        var voteOptions = new MultiChoiceVoteOptions
        {
            Title = LocalizedString.From(c => _plugin.Localizer.ForCulture(isRunoff ? "Vote.Title.Runoff" : "Vote.Title.Initial", c ?? System.Globalization.CultureInfo.CurrentCulture)),
            Description = LocalizedString.From(c => _plugin.Localizer.ForCulture("Vote.Description", c ?? System.Globalization.CultureInfo.CurrentCulture)),
            VoteDuration = voteDuration,
            VoteHandler = handler,
            VoteContents = voteContents,
            PassCondition = passCondition,
            RandomShuffle = _conVars.ShuffleMenu.GetInt32() != 0,
            Participants = participants,
        };

        var result = _nativeVoteManager.InitiateMultiChoiceVote(voteOptions);

        if (result != VoteInitiateResult.Success)
        {
            _logger.LogWarning("Failed to initiate native vote: {Result}", result);
            var cancelledParams = new MapVoteCancelledParams(_plugin, _moduleBase, null);
            _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapVoteCancelled(cancelledParams));
            _voteState.Reset();
            _voteManager.ClearSession();
            return false;
        }

        session.CurrentState = isRunoff ? McsMapVoteState.RunoffVoting : McsMapVoteState.Voting;
        _voteState.SetState(session.CurrentState);
        return true;
    }
}
