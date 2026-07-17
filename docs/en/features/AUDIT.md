# Audit System

MapChooserSharpMS records server events to SurrealDB (via Wuling) for analysis and external tooling. All audit tables are append-only and include a `server_id` field for multi-server environments.

## Tables

### mcs_audit_map_play

Recorded when a map ends.

| Field | Type | Description |
|---|---|---|
| map_name | string | Map name |
| workshop_id | int? | Workshop ID (null if not a workshop map) |
| group_names | string[] | Groups the map belongs to |
| peak_player_count | int | Peak player count during the map |
| end_player_count | int | Player count at map end |
| map_started_at | datetime | Map start time |
| map_ended_at | datetime | Map end time |
| map_end_reason | string | How the map ended (e.g. timelimit, rtv) |
| round_count | int | Number of rounds played |
| timelimit_type | string | Limit type (timelimit / maxrounds) |
| configured_timelimit | float | Configured time/round limit value |
| extend_count | int | Total extends that occurred |
| max_normal_extends | int | MaxExtends config value |
| normal_extends_used | int | MapVote extends used |
| admin_vote_extend_count | int | Admin vote extends (!ve) |
| user_ext_extends_used | int | !ext extends used |
| max_user_ext_extends | int | MaxExtCommandUses config value |
| server_id | string | Server identifier |
| created_at | datetime | Record creation time |

### mcs_audit_nomination

Recorded for each nomination at the end of a map.

| Field | Type | Description |
|---|---|---|
| nominated_at | datetime | When the nomination was made |
| map_name | string | Nominated map name |
| workshop_id | int? | Workshop ID |
| nominator_steam_id | int? | Nominator's SteamID (null if console) |
| nomination_type | string | Nomination type (see values below) |
| nomination_result | string | Result (see values below) |
| group_name | string | Group name of the map |
| server_id | string | Server identifier |
| created_at | datetime | Record creation time |

#### nomination_type values

| Value | Description |
|---|---|
| `user` | Nominated by a player via `!nominate` |
| `admin` | Nominated by an admin via `!nominate_addmap` / `!nominate_addwsmap` |
| `console` | Nominated by server console |

#### nomination_result values

| Value | Description |
|---|---|
| `voted_won` | Nomination was included in the vote and won |
| `voted_lost` | Nomination was included in the vote but lost |
| `not_picked` | Nomination was not selected as a vote candidate (filtered by slot limit or threshold) |
| `cancelled_by_admin` | Nomination was removed by an admin (`!nominate_removemap`) |
| `cancelled_by_self` | Player cancelled their own nomination (`!unnominate`) |

### mcs_audit_vote

Recorded when a map vote concludes.

| Field | Type | Description |
|---|---|---|
| vote_started_at | datetime | Vote start time |
| vote_ended_at | datetime | Vote end time |
| vote_result | string | Result (confirmed / extended / not_changed / cancelled) |
| map_vote_start_reason | string | What triggered the vote (timelimit / rtv) |
| vote_duration_config | float | Configured vote duration (seconds) |
| total_players | int | Player count at vote time |
| total_votes | int | Number of votes cast |
| server_id | string | Server identifier |
| created_at | datetime | Record creation time |

### mcs_audit_vote_candidate

Recorded for each candidate in a map vote. Linked to `mcs_audit_vote` via `vote_id`.

| Field | Type | Description |
|---|---|---|
| vote_id | string | Parent vote record ID |
| map_name | string | Candidate map name |
| workshop_id | int? | Workshop ID |
| vote_count | int | Votes received |
| is_winner | bool | Whether this candidate won |
| is_nominated | bool | Whether this candidate was nominated |
| candidate_type | string | How the candidate was selected (see values below) |
| created_at | datetime | Record creation time |

#### candidate_type values

| Value | Description |
|---|---|
| `extend` | Extend current map option |
| `dont_change` | Don't change map option (RTV votes) |
| `map` | Regular map candidate (nominated or randomly picked) |

### mcs_audit_extend_vote

Recorded when an admin extend vote (!ve) concludes.

| Field | Type | Description |
|---|---|---|
| vote_started_at | datetime | Vote start time |
| vote_ended_at | datetime | Vote end time |
| vote_result | string | Result |
| success_threshold | float | Required pass ratio |
| yes_count | int | Yes votes |
| no_count | int | No votes |
| total_players | int | Player count |
| passed | bool | Whether the vote passed |
| initiator_steam_id | int? | Admin who started the vote |
| server_id | string | Server identifier |
| created_at | datetime | Record creation time |

### mcs_audit_rtv

Recorded when RTV is triggered. Linked to `mcs_audit_rtv_vote` via record ID.

| Field | Type | Description |
|---|---|---|
| triggered_at | datetime | When RTV was triggered |
| threshold | int | Required vote count |
| immediate_threshold | int? | Immediate change threshold (null if disabled) |
| is_forced | bool | Whether this was a forced RTV (!forcertv) |
| map_state | string | Map state at trigger time |
| server_id | string | Server identifier |
| created_at | datetime | Record creation time |

### mcs_audit_rtv_vote

Individual RTV votes. Linked to `mcs_audit_rtv` via `rtv_id`.

| Field | Type | Description |
|---|---|---|
| rtv_id | string | Parent RTV record ID |
| steam_id | int | Voter's SteamID |
| voted_at | datetime | When the vote was cast |
| server_id | string | Server identifier |
| created_at | datetime | Record creation time |

### mcs_audit_ext

Recorded when !ext threshold is reached. Linked to `mcs_audit_ext_vote` via record ID.

| Field | Type | Description |
|---|---|---|
| triggered_at | datetime | When !ext was triggered |
| threshold | int | Required vote count |
| map_state | string | Map state at trigger time |
| server_id | string | Server identifier |
| created_at | datetime | Record creation time |

### mcs_audit_ext_vote

Individual !ext votes. Linked to `mcs_audit_ext` via `ext_id`.

| Field | Type | Description |
|---|---|---|
| ext_id | string | Parent !ext record ID |
| steam_id | int | Voter's SteamID |
| voted_at | datetime | When the vote was cast |
| server_id | string | Server identifier |
| created_at | datetime | Record creation time |

### mcs_audit_cooldown_expired

Recorded when a map or group cooldown fully expires (both count-based and timed cooldown cleared). Only fires once per cooldown cycle.

| Field | Type | Description |
|---|---|---|
| name | string | Map or group name |
| cooldown_type | string | `map` or `group` |
| became_available_at | datetime | When the cooldown fully expired |
| server_id | string | Server identifier |

## Non-Audit Persistence Tables

These are operational tables, not audit logs.

### mcs_map_cooldown / mcs_group_cooldown

Current cooldown state for maps and groups. Upserted on each map change.

| Field | Type | Description |
|---|---|---|
| name | string | Map or group name |
| cooldown | int | Current count cooldown remaining |
| timed_cooldown_end | datetime | Timed cooldown expiration |
| last_played_at | datetime | Last time this map/group was played |
| unplayed_count | int | Map changes passed while off cooldown without being played |
| nom_cooldown | int | Current nomination cooldown remaining |
| nom_timed_cooldown_end | datetime | Nomination timed cooldown expiration |
| last_nominated_at | datetime | Last nomination time |

### mcs_user_nom_cooldown

Per-player nomination cooldown state.

| Field | Type | Description |
|---|---|---|
| steam_id | int | Player's SteamID |
| remaining_count | int | Remaining cooldown count |
| cooldown_until | datetime | Cooldown expiration time |
| server_id | string | Server identifier |
| created_at | datetime | Record creation time |
