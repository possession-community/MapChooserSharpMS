# Map Cycle API

The MCS map cycle module handles all aspects of map cycle management: time limit management, map transitions, extends, and cooldowns.
External plugins access it via `IMapChooserSharpShared.MapCycleController` and `IMapChooserSharpShared.MapCycleExtendController`.

---

## IMapCycleController

Public facade for the map cycle module. Provides access points to subsystems and event listener registration.

| Member | Type | Description |
|---|---|---|
| `CurrentMapTimeLimitManager` | `ITimeLimitManager` | Time limit manager for the current map |
| `MapTransitionManager` | `IMapTransitionManager` | Manager for map transitions |
| `MapCooldownQueryService` | `IMapCooldownQueryService` | Service for querying cooldown state |
| `MapCooldownCommandService` | `IMapCooldownCommandService` | Service for modifying cooldowns |
| `CooldownStore` | `IMcsCooldownStore` | Runtime cooldown state store (map/group name keyed) |
| `InstallEventListener(IMapCycleEventListener)` | `void` | Register a map cycle event listener |
| `RemoveEventListener(IMapCycleEventListener)` | `void` | Unregister a listener |

---

## IMapCycleExtendController

Public facade for the map extend system. Three independent extend paths exist, each consuming a different budget.

| Path | Budget | Consumed By |
|---|---|---|
| Map vote "Extend Map" option | `MaxExtends` (`ExtendsLeft`) | When Extend wins the map vote |
| `!ext` command (player participation) | `MaxExtCommandUses` (`ExtCommandUsesLeft`) | When the participation threshold is met and extend executes |
| Admin path (`TryExtendCurrentMap` / `!ve` vote) | No budget consumed | Admin direct extend, or extend vote passes |

### Properties

| Property | Type | Description |
|---|---|---|
| `ExtendsLeft` | `int` | Remaining vote-based extends for the current map |
| `ExtCommandUsesLeft` | `int` | Remaining `!ext` command extends for the current map |
| `IsExtendVoteInProgress` | `bool` | Whether an extend vote (native Yes/No vote) is in progress |
| `IsExtEnabled` | `bool` | Whether the `!ext` command is currently accepting participants |

### Methods

| Method | Return Type | Description |
|---|---|---|
| `TryExtendCurrentMap(int? overrideAmount = null)` | `McsMapExtendResult` | Admin/API extend entry point. Does NOT consume any extend budget. `overrideAmount` overrides the extend amount (null = use config default) |
| `SetExtCommandUsesLeft(int count)` | `void` | Directly set the remaining `!ext` command uses |
| `EnableExt()` | `void` | Enable the `!ext` command |
| `DisableExt()` | `void` | Disable the `!ext` command. Existing participants are not cleared |
| `StartExtendVote(IGameClient? initiator = null, int? overrideAmount = null)` | `McsExtendVoteStartResult` | Start a native Yes/No extend vote (admin-only entry point). On pass, the map is extended via the admin path -- no budget consumed |
| `CancelExtendVote(IGameClient? canceller = null)` | `bool` | Cancel the in-progress extend vote. Returns `true` when a vote was in progress and got cancelled |

---

## McsMapExtendResult

Enum representing the result of `TryExtendCurrentMap`.

| Value | Description |
|---|---|
| `Extended` | Map extended successfully |
| `NoExtendsLeft` | Vote-based extends exhausted (`MaxExtends` depleted) |
| `NoExtCommandUsesLeft` | `!ext` command extends exhausted (`MaxExtCommandUses` depleted) |
| `TimeLimitNotActive` | No active time/round limit to extend (map cycle mode is none, or the limit manager is not initialized) |

---

## McsExtendVoteStartResult

Enum representing the result of `StartExtendVote`.

| Value | Description |
|---|---|
| `Started` | Extend vote started successfully |
| `AnotherVoteInProgress` | A map vote or other native vote is in progress, or the next map is already confirmed |
| `ExtendVoteAlreadyInProgress` | An extend vote is already in progress |
| `TimeLimitNotActive` | No active time/round limit to extend |
| `FailedToInitiateNativeVote` | Failed to initiate the native vote (NativeVoteManager unavailable or refused) |

---

## IMapTransitionManager

Manages map transitions: setting, confirming, and executing the next map.

### Typical Map Transition Flow

1. Call `TrySetNextMap()` to confirm the next map
2. Set `ChangeMapOnNextRoundEnd = true` (auto-transition at round end)
3. Or call `TransitionToNextMap(seconds)` for an immediate countdown transition

### Properties

| Property | Type | Description |
|---|---|---|
| `NextMap` | `IMapInformation?` | Next map information including nominator metadata. `null` until a next map is confirmed |
| `CurrentMap` | `IMapInformation?` | Current map information. `null` if MCS has no config for the current map |
| `IsNextMapConfirmed` | `bool` | Whether the next map is confirmed |
| `ChangeMapOnNextRoundEnd` | `bool` (get/set) | When `true`, the map transitions to the next map at round end |

