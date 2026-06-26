# Workshop API

MCS は Steam Workshop と連携してマップの存在確認やメタデータ取得を行います。
ここでは Workshop 関連の公開型と、Workshop ID を指定したマップ設定フローを解説します。

Workshop コレクションの同期や公開状態の定期チェックは MCS 内部の WorkshopSync モジュールが自動で行うため、
外部プラグインからの直接操作は不要です。

---

## IWorkshopFetchResult

Workshop マップのフェッチ結果を表すインターフェースです。
`IMapTransitionManager.TrySetNextMap(long)` の戻り値に含まれます。

**名前空間**: `MapChooserSharpMS.Shared.WorkshopManagement`

| プロパティ | 型 | 説明 |
|---|---|---|
| `ExistenceStatus` | `ExistenceStatus` | フェッチ結果のステータス |
| `MapName` | `string?` | 解決されたマップ名。取得に失敗した場合は `null` |
| `WorkshopId` | `long?` | Workshop ID。取得に失敗した場合は `null` |

---

## ExistenceStatus

Workshop マップの解決結果を表す列挙型です。

**名前空間**: `MapChooserSharpMS.Shared.WorkshopManagement`

| 値 | 説明 |
|---|---|
| `FoundInMemoryConfig` | TOML マップ設定にこの Workshop ID のマップが既に登録されていた。Steam API へのリクエストは行われない |
| `FoundInWorkshop` | Steam API から Workshop アイテムが見つかり、公開状態が有効 (Public / Unlisted / FriendsOnly) だった。仮の `IMapConfig` が自動生成される |
| `NotAvailableInWorkshop` | Steam API に問い合わせたがアイテムが見つからなかった、または公開状態が無効 (Private / NotFound 等) だった |
| `FailedToFetchHttpError` | Steam API への HTTP リクエストが失敗した (ネットワークエラー、タイムアウト等) |
| `FailedToFetchUnknown` | Steam API キーが未設定など、リクエストを送信できなかった場合の汎用エラー |

---

## TrySetNextMap(long workshopId) の動作

`IMapTransitionManager.TrySetNextMap(long)` は Workshop ID を指定して次マップを設定するための非同期メソッドです。
`IMapChooserSharpShared.MapCycleController.MapTransitionManager` からアクセスします。

```csharp
Task<(bool Success, IWorkshopFetchResult FetchResult)> TrySetNextMap(long workshopId);
```

### 解決の流れ

1. **メモリ内設定の検索** -- まず TOML から読み込み済みのマップ設定の中から、一致する Workshop ID を持つ `IMapConfig` を探す。見つかった場合は `ExistenceStatus.FoundInMemoryConfig` で即座に成功を返す
2. **Steam API フォールバック** -- メモリ内に見つからなかった場合、Steam の `IPublishedFileService` API を使ってアイテム情報を非同期で取得する。アイテムが存在し公開状態が有効であれば、デフォルト値で仮の `IMapConfig` を自動生成して次マップに設定する (`ExistenceStatus.FoundInWorkshop`)
3. **失敗** -- アイテムが見つからない、非公開、または HTTP エラーの場合は `Success = false` となり、`FetchResult` に失敗の詳細が格納される

### 戻り値

| フィールド | 型 | 説明 |
|---|---|---|
| `Success` | `bool` | マップの設定に成功したかどうか |
| `FetchResult` | `IWorkshopFetchResult` | 解決結果の詳細。成功・失敗を問わず常に値が入る |

### 使用例

```csharp
var (success, result) = await mapTransitionManager.TrySetNextMap(3070581406);
if (success)
{
    // result.ExistenceStatus は FoundInMemoryConfig または FoundInWorkshop
    Console.WriteLine($"次マップを設定しました: {result.MapName}");
}
else
{
    switch (result.ExistenceStatus)
    {
        case ExistenceStatus.NotAvailableInWorkshop:
            Console.WriteLine("Workshop にマップが見つかりませんでした。");
            break;
        case ExistenceStatus.FailedToFetchHttpError:
            Console.WriteLine("Steam API との通信に失敗しました。");
            break;
        case ExistenceStatus.FailedToFetchUnknown:
            Console.WriteLine("API キーが未設定です。");
            break;
    }
}
```

### 非同期処理に関する注意

`TrySetNextMap(long)` は `Task` を返す非同期メソッドです。
Steam API への HTTP リクエストを伴うため、呼び出し元では `await` が必要です。
メモリ内設定にヒットした場合でも戻り値の型は `Task` のままですが、内部では同期的に完了します。

---

## Workshop の公開状態チェック

MCS の WorkshopSync モジュールは、設定ファイルに登録された Workshop マップの公開状態を定期的に確認します。
非公開 (Private) やアイテム削除済み (NotFound) のマップは自動的に無効化されます。

この機能は MCS 内部で完結しており、外部プラグインから操作する API は公開されていません。
公開状態チェックの結果は `IMapConfig.IsDisabled` に反映されるため、外部プラグインは通常のマップ設定 API で状態を確認できます。

Discord Webhook との連携により、公開状態の変化を通知することも可能です。
Webhook の設定については [設定ドキュメント](../../configuration/MAP_CONFIG.md) を参照してください。
