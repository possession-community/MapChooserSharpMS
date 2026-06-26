# MapChooserSharpMS

A [ModSharp](https://github.com/Kxnrl/modsharp-public) module for Counter-Strike 2 that provides map voting, nomination, RTV, and map cycle management with powerful configuration and API.

Successor to [MapChooserSharp](https://github.com/fltuna/MapChooserSharp) (CounterStrikeSharp version, EOL).

## Translated README

[日本語](README_JA.md)

## Features

- Map voting (time limit / max rounds) with extend and sounds
- Nomination with partial match, search tags, and per-map/group restrictions
- Rock The Vote with two-stage threshold and time decay
- Map cycle management with count-based and timed cooldown (DB persistence)
- Workshop collection sync and visibility check
- Discord webhook notifications
- Per-player settings (countdown UI, vote sound volume)
- Powerful API for external plugins

## Documentation

| Category      | Links                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
|---------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Configuration | [ConVars](docs/en/configuration/CONVARS.md) / [Map Config](docs/en/configuration/MAP_CONFIG.md) ([Editor](https://github.com/possession-community/MapChooserSharpMSEditor)) / [Plugin Config](docs/en/configuration/PLUGIN_CONFIG.md)                                                                                                                                                                                                                                                                                                |
| Features      | [Commands](docs/en/features/COMMANDS.md) / [Behaviour](docs/en/features/BEHAVIOUR.md) / [Workshop Sync](docs/en/features/WORKSHOP_SYNC.md) / [Webhook](docs/en/features/WEBHOOK.md)                                                                                                                                                                                                                                                                      |
| API           | [Getting Started](docs/en/development/USING_MCS_API.md) / [MapConfig](docs/en/development/api/map-config.md) / [Nomination](docs/en/development/api/nomination.md) / [MapVote](docs/en/development/api/map-vote.md) / [MapCycle](docs/en/development/api/map-cycle.md) / [RTV](docs/en/development/api/rtv.md) / [Workshop](docs/en/development/api/workshop.md) / [Menu](docs/en/development/api/menu.md) / [Events](docs/en/development/api/events.md) |

## For Plugin Developers

See [Using MCS API](docs/en/development/USING_MCS_API.md) for details.

## Special Thanks

- [Uru](https://github.com/2vg) — Testing and debugging

## License

Copyright (c) 2025-2026 faketuna
