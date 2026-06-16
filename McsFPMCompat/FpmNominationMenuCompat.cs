using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Shared.Nomination.Services;
using MapChooserSharpMS.Shared.Ui.Menu;
using Sharp.Modules.MenuManager.Shared;
using Sharp.Shared.Objects;

namespace McsFPMCompat;

public sealed class FpmNominationMenuCompat(IMenuManager menuManager) : IMcsNominationMenuCompat
{
    private readonly Dictionary<IGameClient, Menu> _activeMenus = new();

    public INominationMenuManagementService NominationMenuService { get; set; } = null!;

    public void ShowNominationMenu(IGameClient target, McsNominationMenuContext context)
    {
        var builder = Menu.Create();
        builder.Title(context.Title);

        foreach (var item in context.Items)
        {
            var capturedItem = item;
            var capturedContext = context;
            builder.Item(item.DisplayText, controller =>
            {
                controller.Exit();
                ShowDetailMenu(target, capturedItem, capturedContext);
            });
        }

        var built = builder.Build();
        _activeMenus[target] = built;
        menuManager.DisplayMenu(target, built);
    }

    private void ShowDetailMenu(IGameClient target, McsNominationMenuItem item, McsNominationMenuContext context)
    {
        var builder = Menu.Create();
        builder.Title(item.DisplayText);

        builder.Item("» Nominate", controller =>
        {
            controller.Exit();
            item.OnNominate?.Invoke(target);
        });

        builder.Item("« Back", controller =>
        {
            controller.Exit();
            ShowNominationMenu(target, context);
        });

        var cooldownResult = context.CooldownQueryService.GetCurrentCooldowns(item.MapConfig);
        if (cooldownResult.HighestCooldownCount > 0)
            builder.Item($"Cooldown: {cooldownResult.HighestCooldownCount} maps", _ => { });

        if (cooldownResult.LongestTimedCooldown > System.DateTime.UtcNow)
            builder.Item($"Restricted until: {cooldownResult.LongestTimedCooldown.ToLocalTime():yyyy/MM/dd HH:mm}", _ => { });

        var tags = item.MapConfig.SearchTags;
        if (tags.Count > 0)
            builder.Item($"Tags: {string.Join(", ", tags)}", _ => { });

        var groups = item.MapConfig.GroupSettings;
        if (groups.Count > 0)
            builder.Item($"Groups: {string.Join(", ", groups.Select(g => g.GroupName))}", _ => { });

        var extras = NominationMenuService.CollectExtraMenuItems(item.MapConfig, target);
        foreach (var extra in extras)
        {
            var capturedExtra = extra;
            builder.Item(extra.DisplayText, controller =>
            {
                controller.Exit();
                capturedExtra.OnSelect?.Invoke(target);
            });
        }

        var built = builder.Build();
        _activeMenus[target] = built;
        menuManager.DisplayMenu(target, built);
    }

    public void CloseMenu(IGameClient target)
    {
        if (!_activeMenus.TryGetValue(target, out var menu))
            return;

        if (menuManager.IsInCurrentMenu(target, menu))
            menuManager.QuitMenu(target);

        _activeMenus.Remove(target);
    }

    public void Cleanup()
    {
        _activeMenus.Clear();
    }
}
