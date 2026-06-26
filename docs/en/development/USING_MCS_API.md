# MapChooserSharpMS API Guide

MapChooserSharpMS (MCS) is a map voting and selection plugin for CS2 servers.
External plugins access MCS functionality through the `MapChooserSharpMS.Shared` NuGet package.

## Setup

### 1. Add the NuGet Package

```xml
<PackageReference Include="MapChooserSharpMS.Shared" Version="*" />
```

### 2. Obtain the API Entry Point

Retrieve the MCS interface inside your ModSharp module's `OnAllModulesLoaded`.
MCS is not yet initialized at earlier lifecycle points such as `OnInitialize`, so the interface cannot be obtained there.

```csharp
private IMapChooserSharpShared? _mcs;

protected override void OnAllModulesLoaded()
{
    _mcs = GetOptionalSharpModuleInterface<IMapChooserSharpShared>(
        "MapChooserSharpMS");

    if (_mcs is null)
    {
        Logger.LogWarning("MapChooserSharpMS not found");
        return;
    }

    // MCS API is now available
}
```

### 3. IMapChooserSharpShared Overview

The retrieved `IMapChooserSharpShared` instance provides access to all subsystems.

| Property | Type | Purpose |
|---|---|---|
| `MapCycleController` | `IMapCycleController` | Map cycle management (time limits, cooldowns, map transitions) |
| `MapCycleExtendController` | `IMapCycleExtendController` | Extend budget management and extend voting |
| `McsNominationController` | `IMcsNominationController` | Nomination operations, validation, and menu display |
| `McsMapVoteController` | `IMcsMapVoteController` | Map vote initiation, cancellation, and event monitoring |
| `McsRtvController` | `IMcsRtvController` | Rock The Vote state queries and operations |
| `McsMapConfigProvider` | `IMcsMapConfigProvider` | Lookup of map/group configurations loaded from TOML |

Additionally, you can register a custom menu rendering implementation:

```csharp
_mcs.SetNominationMenuCompat(new MyNominationMenuCompat());
```

See [Menu Integration](api/menu.md) for details.

---

## Common Usage Examples

### Set the Next Map from an External Plugin

```csharp
var transition = _mcs.MapCycleController.MapTransitionManager;
if (transition.TrySetNextMap("ze_example_v1"))
{
    Logger.LogInformation("Next map set to ze_example_v1");
}
```

### Get the Current Nomination List

```csharp
var nominations = _mcs.McsNominationController.NominationManager.NominatedMaps;
foreach (var (mapName, data) in nominations)
{
    int count = data.NominationParticipants.Count;
    bool isAdmin = data.IsForceNominated;
    Logger.LogInformation("{Map}: {Count} players (admin: {Admin})", mapName, count, isAdmin);
}
```

### Search Map Configuration by Name

```csharp
var configProvider = _mcs.McsMapConfigProvider;
if (configProvider.TryGetMapConfig("ze_example_v1", out var mapConfig))
{
    string display = configProvider.ToolingService.ResolveMapDisplayName(mapConfig);
    Logger.LogInformation("Display name: {Name}, Workshop: {Id}", display, mapConfig.WorkshopId);
}
```

### Listen to Events

Each subsystem supports event listeners. Methods returning `McsCancellableEvent` are cancellable — return `Continue` to allow, `Handled` to mark as handled, or `Stop` to cancel the action.

```csharp
public class MyVoteListener : IMapVoteEventListener
{
    public int ListenerPriority => 0;

    public void OnMapConfirmed(IMapVoteMapConfirmedEventParams @params)
    {
        // Fired when the next map is confirmed by vote
        var mapName = @params.ConfirmedMap.MapName;
    }
}

// Register the listener (in OnAllModulesLoaded):
_mcs.McsMapVoteController.InstallEventListener(new MyVoteListener());
```

### Use Extra Configuration

Values defined under `[MapName.extra.SectionName]` in TOML can be read in a type-safe manner.
This is the primary mechanism for external plugins to read per-map custom data.

```toml
# maps.toml
[ze_example_v1.extra.shop]
cost = 100
discount = 20
items = ["sword", "shield"]
```

```csharp
if (configProvider.TryGetMapConfig("ze_example_v1", out var cfg))
{
    var extra = cfg.ExtraConfiguration;

    int cost = extra.GetValue<int>("shop", "cost", 0);
    var items = extra.GetArray<string>("shop", "items");

    if (extra.HasSection("shop"))
    {
        var keys = extra.GetKeys("shop"); // ["cost", "discount", "items"]
    }
}
```

### Query Detailed Cooldown Information

Map cooldown state is split between the map's own cooldown and per-group cooldowns.

```csharp
var validate = _mcs.McsNominationController.NominationValidateService;
var detail = validate.GetCooldownInformations(mapConfig);

if (detail.HasCooldown)
{
    // Map's own cooldown
    int mapCd = detail.CooldownCount;
    DateTime mapTimedCd = detail.TimedCooldown;

    // Per-group cooldowns
    foreach (var (groupName, count) in detail.GroupCooldowns)
    {
        Logger.LogInformation("Group {Group}: {Count} maps remaining", groupName, count);
    }
}
```

### Check RTV Status

```csharp
var rtv = _mcs.McsRtvController.RtvManager;
if (rtv.RtvStatus == RtvStatus.Enabled)
{
    float ratio = rtv.RtvCompletionRatio; // 0.0 - 1.0
    Logger.LogInformation("RTV progress: {Ratio:P0} ({Current}/{Required})",
        ratio, rtv.RtvCounts, rtv.RequiredCounts);
}
```

---

## API Reference (by Module)

For detailed interface definitions, see the following pages.

| Page | Contents |
|---|---|
| [Map Configuration](api/map-config.md) | `IMapConfig`, `IMapGroupConfig`, `INominationConfig`, `ICooldownConfig`, `IRandomPickConfig`, `IExtraConfigAccessor`, DaySettings overrides |
| [Nomination](api/nomination.md) | `IMcsNominationController`, `IMapNominationService`, `INominationValidateService`, `IDetailedCooldownResult` |
| [Map Vote](api/map-vote.md) | `IMcsMapVoteController`, `IMapVoteControllingService`, vote states, vote options |
| [Map Cycle](api/map-cycle.md) | `IMapCycleController`, `IMapCycleExtendController`, time limits, map transitions, cooldown operations |
| [Rock The Vote](api/rtv.md) | `IMcsRtvController`, `IRtvService`, `IRtvManager`, RTV status |
| [Events](api/events.md) | All event listeners, cancellable events, editable events, event parameter reference |
| [Menu Integration](api/menu.md) | Implementing `IMcsNominationMenuCompat`, `McsNominationMenuContext`, `McsNominationMenuItem` |
| [Workshop](api/workshop.md) | `IWorkshopFetchResult`, `ExistenceStatus`, Workshop ID-based map configuration |
