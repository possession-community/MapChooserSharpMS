# マップサイクル API

MCS のマップサイクルモジュールは、タイムリミット管理・マップ遷移・延長・クールダウンといったマップサイクル全般の制御を担います。
外部プラグインからは `IMapChooserSharpShared` 経由で `IMapCycleController` および `IMapCycleExtendController` にアクセスできます。

---

## IMapCycleController

マップサイクルモジュールの公開ファサードです。各サブシステムへのアクセスポイントとイベントリスナーの登録を提供します。

| メンバー | 型 | 説明 |
|---|---|---|
| `CurrentMapTimeLimitManager` | `ITimeLimitManager` | 現在のマップに適用されているタイムリミットマネージャ |
| `MapTransitionManager` | `IMapTransitionManager` | マップ遷移を管理するマネージャ |
| `MapCooldownQueryService` | `IMapCooldownQueryService` | クールダウンの照会サービス |
| `MapCooldownCommandService` | `IMapCooldownCommandService` | クールダウンの変更サービス |
| `CooldownStore` | `IMcsCooldownStore` | 実行時クールダウン状態の store (マップ/グループ名キー) |
| `InstallEventListener(IMapCycleEventListener)` | `void` | マップサイクルイベントのリスナーを登録する |
| `RemoveEventListener(IMapCycleEventListener)` | `void` | 登録済みリスナーを解除する |

---

## IMapCycleExtendController

マップ延長システムの公開ファサードです。延長には **3 つの独立した経路** があり、それぞれ異なる予算を消費します。

| 経路 | 予算 | 消費元 |
|---|---|---|
| マップ投票の "Extend Map" 選択肢 | `MaxExtends` (`ExtendsLeft`) | 投票で Extend が選ばれたとき |
| `!ext` コマンド (プレイヤー参加型) | `MaxExtCommandUses` (`ExtCommandUsesLeft`) | 閾値に達して延長が実行されたとき |
| 管理者パス (`TryExtendCurrentMap` / `!ve` 投票) | 予算消費なし | 管理者が直接延長、または延長投票が可決されたとき |

### プロパティ

| プロパティ | 型 | 説明 |
|---|---|---|
| `ExtendsLeft` | `int` | 現在のマップで残っている投票ベースの延長回数 |
| `ExtCommandUsesLeft` | `int` | 現在のマップで残っている `!ext` コマンドの延長回数 |
| `IsExtendVoteInProgress` | `bool` | 延長投票 (ネイティブ Yes/No 投票) が進行中かどうか |
| `IsExtEnabled` | `bool` | `!ext` コマンドが現在受付中かどうか |

### メソッド

| メソッド | 戻り値 | 説明 |
|---|---|---|
| `TryExtendCurrentMap(int?)` | `McsMapExtendResult` | 管理者/API 向けの延長エントリポイント。予算を消費しない。`overrideAmount` で延長量を上書き可能 (省略時は設定値) |
| `SetExtCommandUsesLeft(int)` | `void` | `!ext` コマンドの残り使用回数を直接設定する |
| `EnableExt()` | `void` | `!ext` コマンドの受付を有効化する |
| `DisableExt()` | `void` | `!ext` コマンドの受付を無効化する。既存の参加者はクリアされない |
| `StartExtendVote(IGameClient?, int?)` | `McsExtendVoteStartResult` | ネイティブ Yes/No 延長投票を開始する (管理者専用)。可決時は管理者パスで延長されるため予算を消費しない |
| `CancelExtendVote(IGameClient?)` | `bool` | 進行中の延長投票をキャンセルする。キャンセルできた場合は `true` |

---

## McsMapExtendResult

`TryExtendCurrentMap` の結果を表す列挙型です。

| 値 | 説明 |
|---|---|
| `Extended` | 延長に成功した |
| `NoExtendsLeft` | 投票ベースの延長回数 (`MaxExtends`) を使い切っている |
| `NoExtCommandUsesLeft` | `!ext` コマンドの延長回数 (`MaxExtCommandUses`) を使い切っている |
| `TimeLimitNotActive` | 延長対象のタイムリミットが存在しない (マップサイクルモードが none、またはリミットマネージャが未初期化) |

---

## McsExtendVoteStartResult

`StartExtendVote` の結果を表す列挙型です。

