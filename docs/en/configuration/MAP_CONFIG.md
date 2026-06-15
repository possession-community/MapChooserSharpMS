# Map Configuration

## How to Place Map Configs

This plugin supports the following two methods.

### 1. Using maps.toml

If `config/maps.toml` exists, settings are loaded from `maps.toml`.

```
.
└── MapChooserSharp/
    ├── config/
    │   └── maps.toml
    └── MapChooserSharp.dll
```

### 2. Using split config files (xxx.toml)

Instead of consolidating everything into a single file, you can split configs as shown below.
If you use this method, do not use `maps.toml` as a file name.

Also, at least one TOML file must be placed directly under the `config/` folder.

```
.
└── MapChooserSharp/
    ├── config/
    │   ├── ze_maps/
    │   │   └── ze_xxxxx_v1.toml
    │   ├── de_maps/
    │   │   ├── de_dust2.toml
    │   │   └── de_mirage.toml
    │   ├── surf_maps/
    │   │   ├── surf_xxxxx.toml
    │   │   └── surf_yyyyy.toml
    │   └── default.toml
    └── MapChooserSharp.dll
```

## Configuration Priority and Groups

### Map Priority

The configuration priority is `Map > Group > Default` -- map-level settings have the highest priority.

If `MinPlayers = 0` is set at the default level, `MinPlayers = 16` at the group level, and the map inherits this group but sets `MinPlayers = 32`, the final value of `MinPlayers` will be `32`.

### Configuration Resolution Order

The following uses `ze_example_xyz` as an example map config.

### 1. Default Values

First, the map config loads the values from `[MapChooserSharpSettings.Default]`.

At this point, the config looks like:

---

Given the following default file:

```toml
[MapChooserSharpSettings.Default]
MinPlayers = 0
MaxPlayers = 0
OnlyNomination = false
```

The resulting config is:

```toml
[ze_example_xyz]
MinPlayers = 0
MaxPlayers = 0
OnlyNomination = false
```

### 2. Group Settings

Next, MapChooserSharp can apply group settings to maps.

Groups are defined with `MapChooserSharpSettings.Groups.<GroupName>`:
```toml
[MapChooserSharpSettings.Groups.Group1]
MinPlayers = 16
OnlyNomination = true
```

The part after `MapChooserSharpSettings.Groups` (`Group1`) becomes the group name.

```toml
[ze_example_xyz]
GroupSettings = ["Group1"]
```

### How to Configure Groups

A map can belong to multiple groups, but the first group in the list has the highest priority.

Multiple groups can be used as follows:

```toml
[ze_example_789]
GroupSettings = ["Group1", "Group2", "Group3"]
```

In this case, Group1 has the highest priority among the group settings, so it overrides any values applied by Group2 or Group3.

Groups have higher priority than Default settings, so `MinPlayers` changes from the default `0` to `16`.

Likewise, `OnlyNomination` changes from `false` to `true`.

At this point, the data loaded by the plugin looks like:

```toml
[ze_example_xyz]
MinPlayers = 16
MaxPlayers = 0
OnlyNomination = true
```

### 3. Map Settings

Next, the plugin reads the map-level config data.

Given the following definition:

```toml
[ze_example_xyz]
MinPlayers = 32
```

As mentioned above, map-level settings have the highest priority, so the final loaded data looks like:

```toml
[ze_example_xyz]
MinPlayers = 32
MaxPlayers = 0
OnlyNomination = true
```

### 4. Exceptions

Extra settings are merged rather than overwritten.

---

For example, given the following configs:

Group setting 1

```toml
[MapChooserSharpSettings.Groups.Group1]
MinPlayers = 0

[MapChooserSharpSettings.Groups.Group1.extra.shop]
cost = 100
```
Group setting 2

```toml
[MapChooserSharpSettings.Groups.Group2]
MinPlayers = 32

[MapChooserSharpSettings.Groups.Group2.extra.AnotherShop]
cost = 999
```

Map setting

```toml
[ze_example_xyz]
MinPlayers = 40
GroupSettings = ["Group1", "Group2"]

[ze_example_xyz.extra.ExternalShop]
cost = 10000
```

The final result is:

```toml
[ze_example_xyz]
MinPlayers = 40

[ze_example_xyz.extra.ExternalShop]
cost = 10000

[ze_example_xyz.extra.AnotherShop]
cost = 999

[ze_example_xyz.extra.shop]
cost = 100
```

### 4-2. Cooldown Behavior

Cooldown settings are handled separately for maps and groups and are applied independently.

For example, suppose `Group1` has a cooldown of `15` and `ze_example_xyz` has a cooldown of `20`.

And suppose `ze_example_xyz` and `ze_example_abc` belong to the same group:

```toml
[ze_example_xyz]
Cooldown = 20
GroupSettings = ["Group1"]

[ze_example_abc]
Cooldown = 0
GroupSettings = ["Group1"]

[MapChooserSharpSettings.Groups.Group1]
Cooldown = 15
```

