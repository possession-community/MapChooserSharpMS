using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.Nomination;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Modules.Nomination.Managers;
using MapChooserSharpMS.Modules.Nomination.Models;
using MapChooserSharpMS.Modules.Nomination.Services;
using MapChooserSharpMS.Modules.Ui.Menu;
using MapChooserSharpMS.Shared.Events.Nomination;
using MapChooserSharpMS.Shared.Events.Nomination.Params;
using MapChooserSharpMS.Shared.Events.RockTheVote;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.Nomination;
using MapChooserSharpMS.Shared.Nomination.Managers;
using MapChooserSharpMS.Shared.Nomination.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Plugin;
using TnmsPluginFoundation.Utils.Entity;

namespace MapChooserSharpMS.Modules.Nomination;

internal sealed class McsNominationController(IServiceProvider serviceProvider, bool hotReload) : PluginModuleBase(serviceProvider, hotReload), IMcsInternalNominationController, IRockTheVoteEventListener, IGameListener
{
    public override string PluginModuleName => "McsMapNominationController";
    public override string ModuleChatPrefix => "Prefix.Nomination";
    protected override bool UseTranslationKeyInModuleChatPrefix => true;

    public int ListenerVersion => 1;
    int IGameListener.ListenerPriority => 1;
    public int ListenerPriority => 999999;

    public IMapNominationService NominationService { get; private set; } = null!;
    public INominationValidateService NominationValidateService { get; private set; } = null!;
    public INominationMenuManagementService NominationMenuManagementService { get; private set; } = null!;
    public INominationManager NominationManager => _internalNominationManager;
    
    public void InstallEventListener(INominationEventListener listener)
    {
        _eventManager.RegisterListener(listener);
    }

    public void RemoveEventListener(INominationEventListener listener)
    {
        _eventManager.RemoveListener(listener);
    }

    private IMcsInternalNominationManager _internalNominationManager = null!;
    
    private readonly Dictionary<int, IMcsNominationUserInterface> _mcsActiveUserNominationMenu = new();

    public IReadOnlyDictionary<string, IMcsNominationData> GetNominatedMaps() => NominatedMaps;
    
    private IInternalEventManager _eventManager = null!;

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IMcsInternalNominationController>(this);
        services.AddSingleton<IMcsInternalNominationManager>(_internalNominationManager);
    }

    protected override void OnInitialize()
    {
        _internalNominationManager = ActivatorUtilities.CreateInstance<InternalNominationManager>(ServiceProvider);
        
    }

    protected override void OnAllModulesLoaded()
    {
        NominationValidateService = ActivatorUtilities.CreateInstance<NominationValidateService>(ServiceProvider, this);
        
        _eventManager = ServiceProvider.GetRequiredService<IInternalEventManager>();
        
        _eventManager.RegisterListener<IRockTheVoteEventListener>(this);
    }

    protected override void OnUnloadModule()
    {
    }

    


    private void OnClientDisconnect(int slot)
    {
        foreach (IMcsNominationData data in NominatedMaps.Values)
        {
            data.NominationParticipants.Remove(slot);
        }
    }

    private bool ProcessNominationCheckResult(IGameClient player, IMapConfig mapConfig, IReadOnlyList<NominationCheckResult> check)
    {
        // Success - empty list
        if (check.Count == 0)
            return true;

        // Process each result and notify player
        int playerCountCurrently = SharedSystem.GetClientManager().GetClientCount();

        if (check.Contains(NominationCheckResult.Disabled))
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.MapDisabled", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig)));

        if (check.Contains(NominationCheckResult.NotEnoughPermissions))
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.NotEnoughPermission", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig)));

        if (check.Contains(NominationCheckResult.TooMuchPlayers))
        {
            int maxPlayers = mapConfig.NominationConfig.MaxPlayers;
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.TooMuchPlayers", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig), playerCountCurrently, maxPlayers));
        }

        if (check.Contains(NominationCheckResult.NotEnoughPlayers))
        {
            int minPlayers = mapConfig.NominationConfig.MinPlayers;
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.NotEnoughPlayers", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig), playerCountCurrently, minPlayers));
        }

        if (check.Contains(NominationCheckResult.VotingPeriod))
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.DisableAtThisTime"));

        if (check.Contains(NominationCheckResult.OnlySpecificDay))
        {
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.OnlySpecificDay", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig)));
            player.GetPlayerController()?.PrintToChat(GetTextWithModulePrefix(player, LocalizeString(player, "Nomination.Notification.Failure.OnlySpecificDay.Days", string.Join(", ", mapConfig.NominationConfig.DaysAllowed))));
        }

        if (check.Contains(NominationCheckResult.OnlySpecificTime))
        {
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.OnlySpecificTime", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig)));
            player.GetPlayerController()?.PrintToChat(GetTextWithModulePrefix(player, LocalizeString(player, "Nomination.Notification.Failure.OnlySpecificTime.Times", string.Join(", ", mapConfig.NominationConfig.AllowedTimeRanges))));
        }

        if (check.Contains(NominationCheckResult.MapIsInCooldown))
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.MapIsInCooldown", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig), GetHighestCooldown(mapConfig)));

        if (check.Contains(NominationCheckResult.AlreadyNominated))
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.AlreadyNominatedSameMap", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig)));

        if (check.Contains(NominationCheckResult.NominatedByAdmin))
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.AlreadyNominatedByAdmin", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig)));

        if (check.Contains(NominationCheckResult.SameMap))
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.SameMap"));

        if (check.Contains(NominationCheckResult.GroupNominationLimitReached))
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.GroupLimitReached", PerGroupNominationLimit.Value));

        if (check.Contains(NominationCheckResult.CancelledByExternalPlugin))
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.Generic.WithMapName", _mcsInternalMapConfigProviderApi.GetMapName(mapConfig)));

        return false;
    }
}