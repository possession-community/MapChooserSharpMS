# Map Configuration API

MCS map configuration is loaded from TOML files and composed in Default -> Group -> Map priority order.
This page documents the public interfaces for reading the composed configuration.

For TOML file authoring, see [MAP_CONFIG.md](../../configuration/MAP_CONFIG.md).

---

## IMcsMapConfigProvider

Provider for map configuration lookup and reload.
Access via `IMapChooserSharpShared.McsMapConfigProvider`.

| Member | Type | Description |
|---|---|---|
| `ToolingService` | `IMapConfigToolingService` | Utilities for display name resolution, cooldown aggregation, etc. |
| `ReloadConfigs()` | `void` | Reload all map configurations from disk |
| `GetMapConfigs()` | `IReadOnlyDictionary<string, IReadOnlyCollection<IMapConfigOverrides>>` | All map configurations including DaySettings overrides |
| `GetGroupSettings()` | `IReadOnlyDictionary<string, IReadOnlyCollection<IMapGroupConfigOverrides>>` | All group configurations including DaySettings overrides |
| `TryGetMapConfig(string, out IMapConfig)` | `bool` | Look up a map by name. Returns the final configuration with DaySettings evaluated |
| `TryGetMapConfig(long, out IMapConfig)` | `bool` | Look up a map by Workshop ID |

### IMapConfigToolingService

| Method | Return Type | Description |
|---|---|---|
| `ResolveMapDisplayName(IMapConfig)` | `string` | Returns `MapNameAlias` if set, otherwise `MapName` |
| `GetHighestCooldown(IMapConfig)` | `int` | Returns the most restrictive cooldown across the map and all its groups |

---

## IMapConfig

Represents an individual map's configuration. Inherits from `IBaseMapConfig`.

| Property | Type | Description |
|---|---|---|
| `MapName` | `string` | Internal map name (TOML section name) |
| `MapNameAlias` | `string` | Display alias. Empty string means `MapName` is used as-is |
| `MapDescription` | `string` | Description displayed by the `!mapinfo` command |
| `WorkshopId` | `long` | Steam Workshop ID. `0` when not specified |
| `GroupSettings` | `List<IMapGroupConfig>` | Groups this map belongs to. Earlier groups in the list have higher priority |

Properties inherited from `IBaseMapConfig`:

| Property | Type | Description |
|---|---|---|
| `IsDisabled` | `bool` | When `true`, the map is excluded from nomination and random selection |
| `MaxExtends` | `int` | Maximum number of extends consumable by the map vote Extend option. `0` hides the Extend option |
| `MaxExtCommandUses` | `int` | Maximum number of extends consumable by the `!ext` command |
| `MapTime` | `int` | This map's time limit in minutes. Applied to `mp_timelimit` at map start; MCS manages the limit internally thereafter |
| `ExtendTimePerExtends` | `int` | Minutes added per extend |
| `MapRounds` | `int` | This map's round limit. Applied to `mp_maxrounds` at map start; MCS manages the limit internally thereafter |
| `ExtendRoundsPerExtends` | `int` | Rounds added per extend |
| `RandomPickConfig` | `IRandomPickConfig` | Random selection weighting and exclusion settings |
| `NominationConfig` | `INominationConfig` | Nomination restriction settings |
| `CooldownConfig` | `ICooldownConfig` | Cooldown configuration and current state |
| `ExtraConfiguration` | `IExtraConfigAccessor` | Custom configuration sections for external plugins |

---

## IMapGroupConfig

Represents group-level configuration. Inherits all `IBaseMapConfig` properties plus group-specific properties.

| Property | Type | Description |
|---|---|---|
| `GroupName` | `string` | Group identifier (corresponds to `Groups.<Name>` in TOML) |
| `ShortGroupName` | `string` | Short display tag for the vote screen. Maximum 4 characters. Values longer than 4 characters in TOML are truncated to 4 |
| `MapCooldownOverride` | `int` | When set to a positive value, overrides the cooldown of maps belonging to this group. Takes priority over the map's own `Cooldown` |
| `NominationLimit` | `int` | Maximum number of maps that can be nominated from this group. `0` = unlimited |

---

## INominationConfig

Nomination restriction rules that can be configured on maps and groups.

| Property | Type | Description |
|---|---|---|
| `MaxPlayers` | `int` | Nomination is blocked when the server player count reaches or exceeds this value. `0` = no limit |
| `MinPlayers` | `int` | Nomination is blocked when the server player count is below this value. `0` = no limit |
| `ProhibitAdminNomination` | `bool` | When `true`, even admin nomination via `!nominate_addmap` is rejected (console nomination remains possible) |
| `DaysAllowed` | `IReadOnlyList<DayOfWeek>` | Days of the week when nomination is allowed. Empty list = all days |
| `AllowedTimeRanges` | `IReadOnlyList<ITimeRange>` | Time-of-day windows when nomination is allowed. Empty list = all times |

