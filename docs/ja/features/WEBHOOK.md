# Discord Webhook 通知

## 概要

MapChooserSharpMS はサーバーイベント発生時に Discord Webhook を通じて通知を送信する機能を備えています。
各 Webhook は独立した TOML 設定ファイルで管理され、JSON テンプレート内の `%PLACEHOLDER%` をイベントデータで置換して Discord API に POST します。

現在、以下の 2 種類の Webhook が利用可能です:

| Webhook | ファイル名 | トリガー |
|---|---|---|
| Workshop Visibility Check | `workshop-visibility-check-webhook.toml` | マップ開始時の Workshop 公開状態チェックで非公開/削除/エラーのマップを検出した場合 |
| Map Transition | `map-transition-webhook.toml` | マップ遷移時 (インターミッション or マップ終了) |

## セットアップ

### 1. Discord Webhook URL の取得

1. Discord サーバーの通知先チャンネルの設定を開く
2. **連携サービス** > **ウェブフック** > **新しいウェブフック** を作成
3. **ウェブフック URL をコピー** をクリック

### 2. テンプレートファイルの配置

プラグインの初回起動時に、モジュールディレクトリ直下にデフォルトのテンプレートファイルが自動生成されます:

```
%MOD_SHARP_DIR%/modules/MapChooserSharpMS/
  +-- workshop-visibility-check-webhook.toml
  +-- map-transition-webhook.toml
```

自動生成されたファイルの `WebhookUrl` はデフォルトで空文字列になっているため、コピーした URL を設定してください:

```toml
WebhookUrl = "https://discord.com/api/webhooks/YOUR_WEBHOOK_ID/YOUR_WEBHOOK_TOKEN"
```

> **注意:** `WebhookUrl` が空文字列またはファイルが存在しない場合、その Webhook は送信されません。

## テンプレートの仕組み

各テンプレートファイルは以下の 2 つのキーで構成される TOML ファイルです:

| キー | 説明 |
|---|---|
| `WebhookUrl` | Discord Webhook の URL |
| `JsonTemplate` | Discord API に POST する JSON 本文のテンプレート |

`JsonTemplate` には TOML の multiline literal string (`'''...'''`) を使用します。
テンプレート内の `%PLACEHOLDER%` がイベントデータの値に置換されて送信されます。

置換時、値に含まれる `\`、`"`、改行、タブは自動的に JSON エスケープされるため、テンプレート側でエスケープ処理を行う必要はありません。

## 利用可能な Webhook

### Workshop Visibility Check Webhook

マップ開始時にバックグラウンドで実行される Workshop 公開状態チェックの結果、非公開 (Private) / 削除済み (Deleted) / API エラーのマップが検出された場合に送信されます。**問題のあるマップごとに 1 通ずつ**送信されます。

問題のあるマップが無い場合は送信されません。

#### プレースホルダー

| プレースホルダー | 説明 |
|---|---|
| `%MAP_NAME%` | マップ config 上の名前 |
| `%WORKSHOP_ID%` | Workshop ID |
| `%WORKSHOP_TITLE%` | Workshop 上のタイトル (取得できない場合は空文字列) |
| `%STATUS%` | `Private/Deleted` または `Error` |
| `%TOTAL_COUNT%` | チェック対象の総マップ数 |
| `%UNCHANGED_COUNT%` | 正常 (Public/FriendsOnly/Unlisted) なマップ数 |
| `%PRIVATE_DELETED_COUNT%` | 非公開/削除されたマップ数 |
| `%ERROR_COUNT%` | API エラーのマップ数 |
| `%TIMESTAMP%` | チェック実行時刻 (UTC, `yyyy-MM-dd HH:mm:ss UTC` 形式) |

#### デフォルトテンプレート

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

マップ遷移時に送信されます。インターミッション (投票完了後の遷移画面) またはマップ終了時 (`OnGameDeactivate`) のいずれか先に発生した方で 1 回だけ送信されます。

次のマップが確定していない場合は送信されません。

#### プレースホルダー

| プレースホルダー | 説明 |
|---|---|
| `%CURRENT_MAP%` | 現在のマップ名 (エンジンマップ名) |
| `%CURRENT_MAP_DISPLAY_NAME%` | 現在のマップ表示名 (`MapNameAlias` があればそれを使用) |
| `%CURRENT_WORKSHOP_ID%` | 現在のマップの Workshop ID (未設定時は `0`) |
| `%NEXT_MAP%` | 次のマップ名 |
| `%NEXT_MAP_DISPLAY_NAME%` | 次のマップ表示名 |
| `%NEXT_WORKSHOP_ID%` | 次のマップの Workshop ID (未設定時は `0`) |
| `%PLAYER_COUNT%` | 遷移時のプレイヤー数 (Bot/HLTV 除外) |
| `%MAX_PLAYERS%` | サーバーの最大プレイヤー数 |
| `%TIMESTAMP%` | イベント時刻 (UTC, `yyyy-MM-dd HH:mm:ss UTC` 形式) |

