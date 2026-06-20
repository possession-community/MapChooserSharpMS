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

### Map Config Execution (cfg files)

On every map start, MCS automatically executes `.cfg` files matching the current map and its groups. The cfg files are loaded from fixed directories under the Sharp root:

```
sharp/configs/mcsms/cfgs/
├── maps/       # Map-specific cfg files
│   ├── de_dust2.cfg
│   └── ze_example_v1.cfg
└── groups/     # Group-specific cfg files
    ├── ze_maps.cfg
    └── surf_maps.cfg
```

**Execution order:** Group cfgs first (in config order), then map cfgs. Map settings override group settings.

**Matching modes** (`MapConfigExecutionType`):
- `ExactMatch` — Only executes `<mapname>.cfg` (case-insensitive)
- `StartWithMatch` — Executes all cfgs whose filename is a prefix of the map name (e.g. `de_.cfg` and `de_dust2.cfg` for `de_dust2`), shortest prefix first
- `PartialMatch` — Executes all cfgs whose filename appears anywhere in the map name

**exec expansion:** If a cfg file contains `exec <path>` lines, the referenced file is resolved relative to the same directory (not `csgo/cfg/`) and expanded inline. Recursive includes are supported up to 8 levels deep.

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