When `ze_example_xyz` is played, a cooldown of `15` is applied to the group and a cooldown of `20` is applied to the map separately.

The group cooldown also affects other maps belonging to the same group. In this case, `ze_example_abc` is also affected by the group cooldown, effectively giving it a cooldown of `15`.

### 4-3. CooldownOverride

Group settings support `CooldownOverride`. This value has higher priority than the map's `Cooldown` and forcibly overrides the cooldown of maps belonging to the group.

```toml
[MapChooserSharpSettings.Groups.Group1]
Cooldown = 15
CooldownOverride = 30

[ze_example_xyz]
Cooldown = 20
GroupSettings = ["Group1"]
```

In this case, `ze_example_xyz`'s map cooldown becomes `30` due to `CooldownOverride`. Even though the map specifies `Cooldown = 20`, the group's `CooldownOverride` takes precedence.

`CooldownOverride` is separate from the group's `Cooldown` (group cooldown). The group's `Cooldown` is a cooldown applied to the group as a whole, while `CooldownOverride` is a setting to override the individual cooldown of maps belonging to the group.

Note that `CooldownOverride` is a group-only setting and cannot be used in map settings.

---

## DaySettings (Override Settings)

`DaySettings` allows you to override map or group settings based on specific days of the week and time ranges.

### DaySettings Syntax

Define with `[<MapName or GroupName>.DaySettings.<ArbitraryName>]`. The `<ArbitraryName>` is a label and has no special meaning. You can define any number of overrides for the same map/group.

### Map-Level DaySettings

```toml
[ze_example_abc.DaySettings.WeekendNight]
Enabled = true
ForceOverride = false
OverridePriority = 1
TargetDays = ["saturday", "sunday"]
TargetTimeRanges = ["18:00-03:00"]

# Settings to override
MaxExtends = 5
MinPlayers = 20
OnlyNomination = false

# Extra settings can also be used within DaySettings
[ze_example_abc.DaySettings.WeekendNight.extra.shop]
cost = 50
```

Example of adding a different schedule override for the same map:

```toml
[ze_example_abc.DaySettings.Weekday]
Enabled = true
ForceOverride = false
OverridePriority = 0
TargetDays = ["monday", "tuesday", "wednesday", "thursday", "friday"]

MaxPlayers = 32
MinPlayers = 5
```

### Group-Level DaySettings

Groups can also have DaySettings. These overrides apply to all maps belonging to that group.

```toml
[MapChooserSharpSettings.Groups.HardZeMap.DaySettings.WeekendAfternoon]
Enabled = true
ForceOverride = false
OverridePriority = 1
TargetDays = ["saturday", "sunday"]
TargetTimeRanges = ["14:00-18:00"]

OnlyNomination = false
MinPlayers = 10

[MapChooserSharpSettings.Groups.HardZeMap.DaySettings.WeekendAfternoon.extra.shop]
cost = 50
```

### DaySettings Priority

The overall configuration priority including DaySettings is as follows:

```
Normal: OverrideMap > Map > OverrideGroup > Group > Default
```

Map-level DaySettings take priority over map settings, but group-level DaySettings have lower priority than map settings. This prevents group DaySettings from unintentionally overriding values explicitly set on a map.

When `ForceOverride` is enabled, it takes the highest priority, ignoring the normal priority order.

```
ForceOverride enabled: ForceOverride (ordered by priority) > OverrideMap > Map > OverrideGroup > Group > Default
```

### DaySettings Properties

In a DaySettings section, you can specify the following override control properties along with existing map/group config properties such as `Cooldown` and `MinPlayers`. The specified properties override the base settings when conditions match.

### Enabled

Specifies whether this override is active. Default is `true`.

### ForceOverride

When enabled, forcibly overrides all settings ignoring the normal priority order. Useful for temporarily changing settings during events. When multiple `ForceOverride` entries match, the `OverridePriority` value determines the order. Default is `false`.

### OverridePriority

When multiple overrides match the same map/group, the one with the higher value takes priority. Default is `0`.

### TargetDays

Specifies which days of the week the override applies. This value is required.

`TargetDays = ["saturday", "sunday"]` means this override is applied only on Saturday and Sunday.

### TargetTimeRanges

Specifies the time ranges during which the override applies. If omitted, the override applies all day.

`TargetTimeRanges = ["18:00-03:00"]` means this override is applied only between 18:00 and 03:00 the next day.

---

## Setting Details

```toml
[ze_example_abc]
MapNameAlias = "ze example a b c"
MapDescription = "This map contains a jump scare"
IsDisabled = false
WorkshopId = 1234567891234
OnlyNomination = false
MapSelectionWeight = 1
Cooldown = 60
CooldownDateTime = "2d"
NominationCooldown = 0
NominationCooldownDateTime = ""
MaxExtends = 3
MaxExtCommandUses = 1
ExtendTimePerExtends = 15
MapTime = 20
ExtendRoundsPerExtends = 5
MapRounds = 10
MaxPlayers = 64
MinPlayers = 10
ProhibitAdminNomination = false
DaysAllowed = ["wednesday", "monday"]
AllowedTimeRanges = ["10:00-12:00", "20:00-22:00", "22:00-03:00"]
GroupSettings = ["HardZeMap"]

[ze_example_abc.extra.shop]
cost = 100
```

