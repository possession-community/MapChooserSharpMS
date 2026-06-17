using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.Statistics;

internal sealed class McsStatisticsController
    : PluginModuleBase,
      IClientListener,
      IGameListener,
      IEventListener
{
    public override string PluginModuleName => "McsStatisticsController";
    public override string ModuleChatPrefix => "unused";
    protected override bool UseTranslationKeyInModuleChatPrefix => false;

    public int ListenerVersion => 1;
    public int ListenerPriority => 0;

    private readonly McsStatisticsTracker _tracker = new();

    public McsStatisticsController(IServiceProvider serviceProvider, bool hotReload)
        : base(serviceProvider, hotReload)
    {
    }

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton(_tracker);
    }

    protected override void OnInitialize()
    {
        SharedSystem.GetModSharp().InstallGameListener(this);
        SharedSystem.GetClientManager().InstallClientListener(this);

        var em = SharedSystem.GetEventManager();
        em.InstallEventListener(this);
        em.HookEvent("round_end");

        if (HotReload)
            SyncCurrentPlayerCount();
    }

    protected override void OnUnloadModule()
    {
        SharedSystem.GetModSharp().RemoveGameListener(this);
        SharedSystem.GetClientManager().RemoveClientListener(this);
        SharedSystem.GetEventManager().RemoveEventListener(this);
    }

    public void OnGameActivate()
    {
        _tracker.ResetForNewMap();
    }

    public void OnGameDeactivate()
    {
    }

    public void OnClientPutInServer(IGameClient client)
    {
        if (client.IsFakeClient || client.IsHltv)
            return;
        _tracker.OnPlayerConnected();
    }

    public void OnClientDisconnecting(IGameClient client, NetworkDisconnectionReason reason)
    {
        if (client.IsFakeClient || client.IsHltv)
            return;
        _tracker.OnPlayerDisconnected();
    }

    public void FireGameEvent(IGameEvent @event)
    {
        if (@event.Name == "round_end")
            _tracker.OnRoundEnd();
    }

    private void SyncCurrentPlayerCount()
    {
        int count = SharedSystem.GetModSharp().GetIServer()
            .GetGameClients(false, false)
            .Count(c => !c.IsFakeClient && !c.IsHltv);
        _tracker.SetCurrentPlayerCount(count);
    }
}