### Methods

| Method | Return Type | Description |
|---|---|---|
| `TrySetNextMap(IMapInformation)` | `bool` | Set the next map with full metadata (nominator info, etc.) |
| `TrySetNextMap(IMapConfig)` | `bool` | Set the next map from a given `IMapConfig` (no nominator metadata) |
| `TrySetNextMap(string)` | `bool` | Look up a map by name and set it as the next map |
| `TrySetNextMap(long)` | `Task<(bool Success, IWorkshopFetchResult FetchResult)>` | Set the next map by Workshop ID. Searches in-memory config first, then falls back to Steam Workshop HTTP fetch |
| `TryRemoveNextMap()` | `bool` | Remove the confirmed next map |
| `TransitionToNextMap(float seconds)` | `void` | Initiate a map change with the given delay in seconds. Silently does nothing if no next map is set |

### IMapInformation

A wrapper interface that pairs a map config with contextual metadata such as who nominated it. `IMapTransitionManager.NextMap` and `CurrentMap` return this type.

| Property | Type | Description |
|---|---|---|
| `MapConfig` | `IMapConfig` | The map configuration data |
| `NominatorSteamIds` | `IReadOnlyList<ulong>` | SteamIDs of players who nominated this map (in nomination order). Empty for admin-set, random pick, or API-set maps |

Create instances using the `MapInformation.For(IMapConfig)` builder:

```csharp
var info = MapInformation.For(mapConfig)
    .WithNominator(steamId)       // single nominator
    .Build();

var info2 = MapInformation.For(mapConfig)
    .WithNominators(steamIdList)  // multiple nominators
    .Build();

transition.TrySetNextMap(info);
```

---

## ITimeLimitManager

Base interface for the time limit manager corresponding to the current map cycle mode.
For specific operations, check `TimeLimitType` and cast to the appropriate derived interface.

| Member | Type | Description |
|---|---|---|
| `TimeLimitType` | `TimeLimitType` | Current time limit type |
| `IsLimitReached` | `bool` | Whether the limit has been reached |

### Cast Pattern

```csharp
var manager = mapCycleController.CurrentMapTimeLimitManager;

switch (manager.TimeLimitType)
{
    case TimeLimitType.Time:
        var timeManager = (ITimeBasedTimeLimitManager)manager;
        // timeManager.TimeLeft, timeManager.Extend(TimeSpan), etc.
        break;
    case TimeLimitType.Round:
        var roundManager = (IRoundTimeLimitManager)manager;
        // roundManager.RoundsLeft, roundManager.Extend(int), etc.
        break;
}
```

### TimeLimitType

| Value | Description |
|---|---|
| `Time` | Time-based limit (mp_timelimit equivalent). Cast to `ITimeBasedTimeLimitManager` |
| `Round` | Round-based limit (mp_maxrounds equivalent). Cast to `IRoundTimeLimitManager` |

---

## ITimeBasedTimeLimitManager

Extends `ITimeLimitManager` with time-based limit operations.

| Member | Type | Description |
|---|---|---|
| `TimeLeft` | `TimeSpan` | Time remaining |
| `Extend(TimeSpan)` | `bool` | Extend the limit by the specified duration |
| `Set(TimeSpan)` | `bool` | Set the limit to the specified value directly |
| `GetFormattedTimeLeft(CultureInfo?)` | `string` | Returns a localized string of the remaining time |

---

## IRoundTimeLimitManager

Extends `ITimeLimitManager` with round-based limit operations.

| Member | Type | Description |
|---|---|---|
| `RoundsLeft` | `int` | Rounds remaining |
| `Extend(int)` | `bool` | Extend the limit by the specified number of rounds |
| `Set(int)` | `bool` | Set the limit to the specified value directly |
| `GetFormattedRoundsLeft(CultureInfo?)` | `string` | Returns a localized string of the remaining rounds |

---

## IMcsCooldownStore

Runtime cooldown state store, keyed by map/group **name**. State lives independently of map config objects: it is shared across DaySettings variants of the same map and survives config reloads.

Two layers are exposed:

- **Effective** — this server's own state combined with cooldown records loaded from other servers matched by the configured cooldown scope (see `[Cooldown]` in the plugin config). Per field, the most restrictive value wins. This is what pickup/nomination checks and `!mapinfo` use.
- **Own** — this server's raw state only; the values persisted under this server's key. Useful for debugging (`!mcsdebug config`).

With the default scope (`Exact` + empty pattern) both layers are identical.

All members must be called from the game thread. Returned states are read-only snapshots — use `IMapCooldownCommandService` to modify cooldowns.

| Method | Return Type | Description |
|---|---|---|
| `GetEffectiveMapState(string)` | `IMcsCooldownState` | Scope-aggregated effective state for a map. Zero-value state if unknown |
| `GetEffectiveGroupState(string)` | `IMcsCooldownState` | Scope-aggregated effective state for a group |
| `GetOwnMapState(string)` | `IMcsCooldownState` | This server's own raw state for a map |
| `GetOwnGroupState(string)` | `IMcsCooldownState` | This server's own raw state for a group |

