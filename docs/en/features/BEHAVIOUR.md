# Internal Behaviour

Documents implicit behaviours that are not directly configurable but affect how MCS operates.

## Boot Phase (`+host_workshop_map`)

When the server is launched with `+host_workshop_map <workshopId>`, MCS enters a **boot phase**. During this phase:

- Cooldown consumption is suppressed (map cooldowns are not decremented)
- Audit records are not created
- Map cycle transitions are not triggered

This prevents the intermediate maps loaded during workshop map download from consuming cooldowns or generating audit entries.

The boot phase ends when the map matching the specified workshop ID is loaded. From that point on (including that map), all systems operate normally.

If `+host_workshop_map` is not present in the launch arguments, the boot phase is skipped entirely.

## Empty Server Pause (`PauseMapCycleWhenServerEmpty`)

When enabled via `[MapCycle] PauseMapCycleWhenServerEmpty = true`:

- Map cycle transitions are paused while no real players (non-bot, non-HLTV) are on the server
- Cooldown consumption is skipped for maps that end with no players
- The internal time/round limit tracker is also paused, so transitions fire correctly when a player joins

This prevents unattended servers from cycling through maps and wasting cooldowns. When a player connects, normal operation resumes within 1 second.

## Time Limit Management

MCS takes full control of the map's time/round limit:

1. **Config application**: On map start, `MapTime` (minutes) or `MapRounds` from the map config is applied to `mp_timelimit` / `mp_maxrounds`
2. **Internal manager**: MCS reads the ConVar value and initializes an internal TimeLimitManager
3. **ConVar override**: Both `mp_timelimit` and `mp_maxrounds` are set to `99999999` to prevent the game from ending the match on its own
4. **MCS-driven lifecycle**: Vote thresholds and limit-reached events are driven by the internal manager, not the game's native checks

Map config execution (cfg files) runs **before** the MapTime/MapRounds application, so game-related ConVars like `sv_airaccelerate` are set first.

## End Match Flow

MCS installs a **`GoToIntermission` detour** (hook on `server.dll`'s `GoToIntermission` function, resolved at startup). When the time/round limit is reached and the next map is confirmed:

- If `mcs_end_match_immediately = 1` (default): `ForceMatchEnd()` sets `mp_timelimit=0.01` and `mp_maxrounds=1`, then calls `TerminateRound` for the end-of-round visual (Round Won/Lost). The game engine naturally invokes `GoToIntermission` after the round ends, and the MCS detour intercepts it to fire the intermission event.
- If `mcs_end_match_immediately = 0`: the deferred transition flag is set and `mp_timelimit=0.01, mp_maxrounds=1` are applied. On the next natural round end, the deferred handler fires and triggers the transition.

An idempotency guard ensures that intermission is only fired once per map, even if multiple code paths attempt to trigger it.

## Map Config Execution (cfg files)

On every map start, MCS executes `.cfg` files from `csgo/cfg/mcsms/maps/` and `csgo/cfg/mcsms/groups/` via the `exec` command. See [Plugin Config](../configuration/PLUGIN_CONFIG.md#map-config-execution-cfg-files) for details on matching modes and directory layout.

## Sound Precache

The `.vsndevts` file specified in `[MapVote.Sound] SoundFile` is automatically precached on every map start during the `OnResourcePrecache` callback. No additional setup is needed beyond setting the path.

## SourceTV Recording

When `[MapCycle] ShouldStopSourceTvRecording = true`, MCS executes `tv_stoprecord` before each map change to prevent SourceTV crashes.

## Map Config Resolution

When resolving the current map's config, MCS checks in this order:

1. **Addon ID** (workshop ID from `GetAddonName()`) — checked first for workshop maps where the BSP name may differ from the config name
2. **Map name** (`GetMapName()`) — standard name-based lookup

This ensures workshop maps are correctly matched even when the server's internal map name doesn't match the config file name.