| 値 | 説明 |
|---|---|
| `Started` | 延長投票の開始に成功した |
| `AnotherVoteInProgress` | マップ投票や他のネイティブ投票が進行中、または次マップが既に確定している |
| `ExtendVoteAlreadyInProgress` | 延長投票が既に進行中である |
| `TimeLimitNotActive` | 延長対象のタイムリミットが存在しない |
| `FailedToInitiateNativeVote` | ネイティブ投票の開始に失敗した (NativeVoteManager が利用不可、または拒否された) |

---

## IMapTransitionManager

マップ遷移 (次マップの設定・確認・実行) を管理します。

### マップ遷移の典型的なフロー

1. `TrySetNextMap()` で次マップを確定する
2. `ChangeMapOnNextRoundEnd = true` を設定する (ラウンド終了時に自動遷移)
3. または `TransitionToNextMap(seconds)` を呼んで即座にカウントダウン遷移する

### プロパティ

| プロパティ | 型 | 説明 |
|---|---|---|
| `NextMap` | `IMapInformation?` | 次マップの情報 (ノミネーターのメタデータ含む)。マップ確定前は `null` |
| `CurrentMap` | `IMapInformation?` | 現在のマップの情報。設定が見つからない場合は `null` |
| `IsNextMapConfirmed` | `bool` | 次マップが確定しているかどうか |
| `ChangeMapOnNextRoundEnd` | `bool` | `true` の場合、ラウンド終了時に次マップへ遷移する。読み書き可能 |

### メソッド

| メソッド | 戻り値 | 説明 |
|---|---|---|
| `TrySetNextMap(IMapInformation)` | `bool` | ノミネーター情報等のメタデータ付きで次マップを設定する |
| `TrySetNextMap(IMapConfig)` | `bool` | 指定した `IMapConfig` を次マップとして設定する (メタデータなし) |
| `TrySetNextMap(string)` | `bool` | マップ名で検索して次マップを設定する |
| `TrySetNextMap(long)` | `Task<(bool, IWorkshopFetchResult)>` | Workshop ID で次マップを設定する。メモリ上の設定を検索し、見つからなければ Steam Workshop から HTTP で取得を試みる |
| `TryRemoveNextMap()` | `bool` | 次マップの確定を解除する |
| `TransitionToNextMap(float)` | `void` | 指定秒数後に次マップへ遷移する。次マップが未設定の場合は何もしない |

### IMapInformation

マップの設定データに加えて、誰がノミネートしたか等のコンテキスト情報を持つラッパーインターフェースです。`IMapTransitionManager.NextMap` や `CurrentMap` はこの型を返します。

| プロパティ | 型 | 説明 |
|---|---|---|
| `MapConfig` | `IMapConfig` | マップの設定データ |
| `NominatorSteamIds` | `IReadOnlyList<ulong>` | このマップをノミネートしたプレイヤーの SteamID 一覧 (ノミネーション順)。管理者設定・ランダム選択・API 経由の場合は空 |

`IMapInformation` の作成には `MapInformation.For(IMapConfig)` ビルダーを使用します:

```csharp
var info = MapInformation.For(mapConfig)
    .WithNominator(steamId)    // 単一ノミネーター
    .Build();

var info2 = MapInformation.For(mapConfig)
    .WithNominators(steamIdList)  // 複数ノミネーター
    .Build();

transition.TrySetNextMap(info);
```

---

### IWorkshopFetchResult

`TrySetNextMap(long)` の Workshop 解決結果です。

| プロパティ | 型 | 説明 |
|---|---|---|
| `ExistenceStatus` | `ExistenceStatus` | マップの取得状況 |
| `MapName` | `string?` | 解決されたマップ名 |
| `WorkshopId` | `long?` | Workshop ID |

### ExistenceStatus

| 値 | 説明 |
|---|---|
| `FoundInMemoryConfig` | メモリ上の既存設定から見つかった |
| `FoundInWorkshop` | Steam Workshop から取得できた |
| `NotAvailableInWorkshop` | Workshop 上で利用できない (非公開・削除済み等) |
| `FailedToFetchHttpError` | HTTP リクエストでエラーが発生した |
| `FailedToFetchUnknown` | 不明な理由で取得に失敗した |

---

## ITimeLimitManager

現在のマップサイクルモードに応じたタイムリミットの基底インターフェースです。
具体的な操作は `TimeLimitType` を確認したうえで、対応する派生インターフェースにキャストして使用します。

| メンバー | 型 | 説明 |
|---|---|---|
| `TimeLimitType` | `TimeLimitType` | 現在のタイムリミットの種類 |
| `IsLimitReached` | `bool` | リミットに到達しているかどうか |

### キャストパターン

