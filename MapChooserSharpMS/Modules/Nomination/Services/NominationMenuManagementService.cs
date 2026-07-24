using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.Nomination;
using MapChooserSharpMS.Shared.Events.Nomination;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapConfig.Services;
using MapChooserSharpMS.Shared.MapCycle.Services;
using MapChooserSharpMS.Shared.Nomination;
using MapChooserSharpMS.Shared.Nomination.Managers;
using MapChooserSharpMS.Shared.Nomination.Services;
using MapChooserSharpMS.Shared.Ui.Menu;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.Nomination.Services;

internal sealed class NominationMenuManagementService : INominationMenuManagementService
{
    private readonly Func<IMcsNominationMenuCompat?> _menuCompatProvider;
    private readonly IMcsMapConfigProvider _mapConfigProvider;
    private readonly INominationManager _nominationManager;
    private readonly IMapNominationService _nominationService;
    private readonly IMapConfigToolingService _toolingService;
    private readonly IMapCooldownQueryService _cooldownQueryService;
    private readonly Action<IGameClient, IMapConfig, IReadOnlyList<NominationCheckResult>> _failureNotifier;
    private readonly TnmsPlugin _plugin;
    private readonly PluginModuleBase _moduleBase;
    private readonly IInternalEventManager _eventManager;

    internal NominationMenuManagementService(
        Func<IMcsNominationMenuCompat?> menuCompatProvider,
        IMcsMapConfigProvider mapConfigProvider,
        INominationManager nominationManager,
        IMapNominationService nominationService,
        IMapConfigToolingService toolingService,
        IMapCooldownQueryService cooldownQueryService,
        Action<IGameClient, IMapConfig, IReadOnlyList<NominationCheckResult>> failureNotifier,
        TnmsPlugin plugin,
        PluginModuleBase moduleBase,
        IInternalEventManager eventManager)
    {
        _menuCompatProvider = menuCompatProvider;
        _mapConfigProvider = mapConfigProvider;
        _nominationManager = nominationManager;
        _nominationService = nominationService;
        _toolingService = toolingService;
        _cooldownQueryService = cooldownQueryService;
        _failureNotifier = failureNotifier;
        _plugin = plugin;
        _moduleBase = moduleBase;
        _eventManager = eventManager;
    }

    public void ShowNominationMenu(IGameClient client, List<IMapConfig> configs)
    {
        var compat = GetMenuCompatOrThrow();
        var items = configs
            .Select(c => CreateNominationMenuItem(c, isAdmin: false))
            .ToList();

        compat.ShowNominationMenu(client, BuildContext("Nominate Map", items));
    }

    public void ShowNominationMenu(IGameClient client)
    {
        var allMaps = _mapConfigProvider.GetMapConfigs()
            .Select(kv => kv.Value.First().MapConfig)
            .ToList();
        ShowNominationMenu(client, allMaps);
    }

    public void ShowAdminNominationMenu(IGameClient client, List<IMapConfig> configs)
    {
        var compat = GetMenuCompatOrThrow();
        var items = configs
            .Select(c => CreateNominationMenuItem(c, isAdmin: true))
            .ToList();

        compat.ShowNominationMenu(client, BuildContext("Admin Nominate Map", items));
    }

    public void ShowAdminNominationMenu(IGameClient client)
    {
        var allMaps = _mapConfigProvider.GetMapConfigs()
            .Select(kv => kv.Value.First().MapConfig)
            .ToList();
        ShowAdminNominationMenu(client, allMaps);
    }

    public void ShowRemoveNominationMenu(IGameClient client, List<IMcsNominationData> configs)
    {
        var compat = GetMenuCompatOrThrow();
        var items = configs
            .Select(n => new McsNominationMenuItem
            {
                DisplayText = _toolingService.ResolveMapDisplayName(n.MapConfig),
                MapConfig = n.MapConfig,
                OnNominate = c => _nominationService.TryRemoveNomination(n.MapConfig, c, forceRemoval: true),
            })
            .ToList();

        compat.ShowNominationMenu(client, BuildContext("Remove Nomination", items));
    }

    public void ShowRemoveNominationMenu(IGameClient client)
    {
        var nominations = _nominationManager.NominatedMaps.Values.ToList();
        ShowRemoveNominationMenu(client, nominations);
    }

    private McsNominationMenuItem CreateNominationMenuItem(IMapConfig config, bool isAdmin)
    {
        return new McsNominationMenuItem
        {
            DisplayText = _toolingService.ResolveMapDisplayName(config),
            MapConfig = config,
            OnNominate = isAdmin
                ? c => ExecuteNomination(c, config, true)
                : c => NominateOrConfirm(c, config, false),
        };
    }

    public void NominateOrConfirm(IGameClient client, IMapConfig config, bool isAdmin)
    {
        if (!isAdmin)
        {
            ShowConfirmMenu(client, config);
            return;
        }

        ExecuteNomination(client, config, isAdmin);
    }

    public List<McsMenuItem> CollectExtraMenuItems(IMapConfig mapConfig, IGameClient client)
    {
        var eventParams = new NominationMenuDetailsOpeningParams(_plugin, _moduleBase, mapConfig, client);
        _eventManager.Fire<INominationEventListener>(e => e.OnNominationMenuDetailsOpening(eventParams));
        return eventParams.ExtraItems;
    }

    private void ShowConfirmMenu(IGameClient client, IMapConfig config)
    {
        var compat = GetMenuCompatOrThrow();
        var item = new McsNominationMenuItem
        {
            DisplayText = _toolingService.ResolveMapDisplayName(config),
            MapConfig = config,
            OnNominate = c => ExecuteNomination(c, config, false),
        };

        compat.ShowNominationMenu(client, BuildContext($"Nominate {item.DisplayText}?", [item]));
    }

    private McsNominationMenuContext BuildContext(string title, List<McsNominationMenuItem> items)
    {
        return new McsNominationMenuContext
        {
            Title = title,
            Items = items,
            ToolingService = _toolingService,
            CooldownQueryService = _cooldownQueryService,
            NominationMenuService = this,
        };
    }

    private void ExecuteNomination(IGameClient client, IMapConfig config, bool isAdmin)
    {
        var results = isAdmin
            ? _nominationService.TryAdminNominateMap(client, config)
            : _nominationService.TryNominateMap(client, config);

        if (results.Count > 0)
            _failureNotifier(client, config, results);
    }

    private IMcsNominationMenuCompat GetMenuCompatOrThrow()
    {
        var compat = _menuCompatProvider();
        if (compat is null)
            throw new InvalidOperationException(
                "No IMcsNominationMenuCompat registered. Ensure a companion menu plugin " +
                "(e.g. McsFPMCompat) calls IMapChooserSharpShared.SetNominationMenuCompat " +
                "during OnAllModulesLoaded.");
        return compat;
    }
}
