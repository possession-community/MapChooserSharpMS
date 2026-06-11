using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Modules.Nomination.Managers;
using MapChooserSharpMS.Modules.Nomination.Services;
using MapChooserSharpMS.Shared.Events.RockTheVote;
using MapChooserSharpMS.Shared.Events.Nomination;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapConfig.Services;
using MapChooserSharpMS.Shared.Nomination;
using MapChooserSharpMS.Shared.Nomination.Managers;
using MapChooserSharpMS.Shared.Nomination.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.Nomination;

internal sealed class McsNominationController(IServiceProvider serviceProvider, bool hotReload)
    : PluginModuleBase(serviceProvider, hotReload),
      IMcsInternalNominationController,
      IRockTheVoteEventListener,
      IClientListener
{
    public override string PluginModuleName => "McsMapNominationController";
    public override string ModuleChatPrefix => "Prefix.Nomination";
    protected override bool UseTranslationKeyInModuleChatPrefix => true;

    public int ListenerVersion => 1;
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
    private IInternalEventManager _eventManager = null!;
    private IMcsMapConfigProvider _mapConfigProvider = null!;
    private IMapConfigToolingService _mapConfigToolingService = null!;

    public IReadOnlyDictionary<string, IMcsNominationData> GetNominatedMaps() => _internalNominationManager.NominatedMaps;

    public override void RegisterServices(IServiceCollection services)
    {
        // IMcsInternalNominationController : IMcsNominationController — the
        // public API is wired by the plugin entrypoint (MapChooserSharpMs.cs),
        // which resolves this instance and casts to IMcsNominationController
        // when building the shared API object.
        services.AddSingleton<IMcsInternalNominationController>(this);
        // NominationManager is created during OnInitialize; register a factory
        // so other modules resolving IMcsInternalNominationManager via DI
        // still get the same instance.
        services.AddSingleton<IMcsInternalNominationManager>(_ => _internalNominationManager);
        services.AddSingleton<INominationManager>(_ => _internalNominationManager);
        services.AddSingleton<INominationValidateService>(_ => NominationValidateService);
    }

    protected override void OnInitialize()
    {
        _internalNominationManager = ActivatorUtilities.CreateInstance<InternalNominationManager>(ServiceProvider);
    }

    protected override void OnAllModulesLoaded()
    {
        NominationValidateService = ActivatorUtilities.CreateInstance<NominationValidateService>(ServiceProvider, this);
        NominationService          = ActivatorUtilities.CreateInstance<MapNominationService>(ServiceProvider, this, NominationValidateService);

        NominationMenuManagementService = new NominationMenuManagementService(
            () => ((MapChooserSharpMs)Plugin).MenuCompat,
            _mapConfigProvider,
            _internalNominationManager,
            NominationService,
            _mapConfigToolingService);

        _eventManager = ServiceProvider.GetRequiredService<IInternalEventManager>();
        _mapConfigProvider = ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();
        _mapConfigToolingService = ServiceProvider.GetRequiredService<IMapConfigToolingService>();

        _eventManager.RegisterListener<IRockTheVoteEventListener>(this);
        SharedSystem.GetClientManager().InstallClientListener(this);

        AddCommandsUnderNamespace("MapChooserSharpMS.Modules.Nomination.Commands");
    }

    protected override void OnUnloadModule()
    {
        _eventManager.RemoveListener<IRockTheVoteEventListener>(this);
        SharedSystem.GetClientManager().RemoveClientListener(this);
    }

    public void OnClientDisconnecting(IGameClient client, NetworkDisconnectionReason reason)
    {
        NominationService.TryUnNominate(client, UnNominateReason.PlayerDisconnect);
    }

    private bool ProcessNominationCheckResult(IGameClient player, IMapConfig mapConfig, IReadOnlyList<NominationCheckResult> check)
    {
        // Success - empty list
        if (check.Count == 0)
            return true;

        int playerCountCurrently = SharedSystem.GetClientManager().GetClientCount();
        string mapDisplay = _mapConfigToolingService.ResolveMapDisplayName(mapConfig);

        if (check.Contains(NominationCheckResult.Disabled))
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.MapDisabled", mapDisplay));

        if (check.Contains(NominationCheckResult.NotEnoughPermissions))
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.NotEnoughPermission", mapDisplay));

        if (check.Contains(NominationCheckResult.TooMuchPlayers))
        {
            int maxPlayers = mapConfig.NominationConfig.MaxPlayers;
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.TooMuchPlayers", mapDisplay, playerCountCurrently, maxPlayers));
        }

        if (check.Contains(NominationCheckResult.NotEnoughPlayers))
        {
            int minPlayers = mapConfig.NominationConfig.MinPlayers;
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.NotEnoughPlayers", mapDisplay, playerCountCurrently, minPlayers));
        }

        if (check.Contains(NominationCheckResult.VotingPeriod))
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.DisableAtThisTime"));

        if (check.Contains(NominationCheckResult.OnlySpecificDay))
        {
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.OnlySpecificDay", mapDisplay));
            player.GetPlayerController()?.PrintToChat(GetTextWithModulePrefix(player, LocalizeString(player, "Nomination.Notification.Failure.OnlySpecificDay.Days", string.Join(", ", mapConfig.NominationConfig.DaysAllowed))));
        }

        if (check.Contains(NominationCheckResult.OnlySpecificTime))
        {
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.OnlySpecificTime", mapDisplay));
            player.GetPlayerController()?.PrintToChat(GetTextWithModulePrefix(player, LocalizeString(player, "Nomination.Notification.Failure.OnlySpecificTime.Times", string.Join(", ", mapConfig.NominationConfig.AllowedTimeRanges))));
        }

        if (check.Contains(NominationCheckResult.MapIsInCooldown))
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.MapIsInCooldown", mapDisplay, _mapConfigToolingService.GetHighestCooldown(mapConfig)));

        if (check.Contains(NominationCheckResult.AlreadyNominated))
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.AlreadyNominatedSameMap", mapDisplay));

        if (check.Contains(NominationCheckResult.NominatedByAdmin))
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.AlreadyNominatedByAdmin", mapDisplay));

        if (check.Contains(NominationCheckResult.SameMap))
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.SameMap"));

        if (check.Contains(NominationCheckResult.GroupNominationLimitReached))
        {
            int limit = ((NominationValidateService)NominationValidateService).PerGroupNominationLimit.GetInt32();
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.GroupLimitReached", limit));
        }

        if (check.Contains(NominationCheckResult.CancelledByExternalPlugin))
            player.GetPlayerController()?.PrintToChat(LocalizeWithModulePrefix(player, "Nomination.Notification.Failure.Generic.WithMapName", mapDisplay));

        return false;
    }
}
