# Discord Webhook Notifications

## Overview

MapChooserSharpMS can send Discord Webhook notifications when server events occur.
Each webhook is managed by an independent TOML configuration file. Placeholders (`%PLACEHOLDER%`) in the JSON template are replaced with event data before POSTing to the Discord API.

Two webhook types are currently available:

| Webhook | File Name | Trigger |
|---|---|---|
| Workshop Visibility Check | `workshop-visibility-check-webhook.toml` | A Private, Deleted, or Error map is detected during the Workshop visibility check at map start |
| Map Transition | `map-transition-webhook.toml` | Map transition (intermission or map end) |

## Setup

### 1. Obtain a Discord Webhook URL

1. Open the settings for the target notification channel in your Discord server
2. Go to **Integrations** > **Webhooks** > **New Webhook**
3. Click **Copy Webhook URL**

### 2. Template File Placement

On first launch, default template files are auto-generated in the module directory:

```
%MOD_SHARP_DIR%/modules/MapChooserSharpMS/
  +-- workshop-visibility-check-webhook.toml
  +-- map-transition-webhook.toml
```

The auto-generated files have an empty `WebhookUrl` by default. Set it to the URL you copied:

```toml
WebhookUrl = "https://discord.com/api/webhooks/YOUR_WEBHOOK_ID/YOUR_WEBHOOK_TOKEN"
```

> **Note:** If `WebhookUrl` is an empty string or the file does not exist, the webhook is not sent.

## How Templates Work

Each template file is a TOML file consisting of two keys:

| Key | Description |
|---|---|
| `WebhookUrl` | Discord Webhook URL |
| `JsonTemplate` | JSON body template to POST to the Discord API |

`JsonTemplate` uses TOML multiline literal strings (`'''...'''`).
Placeholders (`%PLACEHOLDER%`) in the template are replaced with event data values before sending.

During replacement, `\`, `"`, newlines, and tabs in values are automatically JSON-escaped, so no manual escaping is needed in the template.

## Available Webhooks

### Workshop Visibility Check Webhook

Sent when the background Workshop visibility check at map start detects Private, Deleted, or API Error maps. **One notification is sent per affected map.**

Not sent if there are no problematic maps.

#### Placeholders

| Placeholder | Description |
|---|---|
| `%MAP_NAME%` | Map name in config |
| `%WORKSHOP_ID%` | Workshop ID |
| `%WORKSHOP_TITLE%` | Title on Workshop (empty string if unavailable) |
| `%STATUS%` | `Private/Deleted` or `Error` |
| `%TOTAL_COUNT%` | Total number of maps checked |
| `%UNCHANGED_COUNT%` | Number of maps with no issues (Public/FriendsOnly/Unlisted) |
| `%PRIVATE_DELETED_COUNT%` | Number of private/deleted maps |
| `%ERROR_COUNT%` | Number of maps with API errors |
| `%TIMESTAMP%` | Check execution time (UTC, `yyyy-MM-dd HH:mm:ss UTC` format) |

#### Default Template

