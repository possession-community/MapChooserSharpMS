using System;
using System.Linq;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.MapCycle;
using MapChooserSharpMS.Modules.EventManager.Events.RockTheVote;
using McsCancellableEvent = MapChooserSharpMS.Shared.Events.McsCancellableEvent;
using MapChooserSharpMS.Modules.MapCycle.Managers.MapTransition.Interfaces;
using MapChooserSharpMS.Modules.PluginConfig.Enums;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Modules.RockTheVote.Interfaces;
using MapChooserSharpMS.Modules.RockTheVote.Managers;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.Events.RockTheVote;
using MapChooserSharpMS.Shared.Events.RockTheVote.Params;
using MapChooserSharpMS.Shared.MapConfig.Services;
using MapChooserSharpMS.Shared.RockTheVote;
using MapChooserSharpMS.Shared.RockTheVote.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.RockTheVote.Services;

// serviceProviderAccessor: the controller's ServiceProvider is REPLACED on
// every RebuildServiceProvider, and this service is constructed during
// OnInitialize — before later modules (e.g. MapCycle) have registered their
// services. Capturing the provider instance here would permanently miss
// those registrations, so always go through the accessor.
internal sealed class RtvService(
    TnmsPlugin plugin,
    IMcsInternalRtvController controller,
    InternalRtvManager rtvManager,
    IInternalEventManager eventManager,
    Func<IServiceProvider> serviceProviderAccessor,
    RtvConVars conVars
    ) : IRtvService
{
    private IServiceProvider ServiceProvider => serviceProviderAccessor();

    public RtvExecutionResult AddClientToRtv(IGameClient client)
    {
        if (rtvManager.RtvStatus == RtvStatus.AnotherVoteOngoing)
            return RtvExecutionResult.AnotherVoteOngoing;

        if (rtvManager.RtvStatus == RtvStatus.Disabled)
            return RtvExecutionResult.CommandDisabled;

        if (rtvManager.RtvStatus == RtvStatus.TriggeredWaitingForMapTransition
            && !(conVars.ImmediateChangeThreshold.GetFloat() > 0f && TransitionManager.IsNextMapConfirmed))
            return RtvExecutionResult.TriggeredWaitingForMapTransition;
        
        if (rtvManager.RtvStatus == RtvStatus.TriggeredWaitingForVote)
            return RtvExecutionResult.TriggeredWaitingForVote;
        
        if (rtvManager.RtvStatus == RtvStatus.InCooldown)
            return RtvExecutionResult.CommandInCooldown;
        
        
        bool willTrigger = rtvManager.RtvCounts + 1 >= rtvManager.RequiredCounts;
        IClientRtvCastParams @params = ActivatorUtilities.CreateInstance<ClientRtvCastParams>(ServiceProvider, plugin, controller, client, willTrigger);
        if (eventManager.FireCancellable<IRockTheVoteEventListener>(e => e.OnClientRtvCast(@params)) == McsCancellableEvent.Stop)
            return RtvExecutionResult.DisallowedByExternalConsumer;

        if (!rtvManager.AddParticipants(client))
            return RtvExecutionResult.AlreadyVoted;

        if (TransitionManager.IsNextMapConfirmed)
        {
            if (rtvManager.RtvCounts >= rtvManager.ImmediateRequiredCounts)
            {
                FireRtvConfirmed(null);
                TransitionToNextMapImmediately();
            }
            else if (rtvManager.RtvCounts >= rtvManager.RequiredCounts
                     && rtvManager.RtvStatus != RtvStatus.TriggeredWaitingForMapTransition)
            {
                FireRtvConfirmed(null);
                SetChangeOnRoundEnd();
            }
            else
                BroadcastPostVoteProgress(client);
        }
        else if (rtvManager.RtvCounts >= rtvManager.RequiredCounts)
        {
            InitiateRtvVote();
        }
        else
        {
            BroadcastProgress(client);
        }

        return RtvExecutionResult.Success;
    }

    public RtvExecutionResult AddClientToRtv(int slot)
    {
        var client = plugin.SharedSystem.GetClientManager().GetGameClient(new PlayerSlot((byte)slot));

        if (client == null)
            return RtvExecutionResult.NotAllowed;
        
        return AddClientToRtv(client);
    }

    public bool RemoveClientFromRtv(IGameClient client, IGameClient? enforcer = null)
    {
        IClientRtvUnCastParams @params;
        
        if (enforcer != null)
        {
            @params = ActivatorUtilities.CreateInstance<ClientRtvUnCastParams>(ServiceProvider, plugin, controller, client, true, enforcer);
        }
        else
        {
            @params = ActivatorUtilities.CreateInstance<ClientRtvUnCastParams>(ServiceProvider, plugin, controller, client, false);
        }
        
        if (eventManager.FireCancellable<IRockTheVoteEventListener>(e => e.OnClientRtvUnCast(@params)) == McsCancellableEvent.Stop)
            return false;
        
        return rtvManager.RemoveParticipants(client);
    }

    public bool RemoveClientFromRtv(int slot)
        => rtvManager.RemoveParticipants(slot);

    public void RemoveDisconnectingClientFromRtv(IGameClient client)
    {
        if (!rtvManager.RtvParticipants.Contains(client.Slot))
            return;

        var @params = ActivatorUtilities.CreateInstance<ClientRtvUnCastParams>(ServiceProvider, plugin, controller, client, false);
        eventManager.FireCancellable<IRockTheVoteEventListener>(e => e.OnClientRtvUnCast(@params));

        rtvManager.RemoveParticipants(client);
    }

    public void InitiateRtvVote()
    {
        if (rtvManager.RtvStatus != RtvStatus.Enabled)
            return;

        rtvManager.ForceSetRtvStatus(RtvStatus.TriggeredWaitingForVote);

        BroadcastToAll("Rtv.Broadcast.VoteTriggered");

        FireRtvConfirmed(null);
    }

    private void FireRtvConfirmed(IGameClient? client, bool isForced = false)
    {
        IRtvConfirmedParams @params = new RtvConfirmedParams(plugin, (PluginModuleBase)controller, client, isForced);
        eventManager.Fire<IRockTheVoteEventListener>(e => e.OnRtvConfirmed(@params));
    }

    public void EnableRtvCommand(IGameClient? client = null, bool silently = false)
    {
        bool success = rtvManager.TrySetRtvStatus(RtvStatus.Enabled);

        if (silently)
            return;

        if (success)
            BroadcastToAll("Rtv.Broadcast.Admin.EnabledRtv", client?.Name ?? "Console");
        else
            controller.NotifyAdminCommandResult(client, "Rtv.Notification.Admin.Enable.Failure");
    }

    public void DisableRtvCommand(IGameClient? client = null, bool silently = false)
    {
        bool success = rtvManager.TrySetRtvStatus(RtvStatus.Disabled);

        if (silently)
            return;

        if (success)
            BroadcastToAll("Rtv.Broadcast.Admin.DisabledRtv", client?.Name ?? "Console");
        else
            controller.NotifyAdminCommandResult(client, "Rtv.Notification.Admin.Disable.Failure");
    }

    public void InitiateForceRtvVote(IGameClient? client)
    {
        if (rtvManager.RtvStatus is RtvStatus.TriggeredWaitingForVote
            or RtvStatus.TriggeredWaitingForMapTransition)
        {
            controller.NotifyAdminCommandResult(client, "Rtv.Notification.Admin.ForceRtv.AlreadyTriggered");
            return;
        }

        var forceParams = new ForceRtvParams(plugin, (PluginModuleBase)controller, client);
        if (eventManager.FireCancellable<IRockTheVoteEventListener>(e => e.OnForceRtv(forceParams)) == McsCancellableEvent.Stop)
            return;

        if (TransitionManager.IsNextMapConfirmed)
        {
            FireRtvConfirmed(client, isForced: true);
            TransitionToNextMapImmediately();
            return;
        }

        rtvManager.ForceSetRtvStatus(RtvStatus.TriggeredWaitingForVote);

        BroadcastToAll("Rtv.Broadcast.Admin.ForceRtv", client?.Name ?? "Console");

        FireRtvConfirmed(client, isForced: true);
    }

    private IMcsInternalMapTransitionManager TransitionManager =>
        ServiceProvider.GetRequiredService<IMcsInternalMapTransitionManager>();

    private void SetChangeOnRoundEnd()
    {
        var transitionManager = TransitionManager;
        var nextMap = transitionManager.NextMap;
        if (nextMap is null)
            return;

        rtvManager.ForceSetRtvStatus(RtvStatus.TriggeredWaitingForMapTransition);
        transitionManager.ChangeMapOnNextRoundEnd = true;

        string mapDisplayName = ServiceProvider
            .GetRequiredService<IMapConfigToolingService>()
            .ResolveMapDisplayName(nextMap.MapConfig);

        BroadcastToAll("Rtv.Broadcast.ChangeOnRoundEnd", mapDisplayName);
    }

    private void TransitionToNextMapImmediately()
    {
        var transitionManager = TransitionManager;
        var nextMap = transitionManager.NextMap;
        if (nextMap is null)
            return;

        rtvManager.ForceSetRtvStatus(RtvStatus.TriggeredWaitingForMapTransition);

        string mapDisplayName = ServiceProvider
            .GetRequiredService<IMapConfigToolingService>()
            .ResolveMapDisplayName(nextMap.MapConfig);

        var behaviour = ServiceProvider
            .GetRequiredService<IMcsPluginConfigProvider>()
            .PluginConfig.GeneralConfig.RtvMapChangeBehaviour;

        var internalTransitionManager = ServiceProvider
            .GetRequiredService<MapCycle.Managers.MapTransition.Interfaces.IMcsInternalMapTransitionManager>();

        switch (behaviour)
        {
            case RtvMapChangeBehaviourType.Cs2EndMatchScreen:
                BroadcastToAll("Rtv.Broadcast.ChangeToNextMapCs2EndMatchScreen", mapDisplayName);
                internalTransitionManager.BeginMapTransition(
                    MapCycle.Managers.MapTransition.MapTransitionTrigger.AdminForceEnd);
                break;

            case RtvMapChangeBehaviourType.ImmediatelyWithTime:
            default:
                float timing = conVars.MapChangeTimingAfterRtvSuccess.GetFloat();
                BroadcastToAll("Rtv.Broadcast.ChangeToNextMapImmediately", mapDisplayName, timing);
                internalTransitionManager.BeginMapTransition(
                    MapCycle.Managers.MapTransition.MapTransitionTrigger.RtvImmediate, timing);
                break;
        }
    }

    private void BroadcastToAll(string key, params object[] args)
    {
        var clients = plugin.SharedSystem.GetModSharp().GetIServer().GetGameClients(true);
        foreach (var c in clients)
        {
            if (c.IsFakeClient || c.IsHltv)
                continue;

            c.GetPlayerController()?.PrintToChat(
                $" {plugin.GetPluginPrefix(c)} {plugin.LocalizeStringForPlayer(c, key, args)}");
        }
    }

    private void BroadcastProgress(IGameClient caster)
    {
        if (conVars.BroadcastPlayerCast.GetInt32() == 0)
            return;

        int current = rtvManager.RtvCounts;
        int required = rtvManager.RequiredCounts;
        var clients = plugin.SharedSystem.GetModSharp().GetIServer().GetGameClients(true);
        foreach (var c in clients)
        {
            if (c.IsFakeClient || c.IsHltv)
                continue;

            c.GetPlayerController()?.PrintToChat(
                $" {plugin.GetPluginPrefix(c)} {plugin.LocalizeStringForPlayer(c, "Rtv.Notification.Progress", caster.Name, current, required)}");
        }
    }

    private void BroadcastPostVoteProgress(IGameClient caster)
    {
        if (conVars.BroadcastPlayerCast.GetInt32() == 0)
            return;

        bool hasImmediateThreshold = conVars.ImmediateChangeThreshold.GetFloat() > 0f;
        bool normalReached = rtvManager.RtvCounts >= rtvManager.RequiredCounts;

        string key;
        int required;
        if (hasImmediateThreshold && normalReached)
        {
            key = "Rtv.Notification.Progress.ChangeImmediately";
            required = rtvManager.ImmediateRequiredCounts;
        }
        else
        {
            key = "Rtv.Notification.Progress.ChangeOnRoundEnd";
            required = rtvManager.RequiredCounts;
        }

        int current = rtvManager.RtvCounts;
        var clients = plugin.SharedSystem.GetModSharp().GetIServer().GetGameClients(true);
        foreach (var c in clients)
        {
            if (c.IsFakeClient || c.IsHltv)
                continue;

            c.GetPlayerController()?.PrintToChat(
                $" {plugin.GetPluginPrefix(c)} {plugin.LocalizeStringForPlayer(c, key, caster.Name, current, required)}");
        }
    }
}
