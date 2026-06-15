# Map Vote API

Provides public interfaces for controlling MCS map voting.
Supports vote initiation and cancellation, client vote operations, and reading in-progress vote state.

Access via `IMapChooserSharpShared.McsMapVoteController`.

---

## IMcsMapVoteController

Public facade for the map vote module. Write operations for voting are delegated to individual services and managers; this interface exposes only event listener registration and winner threshold customization.

| Member | Type | Description |
|---|---|---|
| `InstallEventListener(IMapVoteEventListener)` | `void` | Register a map vote event listener |
| `RemoveEventListener(IMapVoteEventListener)` | `void` | Unregister a listener |
| `CustomWinnerThresholdProvider` | `Func<float>?` | External plugin override for the winner threshold (see below) |

### CustomWinnerThresholdProvider

A property for overriding the vote pass threshold used in the initial vote round. The delegate is invoked each time a vote starts, and the returned value (`0.0` -- `1.0`) is used as the pass threshold.

- Set to `null` to revert to the default ConVar-based threshold
- **Ignored during runoff votes** -- runoff votes are always decided by plurality (most votes wins)

```csharp
// Example: always require 60% of votes to pass
controller.CustomWinnerThresholdProvider = () => 0.6f;

// Example: revert to default
controller.CustomWinnerThresholdProvider = null;
```

---

## IMapVoteControllingService

Service for starting, cancelling, and force-resetting votes.

| Method | Return Type | Description |
|---|---|---|
| `InitiateVote(bool isActivatedByRtv = false)` | `McsMapVoteState` | Start a map vote. Returns `InitializeAccepted` on success, otherwise returns the current vote state |
| `CancelVote(IGameClient? client)` | `McsMapVoteState` | Cancel the active vote. Returns `Cancelling` on success |
| `ForceResetVote()` | `bool` | Force reset all vote state |

### InitiateVote isActivatedByRtv Parameter

- `true` (RTV-triggered): The first option is "Don't Change" (keep the current map)
- `false` (normal trigger): The first option is "Extend" (extend the current map)

This difference determines whether the special first option offers "keep current map" (RTV context) or "extend play time" (time limit context).

---

## IClientVoteHandlingService

Service for handling individual player vote operations.

| Method | Return Type | Description |
|---|---|---|
| `TryAddClientVote(IGameClient, IMapVoteOption)` | `bool` | Add a player's vote. Returns `true` on success |
| `RemoveClientVote(IGameClient)` | `void` | Remove a player's vote from the current vote |
| `RemoveClientVote(PlayerSlot)` | `void` | Remove a player's vote by slot. For use during disconnects when `IGameClient` is unavailable |
| `ClientReVote(IGameClient)` | `void` | Remove the player's vote and re-display the vote menu. Silently ignored for native vote UI |

---

## IMcsReadOnlyVoteState

Read-only view of vote state. Modules that only need to query "is a vote in progress?" should depend on this lightweight interface rather than the full controller.

| Member | Type | Description |
|---|---|---|
| `CurrentVoteState` | `McsMapVoteState?` | Current vote state. `null` when no vote has been initiated |
| `IsVotingPeriod()` | `bool` | Whether a vote is in progress. Returns `true` for all states where starting another vote would be unsafe (voting, initializing, runoff, finalizing, etc.) |

---

## IVoteControllingManager

Manager for reading current vote session information.

| Property | Type | Description |
|---|---|---|
| `CurrentVote` | `IMapVoteInformation?` | Information about the currently active vote session. `null` when no vote is in progress |

`IVoteControllingManager` references an `IMapVoteInformation` instance that is newly created for each vote session. Vote sessions are disposable -- they are not singletons that get reset.

---

## IMapVoteInformation

Represents an individual vote session.

| Property | Type | Description |
|---|---|---|
| `CurrentState` | `McsMapVoteState` | Current state of this vote session |
| `VoteOptions` | `IReadOnlyCollection<IMapVoteOption>` | List of vote options |
| `Winner` | `IMapVoteOption?` | The winning option. `null` until the vote is finalized |

---

## IMapVoteOption

Represents a single vote option.

| Property | Type | Description |
|---|---|---|
| `MapName` | `string` | Display name of the option |
| `MapConfig` | `IMapConfig?` | Map configuration for this option |
| `VoteParticipants` | `IReadOnlyCollection<PlayerSlot>` | Slots of players who voted for this option |

### When MapConfig is null

`MapConfig` always returns a map configuration for normal map options, but is `null` for the following special options:

- **Extend**: The option to extend the current map's play time. Added in normal votes (`isActivatedByRtv = false`)
- **Don't Change**: The option to keep the current map. Added in RTV votes (`isActivatedByRtv = true`)

Use `MapConfig == null` to detect special options. `MapName` contains the localized display string for these options.

---

## McsMapVoteState

Enum representing the vote state machine.

| Value | Description |
|---|---|
| `NoActiveVote` | No active vote and the next map is not confirmed |
| `Cancelling` | Vote cancellation in progress |
| `InitializeAccepted` | Vote initialization accepted; starting preparation |
| `Initializing` | Vote initializing (candidate map selection, menu construction, etc.) |
| `Voting` | Vote in progress |
| `RunoffVoting` | Runoff vote in progress. Triggered when no option meets the winner threshold in the initial round; conducted among the top candidates |
| `Finalizing` | Vote result finalization in progress (cooldown application, next map setting, etc.) |
| `NextMapConfirmed` | Next map is confirmed. No new votes can be started in this state |
| `NotEnoughMapsToStartVote` | Not enough valid map configurations to start a vote |

### Vote Lifecycle

Votes progress through the following state transitions:

```
NoActiveVote
  -> InitializeAccepted     (InitiateVote accepted)
    -> Initializing          (candidate map building, menu preparation)
      -> Voting              (players cast votes)
        -> RunoffVoting      (if threshold not met; optional)
          -> Finalizing      (result processing)
            -> NextMapConfirmed  (next map decided)
```

When cancelled, the state transitions through `Cancelling` back to `NoActiveVote`. When candidate maps are insufficient, `NotEnoughMapsToStartVote` is returned.

---

## Event Listener (IMapVoteEventListener)

Listener interface registered via `IMcsMapVoteController.InstallEventListener`. All methods have default implementations, so you only need to override the events you are interested in.

| Method | Return Type | Description |
|---|---|---|
| `OnMapVoteStart(IMapVoteStartParams)` | `bool` | Fires before a vote starts. Return `true` to cancel |
| `OnRandomMapPick(IMapVoteRandomMapPickParams)` | `List<IMapConfig>` | Fires during random map selection. Return a non-empty list to override the vote candidates |
| `OnMapVoteFinished(IMapVoteFinishedEventParams)` | `void` | Fires when the vote completes (before individual result events) |
| `OnMapVoteCancelled(IMapVoteCancelledParams)` | `void` | Fires when the vote is cancelled |
| `OnMapExtended(IMapVoteExtendParams)` | `void` | Fires when the vote result is map extension |
| `OnMapNotChanged(IMapVoteNotChangedParams)` | `void` | Fires when the vote result is "Don't Change" |
| `OnMapConfirmed(IMapVoteMapConfirmedEventParams)` | `void` | Fires when the next map is confirmed |