> **補足:** `%PLAYER_COUNT%` はインターミッション/`OnGameDeactivate` 時点のライブプレイヤー数を優先しますが、取得できない場合は `OnNextMapConfirmed` 時点でスナップショットした値にフォールバックします。

#### デフォルトテンプレート

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

## テンプレートのカスタマイズ

テンプレートファイルを直接編集することで、送信内容を自由にカスタマイズできます。

### 編集のポイント

- `JsonTemplate` の内容は [Discord Webhook の Execute Webhook API](https://discord.com/developers/docs/resources/webhook#execute-webhook) に準拠した JSON である必要があります
- TOML の multiline literal string (`'''...'''`) で記述するため、テンプレート内で `'''` を使わないでください
- Embed の `color` は 10 進数の整数値です (例: 赤 = `16711680` = `0xFF0000`、緑 = `3066993` = `0x2ECE71`)
- 不要なフィールドは削除、新しいフィールドの追加も可能です (Discord Embed の仕様に従ってください)

### カスタマイズ例

サーバー名やアイコンを追加する例:

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

> **注意:** Discord の `timestamp` フィールドは ISO 8601 形式を要求しますが、MCS のタイムスタンプは `yyyy-MM-dd HH:mm:ss UTC` 形式のため、そのままでは embed の `timestamp` として正しく表示されない場合があります。`footer.text` に入れる使い方を推奨します。

### テンプレートのリセット

テンプレートファイルを削除してプラグインを再ロード (サーバー再起動 or `!reloadmapcfgs`) すると、デフォルトのテンプレートが再生成されます。

## トラブルシューティング

### Webhook が送信されない

1. **`WebhookUrl` が空でないか確認**
   - テンプレートファイルを開き、`WebhookUrl` に有効な Discord Webhook URL が設定されているか確認してください
   - デフォルトでは空文字列 (`""`) のため、設定しないと送信されません

2. **テンプレートファイルが存在するか確認**
   - モジュールディレクトリ (`%MOD_SHARP_DIR%/modules/MapChooserSharpMS/`) に TOML ファイルが存在するか確認してください
   - ファイルが無い場合はプラグインを再ロードすると自動生成されます

3. **Visibility Check Webhook の場合: 問題のあるマップがあるか確認**
   - 全マップが正常な場合は送信されません
   - Steam Web API Key が設定されていない場合、Visibility Check 自体が実行されません

4. **Map Transition Webhook の場合: 次のマップが確定しているか確認**
   - 投票が行われず次のマップが未確定の場合、`OnMcsIntermission` は発火しますが、`OnGameDeactivate` 経由でのみ送信される可能性があります

### JSON パースエラー / Discord が 400 を返す

- テンプレートの JSON 構文が正しいか確認してください (末尾カンマ、引用符の閉じ忘れなど)
- TOML パースエラーの場合、サーバーログに `Failed to parse webhook config` が出力されます

### ログの確認

Webhook 関連のログメッセージ:

| ログレベル | メッセージ | 説明 |
|---|---|---|
| Debug | `Webhook config not found: {Path}` | テンプレートファイルが見つからない |
| Debug | `Webhook URL is empty, skipping: {Path}` | URL 未設定のためスキップ |
| Debug | `Webhook sent successfully: {Path}` | 送信成功 |
| Warning | `Webhook config missing WebhookUrl: {Path}` | TOML に `WebhookUrl` キーが無い |
| Warning | `Webhook config missing JsonTemplate: {Path}` | TOML に `JsonTemplate` キーが無い |
| Warning | `Failed to parse webhook config: {Path}` | TOML パースエラー |
| Warning | `Webhook POST failed ({Status}): {Path}` | HTTP ステータスエラー (Discord 側の拒否等) |
| Warning | `Webhook POST exception: {Path}` | ネットワークエラー等の例外 |
| Information | `Created default webhook template: {File}` | デフォルトテンプレートを自動生成した |

## 関連ドキュメント

- [Workshop Sync / Visibility Check](WORKSHOP_SYNC.md) -- Workshop 同期・公開状態チェックの詳細
