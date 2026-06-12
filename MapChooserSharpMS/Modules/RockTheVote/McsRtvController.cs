using System;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Modules.RockTheVote.Interfaces;
using MapChooserSharpMS.Modules.RockTheVote.Managers;
using MapChooserSharpMS.Modules.RockTheVote.Services;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.Events.MapCycle.Params;
using MapChooserSharpMS.Shared.Events.MapVote;
using MapChooserSharpMS.Shared.Events.MapVote.Params;
using MapChooserSharpMS.Shared.Events.RockTheVote;
using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.RockTheVote;
using MapChooserSharpMS.Shared.RockTheVote.Managers;
using MapChooserSharpMS.Shared.RockTheVote.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.RockTheVote;

internal sealed class McsRtvController: PluginModuleBase, IMcsInternalRtvController, IMapVoteEventListener, IMapCycleEventListener, IGameListener, IClientListener
{
    public override string PluginModuleName => "McsRtvController";
    public override string ModuleChatPrefix => "Prefix.Rtv";
    protected override bool UseTranslationKeyInModuleChatPrefix => true;

    private InternalRtvManager _rtvManager = null!;
    private RtvService _rtvService = null!;
    private IInternalEventManager _eventManager = null!;
    private IMcsPluginConfigProvider _configProvider = null!;
    private RtvConVars _conVars = null!;
    private Guid _cooldownTimerId = Guid.Empty;

    public IRtvManager RtvManager => _rtvManager;
    public IRtvService RtvService => _rtvService;
    public RtvConVars ConVars => _conVars;

