using System;
using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapConfig.Services;
using Sharp.Shared.Objects;
using Wuling.Abstract.Tianshi.Menu;
using Wuling.Abstract.Tianshi.Registry;

namespace MapChooserSharpMS.Modules.MapCycle.Services;

/// <summary>
/// Lightweight Wuling-menu picker shown when a map-name query matches
/// several maps: one row per candidate, selecting a row runs the command's
/// action for that map.
/// </summary>
internal sealed class McsMapSelectMenuService(
    IMenu menu,
    IRegistry registry,
    IMapConfigToolingService toolingService)
{
    internal void ShowMapSelectMenu(
        IGameClient client,
        string title,
        IReadOnlyList<IMapConfig> maps,
        Action<IGameClient, IMapConfig> onSelect)
    {
        var player = registry.GetPlayer(client);
        if (player is null)
            return;

        var instance = menu.CreateMenu();
        instance.Title = title;

        foreach (var map in maps)
        {
            var captured = map;
            instance.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber,
                toolingService.ResolveMapDisplayName(captured),
                (menuInstance, _, _, _) =>
                {
                    menuInstance.Close();
                    onSelect(client, captured);
                });
        }

        instance.DisplayToPlayer(player);
    }
}
