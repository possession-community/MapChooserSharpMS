using System;
using System.Linq;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.MapCycle;
using MapChooserSharpMS.Modules.EventManager.Events.RockTheVote;
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

internal sealed class RtvService(
    TnmsPlugin plugin,
    IMcsInternalRtvController controller,
    InternalRtvManager rtvManager,
    IInternalEventManager eventManager,
    IServiceProvider serviceProvider,
    RtvConVars conVars
    ) : IRtvService
{
    public RtvExecutionResult AddClientToRtv(IGameClient client)
    {
        if (rtvManager.RtvStatus == RtvStatus.AnotherVoteOngoing)
            return RtvExecutionResult.AnotherVoteOngoing;

        if (rtvManager.RtvStatus == RtvStatus.Disabled)
            return RtvExecutionResult.CommandDisabled;

        if (rtvManager.RtvStatus == RtvStatus.TriggeredWaitingForMapTransition)
            return RtvExecutionResult.TriggeredWaitingForMapTransition;
        
        if (rtvManager.RtvStatus == RtvStatus.TriggeredWaitingForVote)
            return RtvExecutionResult.TriggeredWaitingForVote;
        
        if (rtvManager.RtvStatus == RtvStatus.InCooldown)
            return RtvExecutionResult.CommandInCooldown;
        
        
        IClientRtvCastParams @params = ActivatorUtilities.CreateInstance<ClientRtvCastParams>(serviceProvider, plugin, controller, client, false);
        bool cancelled = eventManager.FireCancellable<IRockTheVoteEventListener>(e => e.OnClientRtvCast(@params));
        if (cancelled)
            return RtvExecutionResult.DisallowedByExternalConsumer;

        if (!rtvManager.AddParticipants(client))
            return RtvExecutionResult.AlreadyVoted;

        if (rtvManager.RtvCounts >= rtvManager.RequiredCounts)
        {
            // When the next map is already decided there is nothing to vote on —
            // RTV success means "go to the confirmed next map now" (old MCS behaviour).
            if (TransitionManager.IsNextMapConfirmed)
                ChangeToNextMap();
            else
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
        var client = plugin.SharedSystem.GetClientManager().GetGameClient(new PlayerSlot(slot));

        if (client == null)
            return RtvExecutionResult.NotAllowed;
        
        return AddClientToRtv(client);
    }

    public bool RemoveClientFromRtv(IGameClient client, IGameClient? enforcer = null)
    {
        IClientRtvUnCastParams @params;
        
        if (enforcer != null)
        {
            @params = ActivatorUtilities.CreateInstance<ClientRtvUnCastParams>(serviceProvider, plugin, controller, client, true, enforcer);
        }
        else
        {
            @params = ActivatorUtilities.CreateInstance<ClientRtvUnCastParams>(serviceProvider, plugin, controller, client, false);
        }
        
        bool cancelled = eventManager.FireCancellable<IRockTheVoteEventListener>(e => e.OnClientRtvUnCast(@params));

        if (cancelled)
            return false;
        
        return rtvManager.RemoveParticipants(client);
    }

    public bool RemoveClientFromRtv(int slot)
        => rtvManager.RemoveParticipants(slot);

    public void InitiateRtvVote()
    {
        if (rtvManager.RtvStatus != RtvStatus.Enabled)
            return;

        rtvManager.ForceSetRtvStatus(RtvStatus.TriggeredWaitingForVote);

        BroadcastToAll("Rtv.Broadcast.VoteTriggered");

        IRtvConfirmedParams @params = new RtvConfirmedParams(plugin, (PluginModuleBase)controller, null, false);
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
        var forceParams = new ForceRtvParams(plugin, (PluginModuleBase)controller, client);
        bool cancelled = eventManager.FireCancellable<IRockTheVoteEventListener>(e => e.OnForceRtv(forceParams));

        if (cancelled)
            return;

        if (TransitionManager.IsNextMapConfirmed)
        {
            ChangeToNextMap();
            return;
        }

        rtvManager.ForceSetRtvStatus(RtvStatus.TriggeredWaitingForVote);

        BroadcastToAll("Rtv.Broadcast.Admin.ForceRtv", client?.Name ?? "Console");

        IRtvConfirmedParams confirmedParams = new RtvConfirmedParams(plugin, (PluginModuleBase)controller, client, true);
        eventManager.Fire<IRockTheVoteEventListener>(e => e.OnRtvConfirmed(confirmedParams));
    }

    private IMcsInternalMapTransitionManager TransitionManager =>
        serviceProvider.GetRequiredService<IMcsInternalMapTransitionManager>();

    /// <summary>
    /// RTV succeeded while the next map is already confirmed — skip the map
    /// vote entirely and transition to the confirmed next map.
    /// </summary>
    private void ChangeToNextMap()
    {
        var transitionManager = TransitionManager;
        var nextMap = transitionManager.NextMap;
        if (nextMap is null)
            return;

        rtvManager.ForceSetRtvStatus(RtvStatus.TriggeredWaitingForMapTransition);

        string mapDisplayName = serviceProvider
            .GetRequiredService<IMapConfigToolingService>()
            .ResolveMapDisplayName(nextMap);

        var behaviour = serviceProvider
            .GetRequiredService<IMcsPluginConfigProvider>()
            .PluginConfig.GeneralConfig.RtvMapChangeBehaviour;

        switch (behaviour)
        {
            case RtvMapChangeBehaviourType.Cs2EndMatchScreen:
                // TODO: Faithful Cs2EndMatchScreen needs a "force end match" path
                // (old MCS wrote mp_timelimit + TerminateRound). The new TimeLimit
                // design deliberately never writes mp_timelimit, so this falls back
                // to ImmediatelyWithTime until that design question is settled.
            case RtvMapChangeBehaviourType.ImmediatelyWithTime:
            default:
                float timing = conVars.MapChangeTimingAfterRtvSuccess.GetFloat();
                BroadcastToAll("Rtv.Broadcast.ChangeToNextMapImmediately", mapDisplayName, timing);

                var intermissionParams = new McsIntermissionParams(plugin, (PluginModuleBase)controller, nextMap);
                eventManager.Fire<IMapCycleEventListener>(e => e.OnMcsIntermission(intermissionParams));

                transitionManager.ChangeMapOnNextRoundEnd = false;
                transitionManager.TransitionToNextMap(timing);
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
}
