# Workshop Sync / Visibility Check

## 概要
Workshop コレクションからのマップ自動同期と、マップ開始時のワークショップアイテム公開状態チェック。

## Steam Web API Key

`IPublishedFileService/GetDetails` エンドポイントを使用するため、Steam Web API Key が必要です。

### 取得方法
https://steamcommunity.com/dev/apikey からキーを発行

### 設定方法 (どちらか一方)

**config.toml:**
```toml
[General]
SteamWebApiKey = "YOUR_API_KEY_HERE"
```

**環境変数 (config が空の場合のフォールバック):**
```
STEAM_WEB_API_KEY=YOUR_API_KEY_HERE
```

## Workshop Collection Sync

プラグインロード時に `WorkshopCollectionIds` で指定されたコレクション内のマップを自動取得。
既存 config に無い公開マップの TOML を `maps/synced_workshop/` に自動生成してリロード。

```toml
[General]
WorkshopCollectionIds = ["3070257939", "1234567890"]
```

## Visibility Check (マップ開始時)

マップ開始時にバックグラウンドで全 Workshop マップの公開状態をチェック。

### 判定基準
| visibility | ステータス | 判定 |
|---|---|---|
| 0 | Public | OK |
| 1 | Friends Only | OK (サーバーからDL可能) |
| 2 | Private | NG |
| 3 | Unlisted | OK (サーバーからDL可能) |
| result=9 | Not Found (削除済み) | NG |

### ログ出力
```
Workshop visibility check: Unchanged 515 | Private/Deleted 3 | Errors 0
Workshop unavailable: ze_example_map (workshop 1234567890)
```

### 自動 Disable
Private / 削除済みのマップは config 上で `IsDisabled = true` を自動書込みし、リロードします。
将来的に Discord Webhook 通知も追加予定。

`LastVisibilityCheckResult` プロパティで構造化された結果を保持:
- `Unchanged`: 公開/Unlisted/FriendsOnly のマップリスト
- `PrivateOrDeleted`: 非公開/削除マップリスト
- `Errors`: API エラーのマップリスト

各エントリは `WorkshopMapEntry(MapName, WorkshopId, Title)` で Discord Webhook 連携用にマップ詳細を保持。

## Workshop リモートフェッチ (管理コマンド)

config に登録されていない Workshop マップでも、Steam API から情報をフェッチして仮の MapConfig を作成し、利用可能です。

| コマンド | 説明 |
|---|---|
| `!setnextmap <workshopId>` | 次マップに設定 (名前検索 → 未ヒット & 数値なら API フェッチ) |
| `!wsmap <workshopId>` | 即時マップ変更 (API フェッチ + transition watchdog 付き) |
| `!nominate_addwsmap <workshopId>` | 管理者ノミネーション追加 |

仮 MapConfig はデフォルト値 (MaxExtends=3, MapTime=20 等) で作成され、MapName には Workshop タイトルがそのまま使用されます。

**要件:** Steam Web API Key が設定されていること。未設定の場合はエラーを返します。

## AutoFixMapWorkshopId

マップ開始時、Workshop ID からマップ config を検索し、config 上のマップ名が実際のマップ名と異なる場合、TOML ファイルをリネームして自動修正。

```toml
[General]
ShouldAutoFixMapName = true
```
