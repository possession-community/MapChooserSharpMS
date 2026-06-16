using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.Nomination;
using MapChooserSharpMS.Shared.Events.Nomination;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapConfig.Services;
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
    private readonly Action<IGameClient, IMapConfig, IReadOnlyList<NominationCheckResult>> _failureNotifier;
    private readonly NominationConVars _conVars;
    private readonly TnmsPlugin _plugin;
    private readonly PluginModuleBase _moduleBase;
    private readonly IInternalEventManager _eventManager;

    internal NominationMenuManagementService(
        Func<IMcsNominationMenuCompat?> menuCompatProvider,
        IMcsMapConfigProvider mapConfigProvider,
        INominationManager nominationManager,
        IMapNominationService nominationService,
        IMapConfigToolingService toolingService,
        Action<IGameClient, IMapConfig, IReadOnlyList<NominationCheckResult>> failureNotifier,
        NominationConVars conVars,
        TnmsPlugin plugin,
        PluginModuleBase moduleBase,
        IInternalEventManager eventManager)
    {
        _menuCompatProvider = menuCompatProvider;
        _mapConfigProvider = mapConfigProvider;
        _nominationManager = nominationManager;
        _nominationService = nominationService;
        _toolingService = toolingService;
        _failureNotifier = failureNotifier;
        _conVars = conVars;
        _plugin = plugin;
        _moduleBase = moduleBase;
        _eventManager = eventManager;
    }

    public void ShowNominationMenu(IGameClient client, List<IMapConfig> configs)
    {
        var compat = GetMenuCompatOrThrow();
        var items = configs
            .Select(c => CreateNominationMenuItem(c, client, isAdmin: false))
            .ToList();

        compat.ShowMenu(client, new McsMenuDefinition
        {
            Title = "Nominate Map",
            Items = items,
        });
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
            .Select(c => CreateNominationMenuItem(c, client, isAdmin: true))
            .ToList();

        compat.ShowMenu(client, new McsMenuDefinition
        {
            Title = "Admin Nominate Map",
            Items = items,
        });
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
            .Select(n => new McsMenuItem
            {
                DisplayText = _toolingService.ResolveMapDisplayName(n.MapConfig),
                OnSelect = c => _nominationService.TryRemoveNomination(n.MapConfig, c, forceRemoval: true),
            })
            .ToList();

        compat.ShowMenu(client, new McsMenuDefinition
        {
            Title = "Remove Nomination",
            Items = items,
        });
    }

    public void ShowRemoveNominationMenu(IGameClient client)
    {
        var nominations = _nominationManager.NominatedMaps.Values.ToList();
        ShowRemoveNominationMenu(client, nominations);
    }

    private McsMenuItem CreateNominationMenuItem(IMapConfig config, IGameClient client, bool isAdmin)
    {
        return new McsMenuItem
        {
            DisplayText = _toolingService.ResolveMapDisplayName(config),
            OnSelect = isAdmin
                ? c => ExecuteNomination(c, config, true)
                : c => NominateOrConfirm(c, config, false),
        };
    }

    public void NominateOrConfirm(IGameClient client, IMapConfig config, bool isAdmin)
    {
        if (!isAdmin && _conVars.ConfirmMenu.GetInt32() != 0)
        {
            string displayName = _toolingService.ResolveMapDisplayName(config);
            ShowConfirmMenu(client, config, displayName);
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

    private void ShowConfirmMenu(IGameClient client, IMapConfig config, string displayName)
    {
        var compat = GetMenuCompatOrThrow();

        var extraItems = CollectExtraMenuItems(config, client);

        var items = new List<McsMenuItem>
        {
            new()
            {
                DisplayText = "Yes",
                OnSelect = c => ExecuteNomination(c, config, false),
            },
            new()
            {
                DisplayText = "No",
                OnSelect = _ => { },
            },
        };

        items.AddRange(extraItems);

        compat.ShowMenu(client, new McsMenuDefinition
        {
            Title = $"Nominate {displayName}?",
            Items = items,
        });
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