```toml
WebhookUrl = ""

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

### Map Transition Webhook

Sent at map transition. Fires once on whichever occurs first: intermission (the transition screen after voting completes) or map end (`OnGameDeactivate`).

Not sent if the next map has not been confirmed.

#### Placeholders

| Placeholder | Description |
|---|---|
| `%CURRENT_MAP%` | Current map name (engine map name) |
| `%CURRENT_MAP_DISPLAY_NAME%` | Current map display name (uses `MapNameAlias` if set) |
| `%CURRENT_WORKSHOP_ID%` | Current map's Workshop ID (`0` if not set) |
| `%NEXT_MAP%` | Next map name |
| `%NEXT_MAP_DISPLAY_NAME%` | Next map display name |
| `%NEXT_WORKSHOP_ID%` | Next map's Workshop ID (`0` if not set) |
| `%PLAYER_COUNT%` | Player count at transition (excluding Bots/HLTV) |
| `%MAX_PLAYERS%` | Server's maximum player count |
| `%TIMESTAMP%` | Event time (UTC, `yyyy-MM-dd HH:mm:ss UTC` format) |

> **Note:** `%PLAYER_COUNT%` preferentially uses the live player count at the time of intermission/`OnGameDeactivate`. If unavailable, it falls back to a snapshot taken at `OnNextMapConfirmed`.

#### Default Template

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

## Customizing Templates

You can freely customize the notification content by editing the template files directly.

### Tips

- The `JsonTemplate` content must be valid JSON conforming to the [Discord Execute Webhook API](https://discord.com/developers/docs/resources/webhook#execute-webhook)
- Because TOML multiline literal strings (`'''...'''`) are used, do not use `'''` inside the template
- The Embed `color` value is a decimal integer (e.g., red = `16711680` = `0xFF0000`, green = `3066993` = `0x2ECE71`)
- You can remove unnecessary fields or add new ones (follow the Discord Embed specification)

### Example

Adding a server name and icon:

```toml
WebhookUrl = "https://discord.com/api/webhooks/..."

JsonTemplate = '''
{
  "username": "MCS Bot",
  "avatar_url": "https://example.com/icon.png",
  "embeds": [{
    "title": "Map Changed",
    "description": "%CURRENT_MAP_DISPLAY_NAME% → %NEXT_MAP_DISPLAY_NAME%",
    "color": 3066993,
    "fields": [
      {"name": "Players", "value": "%PLAYER_COUNT%/%MAX_PLAYERS%", "inline": true}
    ],
    "timestamp": "%TIMESTAMP%"
  }]
}
'''
```

> **Note:** Discord's `timestamp` field requires ISO 8601 format, but MCS timestamps use `yyyy-MM-dd HH:mm:ss UTC` format. This may not display correctly as an embed `timestamp`. Using `footer.text` is recommended instead.

### Resetting a Template

Delete the template file and reload the plugin (server restart or `!reloadmapcfgs`) to regenerate the default template.

## Troubleshooting

### Webhook is not being sent

1. **Verify `WebhookUrl` is not empty**
   - Open the template file and check that `WebhookUrl` contains a valid Discord Webhook URL
   - The default is an empty string (`""`), so webhooks are not sent unless configured

2. **Verify the template file exists**
   - Check that the TOML file exists in the module directory (`%MOD_SHARP_DIR%/modules/MapChooserSharpMS/`)
   - If the file is missing, reload the plugin to auto-generate it

3. **For Visibility Check Webhook: Verify problematic maps exist**
   - Nothing is sent if all maps are in a normal state
   - If no Steam Web API Key is configured, the Visibility Check itself does not run

4. **For Map Transition Webhook: Verify the next map is confirmed**
   - If no vote occurred and the next map is not confirmed, `OnMcsIntermission` fires but the webhook may only be sent via the `OnGameDeactivate` path

### JSON Parse Error / Discord returns 400

- Check the JSON syntax in the template (trailing commas, unclosed quotes, etc.)
- For TOML parse errors, the server log outputs `Failed to parse webhook config`

### Checking Logs

Webhook-related log messages:

| Log Level | Message | Description |
|---|---|---|
| Debug | `Webhook config not found: {Path}` | Template file not found |
| Debug | `Webhook URL is empty, skipping: {Path}` | Skipped because URL is not set |
| Debug | `Webhook sent successfully: {Path}` | Sent successfully |
| Warning | `Webhook config missing WebhookUrl: {Path}` | TOML is missing the `WebhookUrl` key |
| Warning | `Webhook config missing JsonTemplate: {Path}` | TOML is missing the `JsonTemplate` key |
| Warning | `Failed to parse webhook config: {Path}` | TOML parse error |
| Warning | `Webhook POST failed ({Status}): {Path}` | HTTP status error (rejected by Discord, etc.) |
| Warning | `Webhook POST exception: {Path}` | Exception such as a network error |
| Information | `Created default webhook template: {File}` | Auto-generated a default template |

## Related Documentation

- [Workshop Sync / Visibility Check](WORKSHOP_SYNC.md) -- Details on Workshop sync and visibility checking
