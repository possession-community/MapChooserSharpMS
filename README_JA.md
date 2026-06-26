# MapChooserSharpMS

Counter-Strike 2 向けの [ModSharp](https://github.com/Kxnrl/modsharp-public) モジュールで、マップ投票・ノミネーション・RTV・マップサイクル管理を提供します。

[MapChooserSharp](https://github.com/fltuna/MapChooserSharp) (CounterStrikeSharp 版、EOL) の後継プロジェクトです。

## Translated README

[English](README.md)

## Features

- マップ投票 (タイムリミット / ラウンド数) + 延長・サウンド対応
- ノミネーション — 部分一致・検索タグ・マップ/グループ単位の制限設定
- Rock The Vote — 2段階閾値 + 時間減衰
- マップサイクル管理 — カウント・時間ベースのクールダウン (DB 永続化)
- Workshop コレクション同期・公開状態チェック
- Discord Webhook 通知
- プレイヤー設定 (カウントダウン UI、投票サウンド音量)
- 外部プラグイン向け API

## ドキュメント

| カテゴリ | リンク |
|---|---|
| 設定 | [ConVars](docs/ja/configuration/CONVARS.md) / [マップ設定](docs/ja/configuration/MAP_CONFIG.md) ([エディタ](https://github.com/possession-community/MapChooserSharpMSEditor)) / [プラグイン設定](docs/ja/configuration/PLUGIN_CONFIG.md) |
| 機能 | [コマンド](docs/ja/features/COMMANDS.md) / [内部動作](docs/ja/features/BEHAVIOUR.md) / [Workshop 同期](docs/ja/features/WORKSHOP_SYNC.md) / [Webhook](docs/ja/features/WEBHOOK.md) |
| API | [はじめに](docs/ja/development/USING_MCS_API.md) / [MapConfig](docs/ja/development/api/map-config.md) / [Nomination](docs/ja/development/api/nomination.md) / [MapVote](docs/ja/development/api/map-vote.md) / [MapCycle](docs/ja/development/api/map-cycle.md) / [RTV](docs/ja/development/api/rtv.md) / [Workshop](docs/ja/development/api/workshop.md) / [Menu](docs/ja/development/api/menu.md) / [Events](docs/ja/development/api/events.md) |

## プラグイン開発者向け

詳しくは [MCS API の使い方](docs/ja/development/USING_MCS_API.md) を参照してください。

## Special Thanks

- [Uru](https://github.com/2vg) — テスト・デバッグ

## License

Copyright (c) 2025-2026 faketuna