Group setting example:

```toml
[MapChooserSharpSettings.Groups.HardZeMap]
ShortGroupName = "HD"
NominationLimit = 3
CooldownOverride = 30
Cooldown = 15
MinPlayers = 16
OnlyNomination = true
```

## General Map Settings

### MapNameAlias

Allows you to change the display name shown in the vote screen, etc.

### MapDescription

Specifies the content displayed by the `!mapinfo` command.

### IsDisabled

Specifies whether the map is enabled.
When disabled, the map cannot be nominated and will not be selected in random map selection for votes.

### WorkshopId

For Workshop maps, specifying this value means you won't need to edit the config even if the map name changes.

If this key is not set, Workshop maps are changed via `ds_workshop_changelevel <keyName>` and official maps via `changelevel <keyName>`.

The top-level key name is used as the map name. For example, `[ze_example_abc]` results in `ds_workshop_changelevel ze_example_abc`.

### OnlyNomination

Specifies whether the map is nomination-only.
When enabled, the map will not be selected in random map selection for votes.

### MapSelectionWeight

Weight for random map selection. Higher values make the map more likely to be selected. Default is `1`. Setting to `0` excludes the map from random selection. However, unlike `OnlyNomination = true`, the pickable flag (`IsPickable`) itself is not changed.

### Cooldown

Specifies the cooldown applied after the map is played.
Note that this is separate from group cooldown.

### CooldownOverride (Group Only)

Can only be used in group settings. When specified, this value forcibly overrides the `Cooldown` of maps belonging to the group. It has higher priority than the map's `Cooldown` setting.

This value is separate from the group's `Cooldown` (group cooldown). The group's `Cooldown` is a cooldown applied to the group as a whole, while `CooldownOverride` overrides the individual cooldown of each map.

### CooldownDateTime

A time-based cooldown applied in addition to the normal cooldown. The cooldown state is not released until the specified duration has elapsed.

Available suffixes: `"h"` (hours), `"d"` (days), `"w"` (weeks), `"m"` (months = 30 days). Leave empty to disable.

Example: `CooldownDateTime = "2d"` sets a 2-day cooldown.

### NominationCooldown

A nomination-specific cooldown (count-based) applied after the map is consumed as a nomination candidate. It operates independently from the regular `Cooldown`. Default is `0` (disabled).

### NominationCooldownDateTime

A time-based cooldown specific to nominations. The same suffixes as `CooldownDateTime` can be used.

### MaxExtends

Specifies the maximum number of extends for the map.

### MaxExtCommandUses

Specifies the maximum number of extends that can be performed via the `!ext` command.

### ExtendTimePerExtends

For time-based maps, specifies how many minutes are added per extend.

### MapTime

For time-based maps, specifies the map's time limit.

### ExtendRoundsPerExtends

For round-based maps, specifies how many rounds are added per extend.

### MapRounds

For round-based maps, specifies the number of rounds for the map.

## Nomination Settings

### MaxPlayers

Nomination is disabled when the number of players on the server exceeds this value.

### MinPlayers

Nomination is disabled when the number of players on the server is below this value.

### ProhibitAdminNomination

Prohibits nomination via `!nominate_addmap`.

Note that this does not prohibit nomination from the console.

### DaysAllowed

Nomination is allowed only on the days listed here. Note that this is applied in conjunction with AllowedTimeRanges, so be careful when specifying days.

`DaysAllowed = ["wednesday", "monday"]` allows nomination only on Wednesday and Monday.

### AllowedTimeRanges

Nomination is allowed only during the time ranges listed here. Note that this is applied in conjunction with DaysAllowed, so be careful when specifying time ranges.

`AllowedTimeRanges = ["10:00-12:00", "22:00-03:00"]` allows nomination only between 10:00-12:00 or 22:00-03:00.

## Group-Only Settings

The following settings can only be used in group settings (`[MapChooserSharpSettings.Groups.<GroupName>]`).

### ShortGroupName

A tag used for abbreviated group name display in the vote screen, etc. Maximum 4 characters.

Example: `ShortGroupName = "HD"`

### NominationLimit

The maximum number of maps that can be nominated from this group. `0` means unlimited. Default is `0`.

Different limits can be set per group. For example, HardGroup can have a maximum of 2 maps and EasyGroup a maximum of 5 maps.

```toml
[MapChooserSharpSettings.Groups.HardGroup]
NominationLimit = 2

[MapChooserSharpSettings.Groups.EasyGroup]
NominationLimit = 5
```

### Extra Settings

See the [MCS API Documentation](../development/USING_MCS_API.md).
