# Menu Integration

MCS separates menu rendering from the plugin core through an abstraction layer.
This allows server operators to choose their preferred menu plugin (FPM MenuManager, Wuling IMenu, etc.).
MCS internally builds `McsMenuDefinition` instances and delegates actual rendering to the registered `IMcsMenuCompat` implementation.

---

## Overview

1. A companion module (e.g. `McsFPMCompat`) creates an `IMcsMenuCompat` implementation in `OnAllModulesLoaded`
2. It registers the implementation via `IMapChooserSharpShared.SetDefaultMenuCompat()`
3. When MCS needs to display a menu, it calls `ShowMenu()` on the registered implementation

If MCS attempts to display a menu before any implementation is registered, an `InvalidOperationException` is thrown.

---

## IMcsMenuCompat

Abstract interface for menu rendering. One implementation per menu plugin.

**Namespace**: `MapChooserSharpMS.Shared.Ui.Menu`

| Method | Description |
|---|---|
| `ShowMenu(IGameClient target, McsMenuDefinition menu)` | Display `menu` to `target`. If a menu is already open for this client via this compat, close it first before showing the new one |
| `CloseMenu(IGameClient target)` | Close the currently open MCS menu for `target`. No-op when no menu is open for this client |
| `Cleanup()` | Discard all cached menu state. Called by MCS during plugin unload or map changes |

### When MCS Calls Each Method

- **ShowMenu** -- Nomination menu (`!nominate`), admin nomination menu (`!nominate_addmap`), nomination removal menu (`!nominate_removemap`), and nomination confirmation menu display
- **CloseMenu** -- When MCS explicitly needs to close a menu (in practice, the `OnSelect` callback pattern inside the implementation is more common)
- **Cleanup** -- During plugin shutdown or map changes to clear cached state

---

## McsMenuDefinition

Declarative definition for a single menu. Built internally by MCS and passed to `IMcsMenuCompat` implementations.

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
| `OnSelect` | `Action<IGameClient>?` | Callback invoked when the client selects this item. The argument is the same client passed to `ShowMenu`. `null` means selecting the item does nothing |

---

## Registration Flow

Register a menu implementation via `IMapChooserSharpShared.SetDefaultMenuCompat(IMcsMenuCompat)`.
This should be called once during `OnAllModulesLoaded`. Calling again replaces the previous implementation.

```csharp
public void OnAllModulesLoaded()
{
    var mcs = _sharedSystem.GetSharpModuleManager()
        .GetRequiredSharpModuleInterface<IMapChooserSharpShared>(
            IMapChooserSharpShared.ModSharpModuleIdentity).Instance!;

    mcs.SetDefaultMenuCompat(new MyMenuCompat());
}
```

The registered `IMcsMenuCompat` instance is used internally by MCS only. There is no public API to retrieve the registered instance.

---

## Implementation Example

Below is a complete `IMcsMenuCompat` implementation example. Adapt the rendering logic to your actual menu plugin's API.

```csharp
using System;
using System.Collections.Generic;
using MapChooserSharpMS.Shared.Ui.Menu;
using Sharp.Shared.Objects;

public sealed class MyMenuCompat : IMcsMenuCompat
{
    // Track the currently displayed menu per client
    private readonly Dictionary<IGameClient, object> _activeMenus = new();

    public void ShowMenu(IGameClient target, McsMenuDefinition menu)
    {
        // Close any existing menu first
        CloseMenu(target);

        // Build the menu using your menu plugin's API
        // (hypothetical builder shown here)
        var builder = new SomeMenuBuilder();
        builder.SetTitle(menu.Title);

        foreach (var item in menu.Items)
        {
            var onSelect = item.OnSelect;
            builder.AddItem(item.DisplayText, () =>
            {
                // Close the menu, then invoke the callback
                CloseMenu(target);
                onSelect?.Invoke(target);
            });
        }

        var built = builder.Build();
        _activeMenus[target] = built;

        // Display via your menu plugin's API
        SomeMenuPlugin.Display(target, built);
    }

    public void CloseMenu(IGameClient target)
    {
        if (!_activeMenus.TryGetValue(target, out var menu))
            return;

        // Close via your menu plugin's API
        SomeMenuPlugin.Close(target, menu);
        _activeMenus.Remove(target);
    }

    public void Cleanup()
    {
        _activeMenus.Clear();
    }
}
```

### Implementation Notes

- When `ShowMenu` is called, if a previous menu is still open for the same client, close it before showing the new one
- `OnSelect` callbacks are expected to be invoked on the game server's main thread. If your menu plugin invokes callbacks on a worker thread, you must dispatch to the main thread
- `Cleanup` is tied to the plugin's overall lifecycle. Forgetting to clear state here can cause stale menu state to persist across map changes
