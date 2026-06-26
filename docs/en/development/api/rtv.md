# Rock The Vote API

RTV (Rock The Vote) allows players to vote for initiating a map change vote.
External plugins access it via `IMapChooserSharpShared.McsRtvController`.

---

## RTV Operation Flow

1. Players join RTV via the `!rtv` command
2. When the participant count reaches the normal threshold, the `OnRtvConfirmed` event fires and a map vote is initiated
3. When the participant count reaches the immediate change threshold, the map changes instantly without a vote

### 2-Tier Threshold System

RTV has two thresholds:

- **Normal threshold** (`mcs_rtv_threshold`): When this ratio is reached, a map vote is started
- **Immediate change threshold** (`mcs_rtv_immediate_change_threshold`): When this ratio is reached, the map transitions immediately without a vote. `0` disables this feature

### Status Lifecycle

```
Enabled --(map start cooldown)--> InCooldown --(time elapsed)--> Enabled
   |
   +--(participant threshold reached)--> TriggeredWaitingForVote --(vote done)--> Enabled (next map)
   |
   +--(admin disables)--> Disabled --(admin enables)--> Enabled
```

During an active vote, the status becomes `AnotherVoteOngoing`. When the vote completes or is cancelled, the status returns to its previous state.
When waiting for map transition, the status becomes `TriggeredWaitingForMapTransition`.

---

## IMcsRtvController

Public facade for the RTV module. Provides access to the manager and service, and event listener registration.

| Member | Type | Description |
|---|---|---|
| `RtvManager` | `IRtvManager` | Manager for querying RTV state |
| `RtvService` | `IRtvService` | Service for RTV operations (join, leave, trigger vote) |
| `InstallEventListener(IRockTheVoteEventListener)` | `void` | Register an RTV event listener |
| `RemoveEventListener(IRockTheVoteEventListener)` | `void` | Unregister a listener |

---

## IRtvManager

Read-only manager for querying current RTV state. To modify state, use `IRtvService`.

| Member | Type | Description |
|---|---|---|
| `RtvStatus` | `RtvStatus` | Current RTV status |
| `RtvCommandUnlockTime` | `TimeSpan` | Engine time when the RTV command unlocks. Remaining seconds can be calculated as `RtvCommandUnlockTime - ISharedSystem.GetModSharp().EngineTime()` |
| `RtvCounts` | `int` | Current number of RTV participants |
| `RequiredCounts` | `int` | Number of participants required to trigger |
| `RtvCompletionRatio` | `float` | Progress ratio toward the threshold (`0.0` -- `1.0`) |
| `RtvParticipants` | `IReadOnlySet<int>` | Set of user slots participating in RTV |

---

## IRtvService

Service for RTV operations: player participation, vote triggering, and enable/disable toggling.

| Method | Return Type | Description |
|---|---|---|
| `AddClientToRtv(IGameClient)` | `RtvExecutionResult` | Add a player to RTV |
| `AddClientToRtv(int)` | `RtvExecutionResult` | Add a player to RTV by slot number |
| `RemoveClientFromRtv(IGameClient, IGameClient?)` | `bool` | Remove a player from RTV. `enforcer` is the admin who forced the removal |
| `RemoveClientFromRtv(int)` | `bool` | Remove a player from RTV by slot number |
| `InitiateRtvVote()` | `void` | Trigger an RTV vote (called internally when the normal threshold is reached) |
| `InitiateForceRtvVote(IGameClient?)` | `void` | Trigger a forced RTV by admin. Goes through the cancellable `OnForceRtv` event |
| `EnableRtvCommand(IGameClient?, bool)` | `void` | Enable the RTV command. Set `silently = true` to suppress the broadcast message |
| `DisableRtvCommand(IGameClient?, bool)` | `void` | Disable the RTV command. Set `silently = true` to suppress the broadcast message |

---

## RtvStatus

Enum representing the current RTV state.

| Value | Description |
|---|---|
| `Enabled` | Active and accepting `!rtv` commands |
| `Disabled` | Disabled by an admin |
| `InCooldown` | On cooldown (e.g. shortly after map start) |
| `AnotherVoteOngoing` | Another vote (such as a map vote) is in progress |
| `TriggeredWaitingForVote` | RTV has been triggered and is waiting for the map vote to start |
| `TriggeredWaitingForMapTransition` | RTV has been triggered and is waiting for the map to transition |

---

## RtvExecutionResult

Enum representing the result of `AddClientToRtv`.

| Value | Description |
|---|---|
| `Success` | Successfully joined RTV |
| `AlreadyVoted` | The player has already joined RTV |
| `CommandInCooldown` | The RTV command is on cooldown |
| `CommandDisabled` | The RTV command has been disabled by an admin |
| `AnotherVoteOngoing` | Another vote (such as a map vote) is in progress |
| `NotAllowed` | The player's RTV is not allowed for some reason |
| `DisallowedByExternalConsumer` | An external plugin's API consumer rejected the RTV |
| `TriggeredWaitingForVote` | RTV has already been triggered and is waiting for the vote to start |
| `TriggeredWaitingForMapTransition` | RTV has already been triggered and is waiting for map transition |

---

## Event Listener (IRockTheVoteEventListener)

Listener interface registered via `IMcsRtvController.InstallEventListener`. All methods have default implementations, so you only need to override the events you are interested in.

| Method | Return Type | Description |
|---|---|---|
| `OnClientRtvCast(IClientRtvCastParams)` | `McsCancellableEvent` | Fires when a player attempts to join RTV. Return `Stop` to cancel |
| `OnClientRtvUnCast(IClientRtvUnCastParams)` | `McsCancellableEvent` | Fires when a player attempts to leave RTV. Return `Stop` to cancel |
| `OnForceRtv(IForceRtvParam)` | `McsCancellableEvent` | Fires when a force RTV is about to be triggered. Return `Stop` to cancel |
| `OnRtvConfirmed(IRtvConfirmedParams)` | `void` | Fires when RTV is confirmed (threshold reached or forced). Non-cancellable. The MapVoteController listens to this event to initiate a map vote |
