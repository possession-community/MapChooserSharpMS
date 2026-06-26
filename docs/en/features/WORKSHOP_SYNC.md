# Workshop Sync / Visibility Check

## Overview
Automatic map synchronization from Workshop collections and Workshop item visibility checking at map start.

## Steam Web API Key

A Steam Web API Key is required to use the `IPublishedFileService/GetDetails` endpoint.

### How to Obtain
Issue a key from https://steamcommunity.com/dev/apikey

### Configuration (either method)

**config.toml:**
```toml
[General]
SteamWebApiKey = "YOUR_API_KEY_HERE"
```

**Environment variable (fallback when config is empty):**
```
STEAM_WEB_API_KEY=YOUR_API_KEY_HERE
```

## Workshop Collection Sync

At plugin load, maps from collections specified in `WorkshopCollectionIds` are automatically fetched.
TOML files are auto-generated under `maps/synced_workshop/` for public maps not in the existing config, then reloaded.

```toml
[General]
WorkshopCollectionIds = ["3070257939", "1234567890"]
```

## Visibility Check (At Map Start)

At map start, the visibility status of all Workshop maps is checked in the background.

### Criteria
| visibility | Status | Result |
|---|---|---|
| 0 | Public | OK |
| 1 | Friends Only | OK (downloadable from server) |
| 2 | Private | NG |
| 3 | Unlisted | OK (downloadable from server) |
| result=9 | Not Found (deleted) | NG |

### Log Output
```
Workshop visibility check: Unchanged 515 | Private/Deleted 3 | Errors 0
Workshop unavailable: ze_example_map (workshop 1234567890)
```

### Auto Disable
Private / deleted maps are automatically marked with `IsDisabled = true` in the config and reloaded.

### Discord Webhook Notification

When the Visibility Check finds Private/Deleted/Error maps, a Discord Webhook notification is sent for each affected map.

#### Configuration File

Place `workshop-visibility-check-webhook.toml` in the module directory root:

```toml
WebhookUrl = "https://discord.com/api/webhooks/YOUR_WEBHOOK_ID/YOUR_WEBHOOK_TOKEN"

JsonTemplate = '''
{
  "embeds": [{
    "title": "Workshop Map Unavailable",
    "description": "**%MAP_NAME%** (ID: %WORKSHOP_ID%) is now %STATUS%",
    "color": 16711680,
    "fields": [
      {"name": "Workshop Title", "value": "%WORKSHOP_TITLE%", "inline": true},
      {"name": "Status", "value": "%STATUS%", "inline": true}
    ],
    "footer": {"text": "%TIMESTAMP% | Total: %TOTAL_COUNT% OK: %UNCHANGED_COUNT% NG: %PRIVATE_DELETED_COUNT%"}
  }]
}
'''
```

#### Placeholders (per-map)

| Placeholder | Description |
|---|---|
| `%MAP_NAME%` | Map name in config |
| `%WORKSHOP_ID%` | Workshop ID |
| `%WORKSHOP_TITLE%` | Title on Workshop (empty if unavailable) |
| `%STATUS%` | `Private/Deleted` or `Error` |
| `%TOTAL_COUNT%` | Total number of maps checked |
| `%UNCHANGED_COUNT%` | Number of maps with no issues |
| `%PRIVATE_DELETED_COUNT%` | Number of private/deleted maps |
| `%ERROR_COUNT%` | Number of maps with errors |
| `%TIMESTAMP%` | Check execution time (UTC) |

- If `WebhookUrl` is empty or the file does not exist, webhooks are not sent
- If there are no problematic maps, webhooks are not sent
- The JSON template is written using TOML multiline literal strings (`'''...'''`)

### Map Transition Webhook

Sends a Discord Webhook notification at map transition (intermission or map end).

#### Configuration File

Place `map-transition-webhook.toml` in the module directory root (auto-generated on first launch):

```toml
WebhookUrl = ""

JsonTemplate = '''
{
  "embeds": [{
    "title": "Map Transition",
    "description": "**%CURRENT_MAP_DISPLAY_NAME%** → **%NEXT_MAP_DISPLAY_NAME%**",
    "color": 3066993,
    "fields": [
      {"name": "Players", "value": "%PLAYER_COUNT%/%MAX_PLAYERS%", "inline": true},
      {"name": "Next Workshop ID", "value": "%NEXT_WORKSHOP_ID%", "inline": true}
    ],
    "footer": {"text": "%TIMESTAMP%"}
  }]
}
'''
```

#### Placeholders

| Placeholder | Description |
|---|---|
| `%CURRENT_MAP%` | Current map name |
| `%CURRENT_MAP_DISPLAY_NAME%` | Current map display name |
| `%CURRENT_WORKSHOP_ID%` | Current map's Workshop ID |
| `%NEXT_MAP%` | Next map name |
| `%NEXT_MAP_DISPLAY_NAME%` | Next map display name |
| `%NEXT_WORKSHOP_ID%` | Next map's Workshop ID (0 if not set) |
| `%PLAYER_COUNT%` | Player count at transition (excluding Bots/HLTV) |
| `%MAX_PLAYERS%` | Server's maximum player count |
| `%TIMESTAMP%` | Event time (UTC) |

## Workshop Remote Fetch (Admin Commands)

Even Workshop maps not registered in the config can be used by fetching information from the Steam API to create a provisional MapConfig.

| Command | Description |
|---|---|
| `!setnextwsmap <workshopId>` | Set as next map by Workshop ID (API fetch if not in config) |
| `!wsmap <workshopId>` | Immediately change map (API fetch + transition watchdog) |
| `!nominate_addwsmap <workshopId>` | Add as admin nomination |

Provisional MapConfigs are created with default values (MaxExtends=3, MapTime=20, etc.), and the Workshop title is used directly as the MapName.

**Requirement:** A Steam Web API Key must be configured. An error is returned if not set.

## AutoFixMapName

At map start, if a map config is found by Workshop ID and the map name in the config differs from the actual map name, the TOML file is renamed to auto-correct.

```toml
[General]
ShouldAutoFixMapName = true
```
