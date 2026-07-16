using System;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.MapVote.Interfaces;
using MapChooserSharpMS.Modules.MapVote.Managers;
using MapChooserSharpMS.Modules.MapVote.Services;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.Events.MapCycle.Params;
using MapChooserSharpMS.Shared.Events.MapVote;
using MapChooserSharpMS.Shared.Events.RockTheVote;
using MapChooserSharpMS.Shared.Events.RockTheVote.Params;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.MapVote.Managers;
using MapChooserSharpMS.Shared.MapVote.Services;
using MapChooserSharpMS.Shared.Nomination.Managers;
using MapChooserSharpMS.Shared.Nomination.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NativeVoteManagerMS.Shared;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.MapVote;

internal sealed class McsMapVoteController
    : PluginModuleBase,
      IMcsInternalVoteController,
      IRockTheVoteEventListener,
      IMapCycleEventListener,
      Sharp.Shared.Listeners.IGameListener
{
    public override string PluginModuleName => "McsMapVoteController";
    public override string ModuleChatPrefix => "Prefix.Vote";
    protected override bool UseTranslationKeyInModuleChatPrefix => true;

    public int ListenerVersion => 1;
    public int ListenerPriority => 100;

    private IInternalEventManager _eventManager = null!;
    private INativeVoteManager _nativeVoteManager = null!;

    private IMcsInternalMainVoteState _voteState = null!;
    private readonly MapVoteConVars _conVars;
    private readonly VoteControllingManager _voteManager = new();

    private MapVoteControllingService _controllingService = null!;
    private ClientVoteHandlingService _clientVoteService = null!;

    public IMcsReadOnlyVoteState VoteState => (IMcsReadOnlyVoteState)_voteState;
    public IVoteControllingManager MapVoteManager => _voteManager;
    public IMapVoteControllingService MapVoteControllingService => _controllingService;
    public IClientVoteHandlingService ClientVoteHandlingService => _clientVoteService;
    public MapVoteConVars ConVars => _conVars;

    private Func<float>? _customWinnerThresholdProvider;

    public Func<float>? CustomWinnerThresholdProvider
    {
        get => _customWinnerThresholdProvider;
        set
        {
            _customWinnerThresholdProvider = value;
            if (_controllingService is not null)
                _controllingService.CustomWinnerThresholdProvider = value;
        }
    }

    internal INativeVoteManager NativeVoteManager => _nativeVoteManager;

    public McsMapVoteController(
        IServiceProvider serviceProvider,
        bool hotReload) : base(serviceProvider, hotReload)
    {
        _conVars = new MapVoteConVars(Plugin.SharedSystem.GetConVarManager());
        foreach (var cv in _conVars.All()) TrackConVar(cv);
    }

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IMcsInternalVoteController>(this);
        services.AddSingleton<IMcsReadOnlyVoteState>(((MapChooserSharpMs)Plugin).VoteState);
    }

    protected override void OnInitialize()
    {
        _voteState = ServiceProvider.GetRequiredService<IMcsInternalMainVoteState>();
        _eventManager = ServiceProvider.GetRequiredService<IInternalEventManager>();
        SharedSystem.GetModSharp().InstallGameListener(this);
    }

    /// <summary>
    /// The state manager outlives map changes — without this reset, a
    /// NextMapConfirmed left by the previous map's vote would permanently
    /// block extends/nominations on every following map.
    /// </summary>
    public void OnGameActivate()
    {
        // A vote session (or its pre-vote countdown) interrupted by a map
        // transition leaves CurrentSession behind — InitiateVote would then
        // early-return on the stale session forever. Force-clear both on
        // map boundaries.
        _controllingService?.ForceResetVote();
        _voteState.Reset();
    }

    public void OnGameDeactivate()
    {
        _controllingService?.ForceResetVote();
        _voteState.Reset();
    }

    protected override void OnAllModulesLoaded()
    {
        _nativeVoteManager = SharedSystem.GetSharpModuleManager()
            .GetRequiredSharpModuleInterface<INativeVoteManager>(INativeVoteManager.ModSharpModuleIdentity)
            .Instance!;

        var wuling = SharedSystem.GetSharpModuleManager()
            .GetRequiredSharpModuleInterface<Wuling.Abstract.IWuling>(Wuling.Abstract.IWuling.Identity)
            .Instance!;

        var configProvider = ServiceProvider.GetRequiredService<IMcsPluginConfigProvider>();
        var nominationValidateService = ServiceProvider.GetRequiredService<INominationValidateService>();
        var mapConfigProvider = ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();
        var nominationManager = ServiceProvider.GetRequiredService<INominationManager>();
        var mapExtendService = ServiceProvider.GetRequiredService<Modules.MapCycle.Services.Interfaces.IMcsInternalMapExtendService>();
        var cooldownLifecycleService = ServiceProvider.GetRequiredService<Modules.MapCycle.Services.McsMapCooldownLifecycleService>();

        var randomMapPicker = new RandomMapPickingService(
            (Modules.Nomination.Services.NominationValidateService)nominationValidateService,
            configProvider, mapConfigProvider);

        var soundConfig = configProvider.PluginConfig.VoteConfig.VoteSoundConfig;
        var soundPlayer = new McsMapVoteSoundPlayer(Plugin, SharedSystem.GetSoundManager(), soundConfig);

        var countdownUi = ServiceProvider.GetRequiredService<Ui.Countdown.McsCountdownUiController>();
        soundPlayer.VolumeProvider = client => countdownUi.PreferenceService.GetVolume(client.Slot);

        _controllingService = new MapVoteControllingService(
            Plugin, this, Logger,
            _voteManager, _voteState, _eventManager,
            _nativeVoteManager, _conVars, configProvider,
            randomMapPicker, nominationManager, mapConfigProvider,
            mapExtendService, cooldownLifecycleService,
            soundPlayer, countdownUi,
            wuling.Menu, wuling.Registry);

        _controllingService.CustomWinnerThresholdProvider = _customWinnerThresholdProvider;

        _clientVoteService = new ClientVoteHandlingService(_voteManager);

        _eventManager.RegisterListener<IRockTheVoteEventListener>(this);
        _eventManager.RegisterListener<IMapCycleEventListener>(this);

        AddCommandsUnderNamespace("MapChooserSharpMS.Modules.MapVote.Commands");
    }

    protected override void OnUnloadModule()
    {
        SharedSystem.GetModSharp().RemoveGameListener(this);
        _eventManager.RemoveListener<IRockTheVoteEventListener>(this);
        _eventManager.RemoveListener<IMapCycleEventListener>(this);
        _controllingService?.ForceResetVote();
    }

    /// <summary>
    /// Admin/API <c>TrySetNextMap</c> outside a vote must block further votes
    /// and nominations the same way a vote-confirmed map does. The vote path
    /// sets this state itself, so re-setting the same value here is harmless.
    /// </summary>
    public void OnNextMapConfirmed(INextMapConfirmedEventParams @params)
    {
        _voteState.SetState(McsMapVoteState.NextMapConfirmed);
    }

    /// <summary>
    /// Removing the next map (admin !removenextmap / API) lifts the
    /// NextMapConfirmed block so votes and nominations work again.
    /// </summary>
    public void OnNextMapRemoved(INextMapRemovedEventParams @params)
    {
        _controllingService?.ForceResetVote();
        _voteState.Reset();
    }

    public void OnRtvConfirmed(IRtvConfirmedParams @params)
    {
        Logger.LogInformation("RTV confirmed (forced={IsForced}), initiating vote", @params.IsForced);
        _controllingService.InitiateVote(isActivatedByRtv: true);
    }

    public void OnVoteStartThresholdReached(IVoteStartThresholdReachedEventParams @params)
    {
        Logger.LogInformation("Vote start threshold reached (type={LimitType}), initiating vote", @params.LimitType);
        _controllingService.InitiateVote(isActivatedByRtv: false);
    }

    public void InstallEventListener(IMapVoteEventListener listener)
    {
        _eventManager.RegisterListener(listener);
    }

    public void RemoveEventListener(IMapVoteEventListener listener)
    {
        _eventManager.RemoveListener(listener);
    }
}
