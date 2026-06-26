# ConVars (Server Variables)

Settings that can be changed at runtime. Configurable via `server.cfg` or RCON.

## MapVote

| ConVar | Default | Range | Description |
|---|---|---|---|
| mcs_vote_shuffle_menu | 0 | 0-1 | Whether to shuffle vote menu options per player |
| mcs_vote_end_time | 15.0 | 5-120 | Vote time limit (seconds) |
| mcs_vote_countdown_time | 13 | 0-120 | Pre-vote countdown (seconds). 0 for immediate start |
| mcs_vote_runoff_map_pickup_threshold | 0.3 | 0-1 | Minimum vote percentage for a map to advance to a runoff vote |
| mcs_vote_winner_pickup_threshold | 0.7 | 0-1 | Vote percentage at or above which a winner is immediately confirmed |
| mcs_vote_exclude_spectators | 0 | 0-1 | Whether to exclude spectators from voting |
| ~~mcs_vote_change_map_immediately_rtv_vote_success~~ | - | - | **Deprecated** -- migrated to `mcs_rtv_immediate_change_threshold` |

## MapCycle

| ConVar | Default | Range | Description |
|---|---|---|---|
| mcs_mapcycle_mode | time | none/time/round | Map cycle mode |
| mcs_mapcycle_vote_start_time_threshold | 180 | 0-3600 | Remaining time to start vote in Time mode (seconds) |
| mcs_mapcycle_vote_start_round_threshold | 3 | 0-120 | Remaining rounds to start vote in Round mode |
| mcs_ext_user_vote_threshold | 0.5 | 0-1 | Required vote ratio for !ext |
| mcs_vote_extend_success_threshold | 0.5 | 0-1 | Pass threshold for !ve (extend vote) |
| mcs_vote_extend_vote_time | 15.0 | 10-60 | Vote duration for !ve (seconds) |
| mcs_map_transition_retry_attempts | 3 | 1-10 | Number of map change retry attempts |
| mcs_map_transition_retry_interval | 30.0 | 5-300 | Retry interval (seconds) |
| mcs_map_transition_fallback_map | de_dust2 | - | Fallback map when all retries fail |
| mcs_map_transition_delay | 20.0 | 0-60 | Delay after round end before map change (seconds). 0 for immediate change |
| mcs_end_match_immediately | 1 | 0-1 | 1 = terminate the round immediately when the match ends, 0 = wait for the round to end naturally |

## RTV

| ConVar | Default | Range | Description |
|---|---|---|---|
| mcs_rtv_command_unlock_time_next_map_confirmed | 0.0 | 0-1200 | Seconds until RTV command is unlocked after next map is confirmed |
| mcs_rtv_command_unlock_time_map_dont_change | 0.0 | 0-1200 | Seconds until RTV is unlocked after "Don't Change" wins |
| mcs_rtv_command_unlock_time_map_extend | 0.0 | 0-1200 | Seconds until RTV is unlocked after map extend |
| mcs_rtv_command_unlock_time_map_start | 0.0 | 0-1200 | Seconds until RTV is unlocked after map start |
| mcs_rtv_vote_start_threshold | 0.5 | 0-1 | Required vote ratio for RTV to pass |
| mcs_rtv_map_change_timing | 20.0 | 0-60 | Seconds until round end after RTV succeeds. 0 for immediate end |
| mcs_rtv_minimum_requirements | 0 | 0-64 | Minimum number of votes required to start RTV. 0 to disable |
| mcs_rtv_broadcast_player_cast | 1 | 0-1 | Whether to broadcast when a player casts an RTV vote |
| mcs_rtv_immediate_change_threshold | 0.0 | 0-1 | If RTV participation ratio after vote completion meets or exceeds this value, change map immediately. 0 = disabled (always wait for round end) |
| mcs_rtv_threshold_decay_time | 0.0 | 0-3600 | Seconds to decay the RTV threshold from 100% to the configured value. 0 = disabled |

### RTV Two-Stage Threshold (`mcs_rtv_immediate_change_threshold`)

Changes the behavior of `!rtv` based on participation ratio after the vote has completed and the NextMap is confirmed.

1. `mcs_rtv_vote_start_threshold` (normal threshold, e.g. 0.5) reached -- map changes at round end
2. `mcs_rtv_immediate_change_threshold` (immediate threshold, e.g. 0.8) reached -- promoted to immediate map change
3. Set to 0.0 to disable immediate change (always change at round end)

After the normal threshold is reached, `!rtv` continues to be accepted. When the immediate threshold is reached, the change is automatically promoted.

### RTV Threshold Time Decay (`mcs_rtv_threshold_decay_time`)

At map start, RTV requires 100% participation, which linearly decays to the `mcs_rtv_vote_start_threshold` value over time.

Example: `threshold=0.5`, `decay_time=600` (10 minutes)
- 0 min: 100% required (10 of 10 players)
- 5 min: 75% required (8 of 10 players)
- 10 min: 50% required (5 of 10 players = normal value)

Set to 0.0 to disable decay (the configured threshold applies from the start).

## Nomination

| ConVar | Default | Range | Description |
|---|---|---|---|
| mcs_nomination_broadcast_enabled | 1 | 0-1 | Whether to enable broadcast notifications on nomination |
| mcs_nomination_confirm_menu | 0 | 0-1 | Whether to show a confirmation menu on nomination |
| mcs_nomination_player_cooldown | 0 | 0-MaxInt | Per-player nomination cooldown in map count (0 = disabled) |
| mcs_nomination_player_timed_cooldown | 0.0 | 0-MaxFloat | Per-player nomination timed cooldown in seconds (0 = disabled) |

## ChatListener

| ConVar | Default | Range | Description |
|---|---|---|---|
| mcs_block_chat_during_vote | 0 | 0-1 | Whether to block chat during voting (AntiCanvas) |