```csharp
var manager = mapCycleController.CurrentMapTimeLimitManager;

switch (manager.TimeLimitType)
{
    case TimeLimitType.Time:
        var timeManager = (ITimeBasedTimeLimitManager)manager;
        // timeManager.TimeLeft, timeManager.Extend(TimeSpan), etc.
        break;
    case TimeLimitType.Round:
        var roundManager = (IRoundTimeLimitManager)manager;
        // roundManager.RoundsLeft, roundManager.Extend(int), etc.
        break;
}
```

### TimeLimitType

| 値 | 説明 |
|---|---|
| `Time` | 時間ベースのリミット (`mp_timelimit` 相当)。`ITimeBasedTimeLimitManager` にキャスト可能 |
| `Round` | ラウンドベースのリミット (`mp_maxrounds` 相当)。`IRoundTimeLimitManager` にキャスト可能 |

---

## ITimeBasedTimeLimitManager

`ITimeLimitManager` を継承し、時間ベースのリミット操作を提供します。

| メンバー | 型 | 説明 |
|---|---|---|
| `TimeLeft` | `TimeSpan` | 残り時間 |
| `Extend(TimeSpan)` | `bool` | 指定した時間だけリミットを延長する |
| `Set(TimeSpan)` | `bool` | リミットを指定した値に直接設定する |
| `GetFormattedTimeLeft(CultureInfo?)` | `string` | ローカライズされた残り時間の文字列を返す |

---

## IRoundTimeLimitManager

`ITimeLimitManager` を継承し、ラウンドベースのリミット操作を提供します。

| メンバー | 型 | 説明 |
|---|---|---|
| `RoundsLeft` | `int` | 残りラウンド数 |
| `Extend(int)` | `bool` | 指定したラウンド数だけリミットを延長する |
| `Set(int)` | `bool` | リミットを指定した値に直接設定する |
| `GetFormattedRoundsLeft(CultureInfo?)` | `string` | ローカライズされた残りラウンド数の文字列を返す |

---

## IMcsCooldownStore

実行時クールダウン状態の store で、マップ/グループの **名前** がキーです。状態はマップ config オブジェクトから独立しており、同一マップの DaySettings variant 間で共有され、config リロード後も維持されます。

2 つの層を公開します:

- **Effective (実効)** — 自サーバーの状態に、クールダウンスコープ (プラグイン config の `[Cooldown]` 参照) でマッチした他サーバーのレコードを合成したもの。フィールドごとに最も厳しい値が勝ちます。ピックアップ/ノミネーション判定や `!mapinfo` はこちらを参照します。
- **Own (自サーバー raw)** — 自サーバーの生の状態のみ。自サーバーキーで永続化される値です。デバッグ用 (`!mcsdebug config`)。

デフォルトスコープ (`Exact` + 空パターン) では両層は一致します。

全メンバーはゲームスレッドから呼び出してください。返される状態は読み取り専用のスナップショットです — 変更には `IMapCooldownCommandService` を使用してください。

| メソッド | 戻り値 | 説明 |
|---|---|---|
| `GetEffectiveMapState(string)` | `IMcsCooldownState` | マップのスコープ合成済み実効状態。未知のマップはゼロ値状態 |
| `GetEffectiveGroupState(string)` | `IMcsCooldownState` | グループのスコープ合成済み実効状態 |
| `GetOwnMapState(string)` | `IMcsCooldownState` | マップの自サーバー raw 状態 |
| `GetOwnGroupState(string)` | `IMcsCooldownState` | グループの自サーバー raw 状態 |

### IMcsCooldownState

| プロパティ | 型 | 説明 |
|---|---|---|
| `CurrentCooldown` | `int` | 現在の回数クールダウン。`int.MaxValue` = ノミネーション除外 |
| `TimedCooldownEndUtc` | `DateTime` | 時限クールダウンの終了 UTC。未設定は `DateTime.MinValue` |
| `LastPlayedAt` | `DateTime` | 最後にプレイされた UTC。未プレイは `DateTime.MinValue` |
| `UnplayedCount` | `int` | クールダウンが完全に切れてからプレイされたマップ数。プレイで 0 にリセット |
| `CurrentNominationCooldown` | `int` | 現在のノミネーションクールダウンカウント |
| `NominationTimedCooldownEndUtc` | `DateTime` | 時限ノミネーションクールダウンの終了 UTC |
| `IsCooldownActive` | `bool` | いずれかのクールダウン軸 (回数 or 時限) が有効な間 true |
| `IsNominationCooldownActive` | `bool` | いずれかのノミネーションクールダウン軸が有効な間 true |

