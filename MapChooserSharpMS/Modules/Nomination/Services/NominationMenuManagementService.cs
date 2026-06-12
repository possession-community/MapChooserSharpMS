using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapConfig.Services;
using MapChooserSharpMS.Shared.Nomination;
using MapChooserSharpMS.Shared.Nomination.Managers;
using MapChooserSharpMS.Shared.Nomination.Services;
using MapChooserSharpMS.Shared.Ui.Menu;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Modules.Nomination.Services;

internal sealed class NominationMenuManagementService : INominationMenuManagementService
{
    private readonly Func<IMcsMenuCompat?> _menuCompatProvider;
    private readonly IMcsMapConfigProvider _mapConfigProvider;
    private readonly INominationManager _nominationManager;
    private readonly IMapNominationService _nominationService;
    private readonly IMapConfigToolingService _toolingService;
    private readonly Action<IGameClient, IMapConfig, IReadOnlyList<NominationCheckResult>> _failureNotifier;

    internal NominationMenuManagementService(
        Func<IMcsMenuCompat?> menuCompatProvider,
        IMcsMapConfigProvider mapConfigProvider,
        INominationManager nominationManager,
        IMapNominationService nominationService,
        IMapConfigToolingService toolingService,
        Action<IGameClient, IMapConfig, IReadOnlyList<NominationCheckResult>> failureNotifier)
    {
        _menuCompatProvider = menuCompatProvider;
        _mapConfigProvider = mapConfigProvider;
        _nominationManager = nominationManager;
        _nominationService = nominationService;
        _toolingService = toolingService;
        _failureNotifier = failureNotifier;
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
            OnSelect = c =>
            {
                var results = isAdmin
                    ? _nominationService.TryAdminNominateMap(c, config)
                    : _nominationService.TryNominateMap(c, config);

                if (results.Count > 0)
                    _failureNotifier(c, config, results);
            },
        };
    }

    private IMcsMenuCompat GetMenuCompatOrThrow()
    {
        var compat = _menuCompatProvider();
        if (compat is null)
            throw new InvalidOperationException(
                "No IMcsMenuCompat registered. Ensure a companion menu plugin " +
                "(e.g. McsFPMCompat) calls IMapChooserSharpShared.SetDefaultMenuCompat " +
                "during OnAllModulesLoaded.");
        return compat;
    }
}
