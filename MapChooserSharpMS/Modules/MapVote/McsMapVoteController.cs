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
      IMapCycleEventListener
{
    public override string PluginModuleName => "McsMapVoteController";
    public override string ModuleChatPrefix => "Prefix.Vote";
    protected override bool UseTranslationKeyInModuleChatPrefix => true;

    public int ListenerVersion => 1;
    public int ListenerPriority => 100;

    private IInternalEventManager _eventManager = null!;
    private INativeVoteManager _nativeVoteManager = null!;

    private readonly IMcsInternalMainVoteState _voteState;
    private readonly MapVoteConVars _conVars;
    private readonly VoteControllingManager _voteManager = new();

    private MapVoteControllingService _controllingService = null!;
    private ClientVoteHandlingService _clientVoteService = null!;

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

    internal McsMapVoteController(
        IServiceProvider serviceProvider,
        bool hotReload,
        IMcsInternalMainVoteState voteState) : base(serviceProvider, hotReload)
    {
        _voteState = voteState;
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
        _eventManager = ServiceProvider.GetRequiredService<IInternalEventManager>();
    }

    protected override void OnAllModulesLoaded()
    {
        _nativeVoteManager = SharedSystem.GetSharpModuleManager()
            .GetRequiredSharpModuleInterface<INativeVoteManager>(INativeVoteManager.ModSharpModuleIdentity)
            .Instance!;

        var configProvider = ServiceProvider.GetRequiredService<IMcsPluginConfigProvider>();
        var nominationValidateService = ServiceProvider.GetRequiredService<INominationValidateService>();
        var mapConfigProvider = ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();
        var nominationManager = ServiceProvider.GetRequiredService<INominationManager>();

        var randomMapPicker = new RandomMapPickingService(nominationValidateService, configProvider, mapConfigProvider);

        _controllingService = new MapVoteControllingService(
            Plugin, this, Logger,
            _voteManager, _voteState, _eventManager,
            _nativeVoteManager, _conVars, configProvider,
            randomMapPicker, nominationManager, mapConfigProvider);

        _controllingService.CustomWinnerThresholdProvider = _customWinnerThresholdProvider;

        _clientVoteService = new ClientVoteHandlingService(_voteManager);

        _eventManager.RegisterListener<IRockTheVoteEventListener>(this);
        _eventManager.RegisterListener<IMapCycleEventListener>(this);
    }

    protected override void OnUnloadModule()
    {
        _eventManager.RemoveListener<IRockTheVoteEventListener>(this);
        _eventManager.RemoveListener<IMapCycleEventListener>(this);
        _controllingService?.ForceResetVote();
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
