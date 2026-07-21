# Plugin Configuration (config.toml)

## General

```toml
[General]
ShouldUseAliasMapNameIfAvailable = true
VerboseCooldownPrint = true
WorkshopCollectionIds = []
ShouldAutoFixMapName = true
SteamWebApiKey = ""
RtvMapChangeBehaviour = "ImmediatelyWithTime"
```

| Key | Type | Default | Description |
|---|---|---|---|
| ShouldUseAliasMapNameIfAvailable | bool | true | Whether to use MapNameAlias as the display name when available |
| VerboseCooldownPrint | bool | true | Whether to display remaining seconds during cooldown |
| WorkshopCollectionIds | string[] | [] | Array of Workshop collection IDs for automatic sync |
| ShouldAutoFixMapName | bool | true | Whether to auto-correct config file names to actual map names for Workshop maps at map start |
| SteamWebApiKey | string | "" | Steam Web API key. Falls back to the `STEAM_WEB_API_KEY` environment variable if empty. [Get one here](https://steamcommunity.com/dev/apikey) |
| RtvMapChangeBehaviour | enum | ImmediatelyWithTime | How to change the map after RTV passes. `ImmediatelyWithTime` (immediate with countdown) / `Cs2EndMatchScreen` (via CS2 end match screen) |

## MapCycle

```toml
[MapCycle]
FallbackMaxExtends = 3
FallbackMaxExtCommandUses = 1
FallbackExtendTimePerExtends = 15
FallbackExtendRoundsPerExtends = 5
ShouldStopSourceTvRecording = false
MapConfigExecutionType = "ExactMatch"
MapConfigDirectoryPath = "maps/"
PauseMapCycleWhenServerEmpty = false
```

| Key | Type | Default | Description |
|---|---|---|---|
| FallbackMaxExtends | int | 3 | Default maximum extend count for maps without config |
| FallbackMaxExtCommandUses | int | 1 | Maximum !ext uses for maps without config |
| FallbackExtendTimePerExtends | int | 15 | Extension time per extend (minutes) |
| FallbackExtendRoundsPerExtends | int | 5 | Extension rounds per extend |
| ShouldStopSourceTvRecording | bool | false | Whether to execute tv_stoprecord before map change (prevents SourceTV crashes) |
| MapConfigExecutionType | enum | ExactMatch | Map cfg execution matching method. `ExactMatch` / `StartWithMatch` / `PartialMatch` |
| MapConfigDirectoryPath | string | maps/ | Map TOML config directory path (relative to the module directory) |
| PauseMapCycleWhenServerEmpty | bool | false | When enabled, map transitions and cooldown consumption are skipped while no real players are on the server |

### Map Config Execution (cfg files)

On every map start, MCS automatically executes `.cfg` files matching the current map and its groups. The cfg files are loaded from fixed directories under the game's `cfg` directory:

```
csgo/cfg/mcsms/
├── maps/       # Map-specific cfg files
│   ├── de_dust2.cfg
│   └── ze/     # Subdirectories are allowed (names are ignored)
│       └── ze_example_v1.cfg
└── groups/     # Group-specific cfg files
    ├── ze_maps.cfg
    └── surf_maps.cfg
```

Both directories are scanned **recursively** — subdirectories are free for organization (e.g. `maps/ze/`, `maps/surf/`) and directory names are ignored; only the cfg **file name** is used for matching.

**Execution order:** Group cfgs first (in config order), then map cfgs. Map settings override group settings. Map cfgs run from generic to specific (shortest filename first), and an exact-match cfg always runs **last** so its values override every prefix/partial cfg. If the same file name exists in multiple subdirectories, all of them are executed.

**Matching modes** (`MapConfigExecutionType`, map cfgs only — group cfgs always match the group name exactly):
- `ExactMatch` — Only executes `<mapname>.cfg` (case-insensitive)
- `StartWithMatch` — Executes all cfgs whose filename is a prefix of the map name (e.g. `de_.cfg` and `de_dust2.cfg` for `de_dust2`), shortest prefix first
- `PartialMatch` — Executes all cfgs whose filename appears anywhere in the map name

**Nested exec:** The cfg files are executed via the engine's `exec` command, so `exec <path>` lines inside a cfg work as usual — the referenced path is resolved by the engine relative to `csgo/cfg/` (e.g. `exec mcsms/maps/shared_settings.cfg`).

## Cooldown

```toml
[Cooldown]
ScopeMatchMode = "Exact"
ScopePattern = ""
```

| Key | Type | Default | Description |
|---|---|---|---|
| ScopeMatchMode | enum | Exact | How cooldown records from other servers are matched. `Exact` / `StartsWith` |
| ScopePattern | string | "" | Server key pattern to match. Empty = this server's own Wuling `server_id` |

Cooldown state is stored **per server** in SurrealDB, keyed by `(server_key, map name)`. The server key is always the Wuling `server_id` (from `wuling.core.surreal.toml`) — MCS has no separate override.

On every map start, this server loads all cooldown records whose server key matches the scope:

- `Exact` + empty pattern (default): this server's cooldowns only. Each server tracks cooldowns independently even when sharing a database.
- `StartsWith` + a common prefix (e.g. `"TokyoAWP"`): cooldowns from `TokyoAWP1`, `TokyoAWP2`, `TokyoAWP_test` all apply here.

When multiple servers match, the **most restrictive value wins** per map/group: highest cooldown count, latest timed-cooldown end, most recent LastPlayedAt, lowest UnplayedCount. The same rule applies to nomination cooldowns.

Writes always target this server's own record — foreign cooldowns affect what this server picks and nominates, but are never written back. Note that if another in-scope server excludes a map from nomination (`!setmapcooldown <map> max`), the exclusion applies to every server in that scope until cleared on the server that set it.

> [!IMPORTANT]
> Cooldown persistence requires Wuling. As of this version MCS treats Wuling as a hard dependency — the plugin does not start without it. If the SurrealDB connection fails at startup, cooldowns fall back to in-memory only for that session.

## MapVote

```toml
[MapVote]
MaxVoteElements = 5
ShouldPrintVoteToChat = true
ShouldPrintVoteRemainingTime = true
CountdownUiType = "Center"

[MapVote.Sound]
SoundFile = ""
InitialVoteCountdownStartSound = ""
InitialVoteStartSound = ""
InitialVoteFinishSound = ""
InitialVoteCountdownSound1 = ""
# ... (1-10)
RunoffVoteCountdownStartSound = ""
RunoffVoteStartSound = ""
RunoffVoteFinishSound = ""
RunoffVoteCountdownSound1 = ""
# ... (1-10)
```

| Key | Type | Default | Description |
|---|---|---|---|
| MaxVoteElements | int | 5 | Number of maps shown in the vote menu (including Extend/DontChange) |
| ShouldPrintVoteToChat | bool | true | Whether to display in chat when a player votes |
| ShouldPrintVoteRemainingTime | bool | true | Whether to display remaining time in chat during voting |
| CountdownUiType | enum ([Flags]) | Center | Default display method for countdowns. `None` / `Hint` / `Center` / `Chat`. Multiple values can be combined with commas (e.g. `"Center,Chat"`). Players can override individually via `!mcs_settings countdown` (persisted with ICookie) |
| SoundFile | string | "" | .vsndevts file path. Automatically precached on every map start. Can be left empty if already handled by another plugin |
