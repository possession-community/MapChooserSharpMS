# Menu Integration

MCS separates menu rendering from the plugin core through an abstraction layer.
This allows server operators to choose their preferred menu plugin (FPM MenuManager, Wuling IMenu, etc.).
MCS internally builds `McsMenuDefinition` instances and delegates actual rendering to the registered compat implementation.

---

## Overview

1. A companion module (e.g. `McsFPMCompat`) creates an `IMcsNominationMenuCompat` implementation in `OnAllModulesLoaded`
2. It registers it via `IMapChooserSharpShared.SetNominationMenuCompat()`
3. When MCS needs to display a menu, it calls `ShowMenu()` on the appropriate registered implementation

If MCS attempts to display a menu before any implementation is registered, an `InvalidOperationException` is thrown.

---

## IMcsMenuCompat (Base)

Base interface for all menu compat adapters.

**Namespace**: `MapChooserSharpMS.Shared.Ui.Menu`

| Method | Description |
|---|---|
| `ShowMenu(IGameClient target, McsMenuDefinition menu)` | Display `menu` to `target`. Close any existing menu for this client first |
| `CloseMenu(IGameClient target)` | Close the currently open MCS menu for `target`. No-op when no menu is open |
| `Cleanup()` | Discard all cached menu state. Called during plugin unload or map changes |

---

## IMcsNominationMenuCompat

Nomination-specific menu compat. Extends `IMcsMenuCompat`.

| Property | Type | Description |
|---|---|---|
| `NominationMenuService` | `INominationMenuManagementService` | Set by MCS during registration. Initialize with `null!` — MCS sets this before any menu is shown |

### CollectExtraMenuItems Example

External plugins can add items to the nomination confirm menu via the `OnNominationMenuDetailsOpening` event.
The compat implementation can also collect these items directly:

```csharp
// Inside your IMcsNominationMenuCompat.ShowMenu implementation:
var extras = NominationMenuService.CollectExtraMenuItems(mapConfig, target);
foreach (var extra in extras)
{
    builder.AddItem(extra.DisplayText, () =>
    {
        extra.OnSelect?.Invoke(target);
    });
}
```

---

## McsMenuDefinition

Declarative definition for a single menu. Built internally by MCS and passed to compat implementations.

**Namespace**: `MapChooserSharpMS.Shared.Ui.Menu`

| Property | Type | Description |
|---|---|---|
| `Title` | `string` | Menu title string |
| `Items` | `IReadOnlyList<McsMenuItem>` | List of menu items |

Both `Title` and `Items` are `required init` properties.

---

## McsMenuItem

A single row in a `McsMenuDefinition`. Display text is pre-resolved by MCS (no translation key indirection at this layer).

**Namespace**: `MapChooserSharpMS.Shared.Ui.Menu`

| Property | Type | Description |
|---|---|---|
| `DisplayText` | `string` | Display text (already translated) |
| `OnSelect` | `Action<IGameClient>?` | Callback invoked when the client selects this item. `null` means no-op |

---

## Registration Flow

Register menu implementations via `IMapChooserSharpShared`:

```csharp
public void OnAllModulesLoaded()
{
    var mcs = _sharedSystem.GetSharpModuleManager()
        .GetRequiredSharpModuleInterface<IMapChooserSharpShared>(
            IMapChooserSharpShared.ModSharpModuleIdentity).Instance!;

    var menuManager = GetMenuManager(); // your menu plugin's API

    mcs.SetNominationMenuCompat(new MyNominationMenuCompat(menuManager));
}
```

---

## OnNominationMenuDetailsOpening Event

Fired when a nomination confirm menu is about to be shown. External plugins can add extra items.

Implement `INominationEventListener.OnNominationMenuDetailsOpening`:

```csharp
public void OnNominationMenuDetailsOpening(INominationMenuDetailsOpeningParams @params)
{
    // Add a "Map Info" item to the confirm menu
    @params.ExtraItems.Add(new McsMenuItem
    {
        DisplayText = $"Cooldown: {@params.MapConfig.CooldownConfig.CurrentCooldown}",
        OnSelect = _ => { },
    });
}
```

---

## Implementation Example

Below is a nomination menu compat implementation example:

```csharp
public sealed class MyNominationMenuCompat : IMcsNominationMenuCompat
{
    private readonly Dictionary<IGameClient, object> _activeMenus = new();

    // Initialized with null! — MCS sets this during registration
    public INominationMenuManagementService NominationMenuService { get; set; } = null!;

    public void ShowMenu(IGameClient target, McsMenuDefinition menu)
    {
        CloseMenu(target);

        var builder = new SomeMenuBuilder();
        builder.SetTitle(menu.Title);

        foreach (var item in menu.Items)
        {
            var onSelect = item.OnSelect;
            builder.AddItem(item.DisplayText, () =>
            {
                CloseMenu(target);
                onSelect?.Invoke(target);
            });
        }

        var built = builder.Build();
        _activeMenus[target] = built;
        SomeMenuPlugin.Display(target, built);
    }

    public void CloseMenu(IGameClient target)
    {
        if (!_activeMenus.TryGetValue(target, out var menu))
            return;
        SomeMenuPlugin.Close(target, menu);
        _activeMenus.Remove(target);
    }

    public void Cleanup() => _activeMenus.Clear();
}
```