### ITimeRange

Represents a time-of-day range. Supports overnight ranges (e.g. `22:00-03:00`).

| Member | Type | Description |
|---|---|---|
| `StartTime` | `TimeOnly` | Start time |
| `EndTime` | `TimeOnly` | End time |
| `IsInRange(TimeOnly time)` | `bool` | Returns whether the given time falls within this range |

---

## ICooldownConfig

Cooldown configuration and current state for maps and groups.

MCS cooldowns have two independent axes:

- **Count-based**: A counter set to the configured value when the map is played, decremented by 1 each time another map is played
- **Timed**: The map remains on cooldown until the configured duration has elapsed since it was last played

| Property | Type | Description |
|---|---|---|
| `ConfigCooldown` | `int` | Count-based cooldown value specified in TOML |
| `TimedCooldown` | `TimeSpan` | Timed cooldown duration specified in TOML |
| `CurrentCooldown` | `int` | Current count-based cooldown in memory. Set to `ConfigCooldown` when the map is played; decremented as other maps are played |
| `LastPlayedAt` | `DateTime` | UTC timestamp of last play. Used for timed cooldown expiration checks |
| `ConfigNominationCooldown` | `int` | Nomination-specific count-based cooldown specified in TOML. Applied when a map is consumed as a vote candidate. `0` = disabled (opt-in) |
| `NominationTimedCooldown` | `TimeSpan` | Nomination-specific timed cooldown duration |

---

## IRandomPickConfig

Settings for random map selection during vote candidate building.

| Property | Type | Description |
|---|---|---|
| `MapSelectionWeight` | `uint` | Selection weight. Higher values increase the probability of being selected as a vote candidate. `0` excludes the map from random selection entirely |
| `IsPickable` | `bool` | Whether the map is eligible for random selection. Setting `OnlyNomination = true` in TOML results in `false` |
| `BypassNominationRestriction` | `bool` | When `true`, nomination restrictions (player count, day-of-week, time-of-day, etc.) are ignored during random selection |

---

## IExtraConfigAccessor

Type-safe accessor for custom configuration defined in TOML under `[MapName.extra.SectionName]`.
Designed as the mechanism for external plugins to piggyback per-map custom data onto MCS configuration files.

Extra configuration is **merged** in Default -> Group -> Map order. When the same section and key exist at multiple levels, the last write wins.

| Method | Return Type | Description |
|---|---|---|
| `GetValue<T>(string section, string key, T defaultValue)` | `T` | Returns the value for the given section and key, converted to type `T`. Returns `defaultValue` if the key does not exist or type conversion fails |
| `TryGetValue<T>(string section, string key, out T value)` | `bool` | Returns `true` and sets `value` if the key exists |
| `HasSection(string section)` | `bool` | Whether the specified section exists |
| `HasKey(string section, string key)` | `bool` | Whether the specified key exists within the section |
| `GetSections()` | `IReadOnlyCollection<string>` | All section names |
| `GetKeys(string section)` | `IReadOnlyCollection<string>` | All key names within a section |
| `GetArray<T>(string section, string key)` | `IReadOnlyList<T>` | Returns a TOML array converted to the specified type |

---

## DaySettings Overrides

The collections returned by `GetMapConfigs()` and `GetGroupSettings()` include both the base configuration and DaySettings override entries.

### IBaseOverrideConfig

Common properties shared by all override entries.

| Property | Type | Description |
|---|---|---|
| `OverrideConfigName` | `string` | Override name. For the base (non-override) entry, this equals `IBaseOverrideConfig.BaseConfigName` (a constant: empty string) |
| `Enabled` | `bool` | Whether this override is active |
| `ForceOverride` | `bool` | When `true`, this override is applied with highest priority regardless of normal priority ordering |
| `OverridePriority` | `int` | Priority among multiple matching ForceOverride entries (higher value wins) |
| `TargetDays` | `IReadOnlyCollection<DayOfWeek>` | Days of the week when this override applies |
| `TargetTimeRanges` | `IReadOnlyCollection<ITimeRange>` | Time-of-day ranges when this override applies |

### IMapConfigOverrides : IBaseOverrideConfig

| Property | Type | Description |
|---|---|---|
| `MapConfig` | `IMapConfig` | The map configuration as it would be with this override applied |

### IMapGroupConfigOverrides : IBaseOverrideConfig

| Property | Type | Description |
|---|---|---|
| `GroupConfig` | `IMapGroupConfig` | The group configuration as it would be with this override applied |

In normal usage, `TryGetMapConfig()` automatically evaluates DaySettings and returns the final resolved configuration.
Use `GetMapConfigs()` when you need to enumerate all overrides or implement custom schedule evaluation logic.