---

## IMapCooldownQueryService

マップのクールダウン状態を **照会** するためのサービスです。クールダウンの値を変更する場合は `IMapCooldownCommandService` を使用してください。値は store の実効層から取得されます。

| メソッド | 戻り値 | 説明 |
|---|---|---|
| `QueryCurrentCooldowns(IMapConfig)` | `Task<IDetailedCooldownResult?>` | データベースからクールダウン情報を取得する |
| `GetCurrentCooldowns(IMapConfig)` | `IDetailedCooldownResult` | メモリ上のキャッシュからクールダウン情報を取得する |

### IDetailedCooldownResult

クールダウン状態の詳細を保持します。マップ自体のクールダウンと、所属グループのクールダウンの両方を含みます。

| プロパティ | 型 | 説明 |
|---|---|---|
| `HasCooldown` | `bool` | いずれかのクールダウンが適用中かどうか |
| `HighestCooldownCount` | `int` | マップ・グループ含め最も大きい回数クールダウン |
| `LongestTimedCooldown` | `DateTime` | マップ・グループ含め最も遅い時限クールダウンの終了時刻 (UTC) |
| `MapConfig` | `IMapConfig` | 対象マップの設定データ |
| `CooldownCount` | `int` | マップ本体の現在の回数クールダウン |
| `TimedCooldown` | `DateTime` | マップ本体の時限クールダウンの終了時刻 (UTC) |
| `GroupCooldowns` | `IReadOnlyDictionary<string, int>` | グループ名をキーとした各グループの回数クールダウン |
| `GroupTimedCooldowns` | `IReadOnlyDictionary<string, DateTime>` | グループ名をキーとした各グループの時限クールダウン終了時刻 |

---

## IMapCooldownCommandService

マップのクールダウンを **変更** するためのサービスです。全メソッドはデータベースへの永続化を試み、結果を `bool` で返します。

| メソッド | 戻り値 | 説明 |
|---|---|---|
| `SetCooldown(IMapConfig, int)` | `Task<bool>` | 回数ベースのクールダウンを設定する |
| `SetTimedCooldown(IMapConfig, TimeSpan)` | `Task<bool>` | 時限クールダウンを設定する |
| `ExcludeFromNomination(IMapConfig)` | `Task<bool>` | クールダウンを `int.MaxValue` に設定し、ノミネーションとランダム選択から事実上除外する |
| `ClearCooldown(IMapConfig)` | `Task<bool>` | 回数クールダウンをクリアする |
| `ClearTimedCooldown(IMapConfig)` | `Task<bool>` | 時限クールダウンをクリアする |

---

## IMapCycleEventListener

マップサイクルイベントのリスナーインターフェースです。`IMapCycleController.InstallEventListener()` で登録します。
全メソッドにはデフォルト実装があるため、必要なイベントだけをオーバーライドすれば十分です。

| メソッド | 戻り値 | 説明 |
|---|---|---|
| `OnExtCommandExecute(IExtCommandExecuteEventParams)` | `McsCancellableEvent` | `!ext` コマンド実行時。`Stop` を返すとキャンセル |
| `OnMapInfoCommandExecuted(IMapInfoCommandExecutedParams)` | `void` | `!mapinfo` コマンド実行後。追加情報の出力に使用できる |
| `OnExtendVoteStarted(IExtendVoteStartedEventParams)` | `void` | 延長投票の開始時 |
| `OnExtendVoteCancelled(IExtendVoteCancelledEventParams)` | `void` | 延長投票のキャンセル時 |
| `OnExtendVoteFinished(IExtendVoteFinishedEventParams)` | `void` | 延長投票の完了時 (可決・否決) |
| `OnNextMapConfirmed(INextMapConfirmedEventParams)` | `void` | 次マップが確定したとき |
| `OnNextMapRemoved(INextMapRemovedEventParams)` | `void` | 次マップの確定が解除されたとき |
| `OnMcsIntermission(IMcsIntermissionParams)` | `void` | インターミッション状態に入ったとき |
| `OnMapCooldownApply(IMapCooldownApplyEventParams)` | `void` | クールダウン適用直前。パラメータを編集してクールダウン値の変更やキャンセルが可能 |
| `OnTimeLimitReached(ITimeLimitReachedEventParams)` | `void` | タイムリミットまたはラウンドリミットに到達したとき |
| `OnVoteStartThresholdReached(IVoteStartThresholdReachedEventParams)` | `void` | 残り時間/ラウンドが投票開始閾値に達したとき |