### IMcsCooldownState

| Property | Type | Description |
|---|---|---|
| `CurrentCooldown` | `int` | Current count-based cooldown. `int.MaxValue` = excluded from nomination |
| `TimedCooldownEndUtc` | `DateTime` | UTC end of the timed cooldown. `DateTime.MinValue` when none |
| `LastPlayedAt` | `DateTime` | UTC timestamp of last play. `DateTime.MinValue` if never |
| `UnplayedCount` | `int` | Maps played since this map's cooldown fully expired. Reset to 0 on play |
| `CurrentNominationCooldown` | `int` | Current nomination cooldown count |
| `NominationTimedCooldownEndUtc` | `DateTime` | UTC end of the timed nomination cooldown |
| `IsCooldownActive` | `bool` | True while either cooldown axis (count or timed) is active |
| `IsNominationCooldownActive` | `bool` | True while either nomination cooldown axis is active |

---

## IMapCooldownQueryService

Service for **querying** map cooldown state. To modify cooldowns, use `IMapCooldownCommandService`. Values come from the store's effective layer.

| Method | Return Type | Description |
|---|---|---|
| `QueryCurrentCooldowns(IMapConfig)` | `Task<IDetailedCooldownResult?>` | Query cooldown information from the database |
| `GetCurrentCooldowns(IMapConfig)` | `IDetailedCooldownResult` | Get cooldown information from the in-memory cache |

### IDetailedCooldownResult

Holds detailed cooldown state including both the map's own cooldown and cooldowns from its groups.

| Property | Type | Description |
|---|---|---|
| `HasCooldown` | `bool` | Whether any cooldown is currently active |
| `HighestCooldownCount` | `int` | Highest count-based cooldown across the map and all groups |
| `LongestTimedCooldown` | `DateTime` | Latest timed cooldown expiration (UTC) across the map and all groups |
| `MapConfig` | `IMapConfig` | The target map's configuration data |
| `CooldownCount` | `int` | The map's own current count-based cooldown |
| `TimedCooldown` | `DateTime` | The map's own timed cooldown expiration (UTC) |
| `GroupCooldowns` | `IReadOnlyDictionary<string, int>` | Count-based cooldowns keyed by group name |
| `GroupTimedCooldowns` | `IReadOnlyDictionary<string, DateTime>` | Timed cooldown expirations keyed by group name |

---

## IMapCooldownCommandService

Service for **modifying** map cooldowns. All methods attempt to persist to the database and return a `bool` indicating success.

| Method | Return Type | Description |
|---|---|---|
| `SetCooldown(IMapConfig, int)` | `Task<bool>` | Set the count-based cooldown |
| `SetTimedCooldown(IMapConfig, TimeSpan)` | `Task<bool>` | Set the timed cooldown |
| `ExcludeFromNomination(IMapConfig)` | `Task<bool>` | Set cooldown to `int.MaxValue`, effectively excluding the map from nomination and random selection until cleared |
| `ClearCooldown(IMapConfig)` | `Task<bool>` | Clear the count-based cooldown |
| `ClearTimedCooldown(IMapConfig)` | `Task<bool>` | Clear the timed cooldown |

---

## Event Listener (IMapCycleEventListener)

Listener interface registered via `IMapCycleController.InstallEventListener`. All methods have default implementations, so you only need to override the events you are interested in.

| Method | Return Type | Description |
|---|---|---|
| `OnExtCommandExecute(IExtCommandExecuteEventParams)` | `McsCancellableEvent` | Fires when `!ext` is executed. Return `Stop` to cancel |
| `OnMapInfoCommandExecuted(IMapInfoCommandExecutedParams)` | `void` | Fires after `!mapinfo` completes. Use to output additional information |
| `OnExtendVoteStarted(IExtendVoteStartedEventParams)` | `void` | Fires when an extend vote starts |
| `OnExtendVoteCancelled(IExtendVoteCancelledEventParams)` | `void` | Fires when an extend vote is cancelled |
| `OnExtendVoteFinished(IExtendVoteFinishedEventParams)` | `void` | Fires when an extend vote concludes (passed or failed) |
| `OnNextMapConfirmed(INextMapConfirmedEventParams)` | `void` | Fires when the next map is confirmed |
| `OnNextMapRemoved(INextMapRemovedEventParams)` | `void` | Fires when the next map confirmation is removed |
| `OnMcsIntermission(IMcsIntermissionParams)` | `void` | Fires when entering the intermission state |
| `OnMapCooldownApply(IMapCooldownApplyEventParams)` | `void` | Fires just before cooldown is applied. Parameters are editable -- listeners can modify cooldown values or cancel application |
| `OnTimeLimitReached(ITimeLimitReachedEventParams)` | `void` | Fires when the time or round limit is reached |
| `OnVoteStartThresholdReached(IVoteStartThresholdReachedEventParams)` | `void` | Fires when remaining time/rounds cross the vote-start threshold |