    internal McsRtvController(IServiceProvider serviceProvider, bool hotReload): base(serviceProvider, hotReload)
    {
        _conVars = new RtvConVars(Plugin.SharedSystem.GetConVarManager());
        foreach (var cv in _conVars.All()) TrackConVar(cv);
    }

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IMcsInternalRtvController>(this);
    }

    protected override void OnInitialize()
    {
        _rtvManager = new InternalRtvManager(Plugin, _conVars);
        _eventManager = ServiceProvider.GetRequiredService<IInternalEventManager>();
        _configProvider = ServiceProvider.GetRequiredService<IMcsPluginConfigProvider>();
        _rtvService = new RtvService(Plugin, this, _rtvManager, _eventManager, ServiceProvider);
    }

    protected override void OnAllModulesLoaded()
    {
        _eventManager.RegisterListener<IMapVoteEventListener>(this);
        _eventManager.RegisterListener<IMapCycleEventListener>(this);
        SharedSystem.GetModSharp().InstallGameListener(this);
        SharedSystem.GetClientManager().InstallClientListener(this);

        AddCommandsUnderNamespace("MapChooserSharpMS.Modules.RockTheVote.Commands");
    }

    protected override void OnUnloadModule()
    {
        SharedSystem.GetModSharp().RemoveGameListener(this);
        SharedSystem.GetClientManager().RemoveClientListener(this);
        _eventManager.RemoveListener<IMapVoteEventListener>(this);
        _eventManager.RemoveListener<IMapCycleEventListener>(this);
        StopCooldownTimer();
        _rtvManager.ForceReset();
    }

    public void NotifyAdminCommandResult(IGameClient? client, string translationKey)
    {
        if (client is null)
        {
            Logger.LogInformation("[RTV] {Key}", translationKey);
            return;
        }

        client.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(client, translationKey));
    }

    public bool OnMapVoteStart(IMapVoteStartParams @params)
    {
        if (_rtvManager.RtvStatus != RtvStatus.TriggeredWaitingForVote
            && _rtvManager.RtvStatus != RtvStatus.TriggeredWaitingForMapTransition)
        {
            _rtvManager.ForceSetRtvStatus(RtvStatus.AnotherVoteOngoing);
        }
        return false;
    }

    public void OnMapVoteFinished(IMapVoteFinishedEventParams @params)
    {
        _rtvManager.ClearParticipants();
    }

    public void OnMapConfirmed(IMapVoteMapConfirmedEventParams @params)
    {
        ScheduleCooldown(_conVars.CommandUnlockTimeNextMapConfirmed.GetFloat());
    }

    public void OnMapExtended(IMapVoteExtendParams @params)
    {
        ScheduleCooldown(_conVars.CommandUnlockTimeMapExtend.GetFloat());
    }

    public void OnMapNotChanged(IMapVoteNotChangedParams @params)
    {
        ScheduleCooldown(_conVars.CommandUnlockTimeMapNotChanged.GetFloat());
    }

    public void OnMapVoteCancelled(IMapVoteCancelledParams @params)
    {
        _rtvManager.ClearParticipants();
        _rtvManager.ForceSetRtvStatus(RtvStatus.Enabled);
    }

    public void OnGameActivate()
    {
        StopCooldownTimer();
        _rtvManager.ForceReset();
        ScheduleCooldown(_conVars.CommandUnlockTimeMapStart.GetFloat());
    }

    #region IMapCycleEventListener — extend vote coordination

    public void OnExtendVoteStarted(IExtendVoteStartedEventParams @params)
    {
        if (_rtvManager.RtvStatus == RtvStatus.Enabled)
            _rtvManager.ForceSetRtvStatus(RtvStatus.AnotherVoteOngoing);
    }

    public void OnExtendVoteFinished(IExtendVoteFinishedEventParams @params)
    {
        // On pass, OnMapExtended has already scheduled the post-extend
        // cooldown (status InCooldown) — only restore from the blocked state.
        if (_rtvManager.RtvStatus == RtvStatus.AnotherVoteOngoing)
            _rtvManager.ForceSetRtvStatus(RtvStatus.Enabled);
    }

    public void OnExtendVoteCancelled(IExtendVoteCancelledEventParams @params)
    {
        if (_rtvManager.RtvStatus == RtvStatus.AnotherVoteOngoing)
            _rtvManager.ForceSetRtvStatus(RtvStatus.Enabled);
    }

    #endregion

    public void InstallEventListener(IRockTheVoteEventListener listener)
    {
        _eventManager.RegisterListener(listener);
    }

    public void RemoveEventListener(IRockTheVoteEventListener listener)
    {
        _eventManager.RemoveListener(listener);
    }

    internal void ResetRtvState()
    {
        StopCooldownTimer();
        _rtvManager.ForceReset();
    }

    public void OnClientDisconnecting(IGameClient client, NetworkDisconnectionReason reason)
    {
        _rtvService.RemoveClientFromRtv(client.Slot);
    }

    private void ScheduleCooldown(float seconds)
    {
        StopCooldownTimer();

        if (seconds <= 0)
        {
            _rtvManager.ForceSetRtvStatus(RtvStatus.Enabled);
            return;
        }

        _rtvManager.ForceSetRtvStatus(RtvStatus.InCooldown);
        _rtvManager.SetNextRtvCommandUnlockTime(TimeSpan.FromSeconds(seconds));

        _cooldownTimerId = SharedSystem.GetModSharp().PushTimer(
            OnCooldownExpired,
            seconds,
            GameTimerFlags.StopOnMapEnd);
    }

    private void OnCooldownExpired()
    {
        _cooldownTimerId = Guid.Empty;
        _rtvManager.SetNextRtvCommandUnlockTime(TimeSpan.Zero);

        if (_rtvManager.RtvStatus == RtvStatus.InCooldown)
            _rtvManager.ForceSetRtvStatus(RtvStatus.Enabled);
    }

    private void StopCooldownTimer()
    {
        if (_cooldownTimerId != Guid.Empty)
        {
            SharedSystem.GetModSharp().StopTimer(_cooldownTimerId);
            _cooldownTimerId = Guid.Empty;
        }
    }

    public int ListenerVersion => 1;
    public int ListenerPriority => 9999999;
}
