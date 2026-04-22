using System;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Modules.RockTheVote.Interfaces;
using MapChooserSharpMS.Modules.RockTheVote.Managers;
using MapChooserSharpMS.Modules.RockTheVote.Services;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.Events.MapVote;
using MapChooserSharpMS.Shared.Events.MapVote.Params;
using MapChooserSharpMS.Shared.Events.RockTheVote;
using MapChooserSharpMS.Shared.RockTheVote;
using MapChooserSharpMS.Shared.RockTheVote.Managers;
using MapChooserSharpMS.Shared.RockTheVote.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.RockTheVote;

internal sealed class McsRtvController: PluginModuleBase, IMcsInternalRtvController, IMapVoteEventListener, IMapCycleEventListener, IGameListener
{
    public override string PluginModuleName => "McsRtvController";
    public override string ModuleChatPrefix => "Prefix.Rtv";
    protected override bool UseTranslationKeyInModuleChatPrefix => true;

    private InternalRtvManager _rtvManager = null!;
    private RtvService _rtvService = null!;
    private IInternalEventManager _eventManager = null!;
    private IMcsPluginConfigProvider _configProvider = null!;
    private RtvConVars _conVars = null!;

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
        // IMcsInternalRtvController : IMcsRtvController — the public API is
        // wired by the plugin entrypoint (MapChooserSharpMs.cs), which resolves
        // this instance and casts to IMcsRtvController when building the shared
        // API object. No duplicate registration needed.
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
    }

    protected override void OnUnloadModule()
    {
        SharedSystem.GetModSharp().RemoveGameListener(this);
        _eventManager.RemoveListener<IMapVoteEventListener>(this);
        _eventManager.RemoveListener<IMapCycleEventListener>(this);
        _rtvManager.ForceReset();
    }

    /// <summary>
    /// Surfaces the outcome of an admin RTV command (enable/disable) to the
    /// invoker. Routed through the controller so <see cref="RtvService"/>
    /// (a plain service, no localization helpers) can reach the module's
    /// <c>LocalizeWithModulePrefix</c>. Falls back to a server-log line when
    /// no client context is available (e.g. server console).
    /// </summary>
    public void NotifyAdminCommandResult(IGameClient? client, string translationKey)
    {
        if (client is null)
        {
            Logger.LogInformation("[RTV] {Key}", translationKey);
            return;
        }

        client.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(client, translationKey));
    }

    /// <summary>
    /// Lock RTV while another (non-RTV) vote is in flight. RTV-initiated votes
    /// are already represented by <see cref="RtvStatus.TriggeredWaitingForVote"/>;
    /// don't clobber those.
    /// </summary>
    public bool OnMapVoteStart(IMapVoteStartParams @params)
    {
        if (_rtvManager.RtvStatus != RtvStatus.TriggeredWaitingForVote
            && _rtvManager.RtvStatus != RtvStatus.TriggeredWaitingForMapTransition)
        {
            _rtvManager.ForceSetRtvStatus(RtvStatus.AnotherVoteOngoing);
        }
        return false;
    }

    /// <summary>
    /// Vote finished — regardless of outcome, clear RTV state.
    /// </summary>
    public void OnMapVoteFinished(IMapVoteFinishedEventParams @params)
    {
        // TODO(rtv): schedule CommandUnlockTime* cooldown based on outcome
        // (NextMapConfirmed / MapNotChanged / MapExtend) instead of full reset.
        _rtvManager.ForceReset();
    }

    /// <summary>
    /// New map started — clear RTV state so the next round begins fresh.
    /// </summary>
    public void OnGameActivate()
    {
        // TODO(rtv): schedule CommandUnlockTimeMapStart cooldown instead of
        // going straight to Enabled.
        _rtvManager.ForceReset();
    }

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
        _rtvManager.ForceReset();
    }

    public void OnClientDisconnect(int slot)
    {
        _rtvService.RemoveClientFromRtv(slot);
    }


    public int ListenerVersion => 1;
    public int ListenerPriority => 9999999;
}
