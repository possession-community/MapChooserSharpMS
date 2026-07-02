using System;
using System.Collections.Generic;
using MapChooserSharpMS.Shared;
using MapChooserSharpMS.Shared.Events;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.Events.MapCycle.Params;
using MapChooserSharpMS.Shared.Events.MapVote;
using MapChooserSharpMS.Shared.Events.MapVote.Params;
using MapChooserSharpMS.Shared.Events.Nomination;
using MapChooserSharpMS.Shared.Events.Nomination.Params;
using MapChooserSharpMS.Shared.Events.RockTheVote;
using MapChooserSharpMS.Shared.Events.RockTheVote.Params;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.Ui.Menu;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Shared;

namespace McsEventDebugger;

public class McsEventDebugger : IModSharpModule,
    IMapCycleEventListener,
    IMapVoteEventListener,
    IRockTheVoteEventListener,
    INominationEventListener
{
    public string DisplayName => "MCS Event Debugger";
    public string DisplayAuthor => "faketuna";

    public int ListenerVersion => 1;
    public int ListenerPriority => int.MaxValue;

    private readonly ISharedSystem _sharedSystem;
    private readonly ILogger _logger;

    public McsEventDebugger(
        ISharedSystem sharedSystem,
        string dllPath,
        string sharpPath,
        Version? version,
        IConfiguration coreConfiguration,
        bool hotReload)
    {
        _sharedSystem = sharedSystem;
        _logger = sharedSystem.GetLoggerFactory().CreateLogger(DisplayName);
    }

    public bool Init() => true;
    public void PostInit() { }

    public void OnAllModulesLoaded()
    {
        var mcs = _sharedSystem.GetSharpModuleManager()
            .GetRequiredSharpModuleInterface<IMapChooserSharpShared>(IMapChooserSharpShared.ModSharpModuleIdentity).Instance!;

        mcs.MapCycleController.InstallEventListener(this);
        mcs.McsMapVoteController.InstallEventListener(this);
        mcs.McsRtvController.InstallEventListener(this);
        mcs.McsNominationController.InstallEventListener(this);

        _logger.LogInformation("All MCS event listeners installed");
    }

    public void Shutdown() { }

    // ── MapCycle ──

    public McsCancellableEvent OnExtCommandExecute(IExtCommandExecuteEventParams p)
    {
        _logger.LogInformation("[MapCycle] OnExtCommandExecute: Client={Client}, Required={Req}, Current={Cur}",
            p.Client?.Name, p.CurrentRequiredVotes, p.CurrentExtVotes);
        return McsCancellableEvent.Continue;
    }

    public void OnExtendVoteStarted(IExtendVoteStartedEventParams p)
        => _logger.LogInformation("[MapCycle] OnExtendVoteStarted: Map={Map}, Initiator={Init}, Duration={Dur}",
            p.CurrentMap?.MapName, p.Initiator?.Name, p.VoteDuration);

    public void OnExtendVoteCancelled(IExtendVoteCancelledEventParams p)
        => _logger.LogInformation("[MapCycle] OnExtendVoteCancelled: Map={Map}, By={By}",
            p.CurrentMap?.MapName, p.CancelledBy?.Name);

    public void OnExtendVoteFinished(IExtendVoteFinishedEventParams p)
        => _logger.LogInformation("[MapCycle] OnExtendVoteFinished: Map={Map}, Passed={Passed}, Yes={Yes}, No={No}",
            p.CurrentMap?.MapName, p.Passed, p.YesCount, p.NoCount);

    public void OnNextMapConfirmed(INextMapConfirmedEventParams p)
        => _logger.LogInformation("[MapCycle] OnNextMapConfirmed: Next={Next}, Old={Old}",
            p.NextMap?.MapName, p.OldNextMap?.MapName);

    public void OnNextMapRemoved(INextMapRemovedEventParams p)
        => _logger.LogInformation("[MapCycle] OnNextMapRemoved: Removed={Map}",
            p.PreviousNextMap?.MapName);

    public void OnMcsIntermission(IMcsIntermissionParams p)
        => _logger.LogInformation("[MapCycle] OnMcsIntermission: NextMap={Map}", p.NextMap?.MapName);

    public void OnMapCooldownApply(IMapCooldownApplyEventParams p)
        => _logger.LogInformation("[MapCycle] OnMapCooldownApply: Map={Map}, CD={CD}, Timed={Timed}, Cancelled={Cancel}",
            p.AppliesTo?.MapName, p.Cooldown, p.TimedCooldownDuration, p.IsCancelled);

    public void OnTimeLimitReached(ITimeLimitReachedEventParams p)
        => _logger.LogInformation("[MapCycle] OnTimeLimitReached: Type={Type}", p.LimitType);

    public void OnVoteStartThresholdReached(IVoteStartThresholdReachedEventParams p)
        => _logger.LogInformation("[MapCycle] OnVoteStartThresholdReached: Type={Type}", p.LimitType);

    public void OnMapInfoCommandExecuted(IMapInfoCommandExecutedParams p)
        => _logger.LogInformation("[MapCycle] OnMapInfoCommandExecuted: Client={Client}, Map={Map}",
            p.Client?.Name, p.MapConfig?.MapName);

    // ── MapVote ──

    public McsCancellableEvent OnMapVoteStart(IMapVoteStartParams p)
    {
        _logger.LogInformation("[MapVote] OnMapVoteStart: Maps={Maps}, Participants={Parts}",
            p.MapsToVote?.Count, p.VoteParticipants?.Count);
        return McsCancellableEvent.Continue;
    }

    public McsValueOverrideEvent<List<IMapConfig>> OnAdminNominatedMapPick(IAdminNominatedMapPickParams p)
    {
        _logger.LogInformation("[MapVote] OnAdminNominatedMapPick: Count={Count}",
            p.SelectedMaps?.Count);
        return McsValueOverrideEvent<List<IMapConfig>>.NoOverride;
    }

    public McsValueOverrideEvent<List<IMapConfig>> OnNominatedMapPick(INominatedMapPickParams p)
    {
        _logger.LogInformation("[MapVote] OnNominatedMapPick: Count={Count}",
            p.SelectedMaps?.Count);
        return McsValueOverrideEvent<List<IMapConfig>>.NoOverride;
    }

    public McsValueOverrideEvent<List<IMapConfig>> OnRandomMapPick(IMapVoteRandomMapPickParams p)
    {
        _logger.LogInformation("[MapVote] OnRandomMapPick: MinSlots={Min}, Available={Avail}",
            p.MinimumMapCounts, p.MapConfigs?.Count);
        return McsValueOverrideEvent<List<IMapConfig>>.NoOverride;
    }

    public void OnMapVoteFinished(IMapVoteFinishedEventParams p)
        => _logger.LogInformation("[MapVote] OnMapVoteFinished: IsRtv={IsRtv}, Winner={Winner}",
            p.IsRtvVote, p.VoteInformation?.Winner?.MapName);

    public void OnMapVoteCancelled(IMapVoteCancelledParams p)
        => _logger.LogInformation("[MapVote] OnMapVoteCancelled");

    public void OnMapExtended(IMapVoteExtendParams p)
        => _logger.LogInformation("[MapVote] OnMapExtended: Amount={Amount}, Type={Type}",
            p.ExtendTime, p.TimeLimitType);

    public void OnMapNotChanged(IMapVoteNotChangedParams p)
        => _logger.LogInformation("[MapVote] OnMapNotChanged");

    public void OnMapConfirmed(IMapVoteMapConfirmedEventParams p)
        => _logger.LogInformation("[MapVote] OnMapConfirmed: Map={Map}, IsRtv={IsRtv}",
            p.ConfirmedMap?.MapName, p.IsRtvVote);

    // ── RTV ──

    public McsCancellableEvent OnClientRtvCast(IClientRtvCastParams p)
    {
        _logger.LogInformation("[RTV] OnClientRtvCast: Client={Client}", p.Client?.Name);
        return McsCancellableEvent.Continue;
    }

    public McsCancellableEvent OnClientRtvUnCast(IClientRtvUnCastParams p)
    {
        _logger.LogInformation("[RTV] OnClientRtvUnCast: Client={Client}", p.Client?.Name);
        return McsCancellableEvent.Continue;
    }

    public McsCancellableEvent OnForceRtv(IForceRtvParam p)
    {
        _logger.LogInformation("[RTV] OnForceRtv: Client={Client}", p.Client?.Name);
        return McsCancellableEvent.Continue;
    }

    public void OnRtvConfirmed(IRtvConfirmedParams p)
        => _logger.LogInformation("[RTV] OnRtvConfirmed: IsForced={IsForced}", p.IsForced);

    // ── Nomination ──

    public McsCancellableEvent OnNominationCheckPassed(INominationCheckPassedEventParams p)
    {
        _logger.LogInformation("[Nomination] OnNominationCheckPassed");
        return McsCancellableEvent.Continue;
    }

    public McsCancellableEvent OnNomination(INominationParams p)
    {
        _logger.LogInformation("[Nomination] OnNomination: Map={Map}, Client={Client}",
            p.NominationData?.MapConfig?.MapName, p.Client?.Name);
        return McsCancellableEvent.Continue;
    }

    public McsCancellableEvent OnAdminNomination(IAdminNominationParams p)
    {
        _logger.LogInformation("[Nomination] OnAdminNomination: Map={Map}, Client={Client}",
            p.NominationData?.MapConfig?.MapName, p.Client?.Name);
        return McsCancellableEvent.Continue;
    }

    public void OnNominationChanged(INominationChangeParams p)
        => _logger.LogInformation("[Nomination] OnNominationChanged: Map={Map}, Client={Client}",
            p.NominationData?.MapConfig?.MapName, p.Client?.Name);

    public void OnNominationRemoved(INominationRemovedParams p)
        => _logger.LogInformation("[Nomination] OnNominationRemoved: Map={Map}",
            p.NominationData?.MapConfig?.MapName);

    public void OnUnNominate(IUnNominateParams p)
        => _logger.LogInformation("[Nomination] OnUnNominate: Map={Map}, Slot={Slot}, Reason={Reason}",
            p.NominationData?.MapConfig?.MapName, p.Slot, p.Reason);

    public void OnNominationMenuDetailsOpening(INominationMenuDetailsOpeningParams p)
    {
        _logger.LogInformation("[Nomination] OnNominationMenuDetailsOpening: Map={Map}, Client={Client}",
            p.MapConfig.MapName, p.Client.Name);

        p.ExtraItems.Add(new McsMenuItem
        {
            DisplayText = $"Debugger - {p.MapConfig.MapName}",
            OnSelect = client =>
            {
                _logger.LogInformation("[Nomination] Debugger item clicked: Map={Map}, Client={Client}",
                    p.MapConfig.MapName, client.Name);
            },
        });
    }
}
