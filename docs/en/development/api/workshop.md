# Workshop API

MCS supports setting maps by Steam Workshop ID. When a Workshop ID is provided, MCS first checks its in-memory configuration for a matching map. If not found, it falls back to fetching map information from the Steam Workshop via the IPublishedFileService HTTP API.

---

## Workshop Fetch Flow

When `IMapTransitionManager.TrySetNextMap(long workshopId)` is called:

1. **In-memory lookup**: MCS searches its loaded map configurations for a map with a matching `WorkshopId`
2. **Steam Workshop fallback**: If no in-memory match is found, MCS sends an HTTP request to the Steam IPublishedFileService API to check the workshop item's existence and retrieve its name
3. **Provisional config generation**: If the workshop item is found, a provisional `IMapConfig` is generated from the fetched data

The result is returned as a tuple of `(bool Success, IWorkshopFetchResult FetchResult)`. `FetchResult` always carries the resolution outcome regardless of whether the operation succeeded.

---

## IWorkshopFetchResult

Result of a Workshop ID resolution. Returned by `IMapTransitionManager.TrySetNextMap(long workshopId)`.

**Namespace**: `MapChooserSharpMS.Shared.WorkshopManagement`

| Property | Type | Description |
|---|---|---|
| `ExistenceStatus` | `ExistenceStatus` | How the map was resolved (or why resolution failed) |
| `MapName` | `string?` | The resolved map name. `null` when resolution failed |
| `WorkshopId` | `long?` | The Workshop ID that was looked up. `null` in error scenarios |

---

## ExistenceStatus

Enum representing the outcome of a Workshop ID resolution.

**Namespace**: `MapChooserSharpMS.Shared.WorkshopManagement`

| Value | Description |
|---|---|
| `FoundInMemoryConfig` | The map was found in the in-memory configuration (loaded from TOML). No HTTP request was needed |
| `FoundInWorkshop` | The map was found via the Steam Workshop API. A provisional map config was generated |
| `NotAvailableInWorkshop` | The workshop item is not available (private, deleted, or does not exist) |
| `FailedToFetchHttpError` | An HTTP error occurred while fetching from the Steam API |
| `FailedToFetchUnknown` | The fetch failed for an unknown reason |

---

## Usage Examples

### Set Next Map by Workshop ID

```csharp
var transition = _mcs.MapCycleController.MapTransitionManager;
var (success, fetchResult) = await transition.TrySetNextMap(3070123456L);

if (success)
{
    Logger.LogInformation("Next map set to {Map} (via {Source})",
        fetchResult.MapName, fetchResult.ExistenceStatus);
}
else
{
    switch (fetchResult.ExistenceStatus)
    {
        case ExistenceStatus.NotAvailableInWorkshop:
            Logger.LogWarning("Workshop item not available (private/deleted)");
            break;
        case ExistenceStatus.FailedToFetchHttpError:
            Logger.LogWarning("Failed to fetch from Steam API");
            break;
        case ExistenceStatus.FailedToFetchUnknown:
            Logger.LogWarning("Unknown fetch failure");
            break;
    }
}
```

### Check Existence Without Setting

The `TrySetNextMap(long)` method both resolves and sets the map. If you only need to check existence, you can use `IMcsMapConfigProvider.TryGetMapConfig(long, out IMapConfig)` for the in-memory check:

```csharp
var configProvider = _mcs.McsMapConfigProvider;
if (configProvider.TryGetMapConfig(3070123456L, out var mapConfig))
{
    // Map exists in configuration
    Logger.LogInformation("Found: {Map}", mapConfig.MapName);
}
```

Note that this only checks in-memory configuration. Workshop-only maps that are not in the TOML config files will not be found this way.

---

## Workshop-Related Commands

MCS provides several built-in commands that use Workshop ID resolution:

- `!setnextwsmap <workshopId>` -- Set next map by Workshop ID (admin)
- `!wsmap <workshopId>` -- Immediately change to a Workshop map (admin)
- `!nominate_addwsmap <workshopId>` -- Admin-nominate a Workshop map (admin)

These commands internally use the same `TrySetNextMap(long)` flow described above.
