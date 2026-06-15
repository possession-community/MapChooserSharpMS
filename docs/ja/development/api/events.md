# イベントシステム

MCS は各モジュール (MapCycle / MapVote / Nomination / RockTheVote) ごとにイベントリスナーインターフェースを提供しています。
外部モジュールはこれらのインターフェースを実装してイベントを購読し、投票やノミネーションの挙動に介入できます。

---

## 基本的な仕組み

### リスナーの登録

各コントローラが `InstallEventListener` / `RemoveEventListener` メソッドを持っています。
`IMapChooserSharpShared` 経由で対応するコントローラを取得し、リスナーを登録します。

```csharp
var mcs = sharedSystem.GetSharpModuleManager()
    .GetRequiredSharpModuleInterface<IMapChooserSharpShared>(
        IMapChooserSharpShared.ModSharpModuleIdentity).Instance!;

mcs.MapCycleController.InstallEventListener(this);
mcs.McsMapVoteController.InstallEventListener(this);
mcs.McsRtvController.InstallEventListener(this);
mcs.McsNominationController.InstallEventListener(this);
```

登録は `OnAllModulesLoaded` のタイミングで行うのが一般的です。

### リスナー優先度

全リスナーインターフェースは `IEventListenerBase` を継承しており、`ListenerPriority` プロパティで実行順を制御します。

```csharp
public interface IEventListenerBase
{
    int ListenerPriority { get; }
}
```

- **値が大きいほど先に実行される** (降順)
- 同じ優先度のリスナー間では実行順序は保証されない

### イベントの種類

MCS のイベントは戻り値の型で 3 つに分類されます。

| 分類 | 戻り値 | 説明 |
|---|---|---|
| キャンセル可能イベント | `bool` | `true` を返すと後続のリスナーを含めてイベントの対象操作がキャンセルされる |
| 通知イベント | `void` | 発火のみ。操作をブロックすることはできない |
| オーバーライドイベント | `List<IMapConfig>` | 空でないリストを返すと、デフォルトの処理結果を置き換える |

デフォルト実装 (何もしない) がインターフェース側に用意されているため、必要なメソッドだけをオーバーライドすれば十分です。

---

## 基底インターフェース

### IEventBaseParams

全イベントパラメータの基底インターフェースです。

| メンバー | 型 | 説明 |
|---|---|---|
| `ModulePrefix(CultureInfo?)` | `string` | イベントを発行したモジュールのプレフィックス文字列 |

### ICommandEventBaseParams : IEventBaseParams

コマンド実行に起因するイベントの基底です。

| プロパティ | 型 | 説明 |
|---|---|---|
| `Client` | `IGameClient?` | コマンドを実行したクライアント。コンソール実行の場合は `null` |
| `Command` | `ref StringCommand` | 実行中のコマンド情報 |

### IEnforceableEvent

管理者による強制操作を伴うイベントが実装するインターフェースです。

| プロパティ | 型 | 説明 |
|---|---|---|
| `EnforcedByAdmin` | `bool` | 管理者による操作かどうか |
| `Enforcer` | `IGameClient?` | 操作を行った管理者。`EnforcedByAdmin` が `true` で `Enforcer` が `null` の場合はコンソールから実行されたことを意味する |

### IMcsNominationEventBaseParams

ノミネーション系イベントの共通パラメータです。

| プロパティ | 型 | 説明 |
|---|---|---|
| `Client` | `IGameClient?` | イベントを発生させたクライアント。コンソールの場合は `null` |
| `NominationData` | `IMcsNominationData` | ノミネーションデータ (後述) |

---

## 関連する型

### IMcsNominationData

ノミネーションの情報を保持するインターフェースです。

| プロパティ | 型 | 説明 |
|---|---|---|
| `MapConfig` | `IMapConfig` | ノミネートされたマップの設定 |
| `NominationParticipants` | `IReadOnlySet<int>` | ノミネーションに参加しているプレイヤーの UserID 一覧 |
| `IsForceNominated` | `bool` | 管理者によって強制ノミネートされたかどうか |

### TimeLimitType

マップサイクルにおける制限の種別を表す列挙型です。

| 値 | 説明 |
|---|---|
| `Time` | 時間制限 (mp_timelimit 相当) |
| `Round` | ラウンド制限 (mp_maxrounds 相当) |

### UnNominateReason

ノミネーション解除の理由を表す列挙型です。

| 値 | 説明 |
|---|---|
| `Normally` | プレイヤーが自発的にノミネーションを解除した (別マップへの変更やコマンド実行など) |
| `PlayerDisconnect` | プレイヤーの切断により自動的に解除された |

### IMapVoteInformation

投票結果の情報を保持するインターフェースです。

| プロパティ | 型 | 説明 |
|---|---|---|
| `CurrentState` | `McsMapVoteState` | 投票の現在の状態 |
| `VoteOptions` | `IReadOnlyCollection<IMapVoteOption>` | 投票選択肢の一覧 |
| `Winner` | `IMapVoteOption?` | 勝者の選択肢。未確定の場合は `null` |

---

## IMapCycleEventListener

マップサイクルに関連するイベントのリスナーです。`IMapChooserSharpShared.MapCycleController.InstallEventListener()` で登録します。

### OnExtCommandExecute

`!ext` コマンドが実行されたときに発火します。

- **戻り値**: `bool` (キャンセル可能 -- `true` でコマンド実行をキャンセル)
- **パラメータ**: `IExtCommandExecuteEventParams` (継承: `ICommandEventBaseParams`)

| プロパティ | 型 | 説明 |
|---|---|---|
| `Client` | `IGameClient?` | コマンド実行者 |
| `Command` | `ref StringCommand` | コマンド情報 |
| `CurrentRequiredVotes` | `int` | 延長発動に必要な投票数 |
| `CurrentExtVotes` | `int` | 現在の延長投票数 (このイベント通過後にインクリメントされる) |

### OnMapInfoCommandExecuted

`!mapinfo` コマンドの処理が完了したときに発火します。このイベントをリッスンして追加情報をプレイヤーに表示できます。

- **戻り値**: `void` (通知イベント)
- **パラメータ**: `IMapInfoCommandExecutedParams` (継承: `ICommandEventBaseParams`)

| プロパティ | 型 | 説明 |
|---|---|---|
| `Client` | `IGameClient?` | コマンド実行者 |
| `Command` | `ref StringCommand` | コマンド情報 |
| `MapConfig` | `IMapConfig` | コマンドで検索されたマップの設定 |

### OnExtendVoteStarted

延長投票 (`!voteextend`) が開始されたときに発火します。

- **戻り値**: `void` (通知イベント)
- **パラメータ**: `IExtendVoteStartedEventParams`

| プロパティ | 型 | 説明 |
|---|---|---|
| `CurrentMap` | `IMapConfig?` | 延長対象のマップ (現在のマップ)。MCS に設定がないマップの場合は `null` |
| `Initiator` | `IGameClient?` | 投票を開始したクライアント。コンソール/サーバーの場合は `null` |
| `VoteDuration` | `float` | 投票の持続時間 (秒) |

### OnExtendVoteCancelled

延長投票がキャンセルされたときに発火します。

- **戻り値**: `void` (通知イベント)
- **パラメータ**: `IExtendVoteCancelledEventParams`

| プロパティ | 型 | 説明 |
|---|---|---|
| `CurrentMap` | `IMapConfig?` | 延長対象だったマップ。MCS に設定がないマップの場合は `null` |
| `CancelledBy` | `IGameClient?` | キャンセルしたクライアント。コンソール/外部キャンセルの場合は `null` |

### OnExtendVoteFinished

延長投票が完了 (可決または否決) したときに発火します。キャンセル時にはこのイベントは発火しません (代わりに `OnExtendVoteCancelled` が発火します)。

- **戻り値**: `void` (通知イベント)
- **パラメータ**: `IExtendVoteFinishedEventParams`

| プロパティ | 型 | 説明 |
|---|---|---|
| `CurrentMap` | `IMapConfig?` | 延長対象のマップ。MCS に設定がないマップの場合は `null` |
| `Passed` | `bool` | 投票が可決された場合 `true` |

### OnNextMapConfirmed

次のマップが確定または変更されたときに発火します。

- **戻り値**: `void` (通知イベント)
- **パラメータ**: `INextMapConfirmedEventParams`

| プロパティ | 型 | 説明 |
|---|---|---|
| `NextMap` | `IMapConfig` | 確定した次のマップ |
| `OldNextMap` | `IMapConfig?` | 変更前の次のマップ。初回確定時は `null` |

### OnNextMapRemoved

確定済みの次のマップが解除されたときに発火します。

- **戻り値**: `void` (通知イベント)
- **パラメータ**: `INextMapRemovedEventParams`

| プロパティ | 型 | 説明 |
|---|---|---|
| `PreviousNextMap` | `IMapConfig` | 解除された次のマップ |

### OnMcsIntermission

インターミッション (マップ遷移前の待機状態) に入ったときに発火します。

- **戻り値**: `void` (通知イベント)
- **パラメータ**: `IMcsIntermissionParams`

| プロパティ | 型 | 説明 |
|---|---|---|
| `NextMap` | `IMapConfig` | 遷移先のマップ |

### OnMapCooldownApply (編集可能イベント)

マップのクールダウンが適用される直前に発火します。このイベントは特殊で、パラメータの値を書き換えることでクールダウンの挙動をカスタマイズできます。

- **戻り値**: `void` (通知イベント -- ただしパラメータ書き換えにより実質的にキャンセル・変更が可能)
- **パラメータ**: `IMapCooldownApplyEventParams`

| プロパティ | 型 | アクセス | 説明 |
|---|---|---|---|
| `AppliesTo` | `IMapConfig` | get | クールダウンの適用対象マップ |
| `Cooldown` | `int` | **get/set** | 適用される回数ベースクールダウン。初期値は `CooldownConfig.ConfigCooldown` |
| `TimedCooldownDuration` | `TimeSpan` | **get/set** | 適用される時限クールダウン。初期値は `CooldownConfig.TimedCooldown` |
| `IsCancelled` | `bool` | **get/set** | `true` にするとクールダウンの適用自体をスキップする |

使用例:

```csharp
public void OnMapCooldownApply(IMapCooldownApplyEventParams p)
{
    // 特定マップのクールダウンを倍にする
    if (p.AppliesTo.MapName == "de_dust2")
    {
        p.Cooldown *= 2;
        p.TimedCooldownDuration = p.TimedCooldownDuration * 2;
    }

    // 別のマップのクールダウンを完全にスキップする
    if (p.AppliesTo.MapName == "de_mirage")
    {
        p.IsCancelled = true;
    }
}
```

### OnTimeLimitReached

時間制限またはラウンド制限に到達したときに発火します。

- **戻り値**: `void` (通知イベント)
- **パラメータ**: `ITimeLimitReachedEventParams`

| プロパティ | 型 | 説明 |
|---|---|---|
| `LimitType` | `TimeLimitType` | 到達した制限の種類 (`Time` または `Round`) |

### OnVoteStartThresholdReached

残り時間またはラウンド数がマップ投票開始の閾値に到達したときに発火します。

- **戻り値**: `void` (通知イベント)
- **パラメータ**: `IVoteStartThresholdReachedEventParams`

| プロパティ | 型 | 説明 |
|---|---|---|
| `LimitType` | `TimeLimitType` | 閾値に到達した制限の種類 (`Time` または `Round`) |

---

## IMapVoteEventListener

マップ投票に関連するイベントのリスナーです。`IMapChooserSharpShared.McsMapVoteController.InstallEventListener()` で登録します。

### OnMapVoteStart

マップ投票が開始されるときに発火します。

- **戻り値**: `bool` (キャンセル可能 -- `true` で投票開始をキャンセル)
- **パラメータ**: `IMapVoteStartParams`

| プロパティ | 型 | 説明 |
|---|---|---|
| `MapsToVote` | `IReadOnlyList<IMapConfig>` | 投票に表示されるマップのリスト |
| `VoteParticipants` | `IReadOnlyList<PlayerSlot>` | 投票の参加者 (プレイヤースロット) |

### OnRandomMapPick (オーバーライドイベント)

投票候補のマップをランダム選択する際に発火します。このイベントは特殊な戻り値パターンを持ちます。

- **戻り値**: `List<IMapConfig>` (空リストを返すとデフォルトのランダム選択が使われる。空でないリストを返すとその内容で候補が置き換わる)
- **パラメータ**: `IMapVoteRandomMapPickParams`

| プロパティ | 型 | 説明 |
|---|---|---|
| `MinimumMapCounts` | `int` | 投票に必要な最小マップ数 |
| `MapConfigs` | `IReadOnlyDictionary<string, IMapConfig>` | 利用可能な全マップ設定。独自の選択ロジックを実装する際に参照する |

使用例:

```csharp
public List<IMapConfig> OnRandomMapPick(IMapVoteRandomMapPickParams p)
{
    // 独自の選択ロジックでマップ候補を返す
    var selected = p.MapConfigs.Values
        .Where(m => !m.IsDisabled && m.MapName.StartsWith("de_"))
        .Take(p.MinimumMapCounts)
        .ToList();

    // 空リストを返すと MCS のデフォルト選択にフォールバックする
    return selected.Count >= p.MinimumMapCounts ? selected : [];
}
```

### OnMapVoteFinished

マップ投票が完了したときに発火します。個別の結果イベント (`OnMapConfirmed` / `OnMapExtended` / `OnMapNotChanged`) よりも先に呼ばれます。

- **戻り値**: `void` (通知イベント)
- **パラメータ**: `IMapVoteFinishedEventParams`

| プロパティ | 型 | 説明 |
|---|---|---|
| `VoteInformation` | `IMapVoteInformation` | 投票結果の詳細情報 (状態・選択肢・勝者) |
| `IsRtvVote` | `bool` | RTV により発生した投票かどうか |

### OnMapVoteCancelled

マップ投票がキャンセルされたときに発火します。

- **戻り値**: `void` (通知イベント)
- **パラメータ**: `IMapVoteCancelledParams`

| プロパティ | 型 | 説明 |
|---|---|---|
| `CancelledBy` | `IGameClient?` | 投票をキャンセルしたクライアント。システム/コンソールの場合は `null` |

### OnMapExtended

投票結果が「延長 (Extend)」になったときに発火します。

- **戻り値**: `void` (通知イベント)
- **パラメータ**: `IMapVoteExtendParams`

| プロパティ | 型 | 説明 |
|---|---|---|
| `ExtendTime` | `int` | 延長される時間 (分) またはラウンド数。`TimeLimitType` に依存する |
| `TimeLimitType` | `TimeLimitType` | 延長対象の種別 (`Time` = 分、`Round` = ラウンド) |

### OnMapNotChanged

RTV 投票で「マップを変更しない (Don't Change)」が選ばれたときに発火します。

- **戻り値**: `void` (通知イベント)
- **パラメータ**: `IMapVoteNotChangedParams` (追加プロパティなし)

### OnMapConfirmed

投票により次のマップが確定したときに発火します。

- **戻り値**: `void` (通知イベント)
- **パラメータ**: `IMapVoteMapConfirmedEventParams`

| プロパティ | 型 | 説明 |
|---|---|---|
| `ConfirmedMap` | `IMapConfig` | 確定したマップの設定 |
| `IsRtvVote` | `bool` | RTV による投票だったかどうか |

---

## INominationEventListener

ノミネーションに関連するイベントのリスナーです。`IMapChooserSharpShared.McsNominationController.InstallEventListener()` で登録します。

### OnNominationCheckPassed (ゲートイベント)

ノミネーションの内部バリデーション (人数制限・曜日・時間帯・クールダウン等) を全て通過した直後に発火します。`OnNomination` / `OnAdminNomination` よりも先に呼ばれます。

外部モジュールが独自のバリデーションロジックを追加するためのゲートとして設計されています。

- **戻り値**: `bool` (`true` を返すとノミネーションチェックを失敗させる = ノミネーションを阻止する)
- **パラメータ**: `INominationCheckPassedEventParams`

| プロパティ | 型 | 説明 |
|---|---|---|
| `Client` | `IGameClient?` | ノミネーションを試みたクライアント。コンソールの場合は `null` |

使用例:

```csharp
public bool OnNominationCheckPassed(INominationCheckPassedEventParams p)
{
    // VIP 以外のプレイヤーを特定条件でブロック
    if (p.Client is not null && !IsVipPlayer(p.Client))
    {
        p.Client.PrintToChat("VIP 専用のノミネーション期間です");
        return true; // ノミネーションをブロック
    }
    return false; // 通過
}
```

### OnNomination

プレイヤーによる通常のノミネーションが行われるときに発火します。

- **戻り値**: `bool` (キャンセル可能 -- `true` でノミネーションをキャンセル)
- **パラメータ**: `INominationParams` (継承: `IEventBaseParams`, `IMcsNominationEventBaseParams`)

| プロパティ | 型 | 説明 |
|---|---|---|
| `Client` | `IGameClient?` | ノミネートしたクライアント |
| `NominationData` | `IMcsNominationData` | ノミネーションデータ |

### OnAdminNomination

管理者によるノミネーション (`!nominate_addmap` 等) が行われるときに発火します。

- **戻り値**: `bool` (キャンセル可能 -- `true` でノミネーションをキャンセル)
- **パラメータ**: `IAdminNominationParams` (継承: `IEventBaseParams`, `IMcsNominationEventBaseParams`)

| プロパティ | 型 | 説明 |
|---|---|---|
| `Client` | `IGameClient?` | ノミネートした管理者 |
| `NominationData` | `IMcsNominationData` | ノミネーションデータ |

### OnNominationChanged

ノミネーション内容が変更されたときに発火します。

- **戻り値**: `void` (通知イベント)
- **パラメータ**: `INominationChangeParams` (継承: `IEventBaseParams`, `IMcsNominationEventBaseParams`, `IEnforceableEvent`)

| プロパティ | 型 | 説明 |
|---|---|---|
| `Client` | `IGameClient?` | 変更を行ったクライアント |
| `NominationData` | `IMcsNominationData` | 変更後のノミネーションデータ |
| `EnforcedByAdmin` | `bool` | 管理者による変更かどうか |
| `Enforcer` | `IGameClient?` | 変更を強制した管理者 |

### OnNominationRemoved

ノミネーションが完全に削除されたときに発火します。

- **戻り値**: `void` (通知イベント)
- **パラメータ**: `INominationRemovedParams` (継承: `IEnforceableEvent`, `INominationParams`)

| プロパティ | 型 | 説明 |
|---|---|---|
| `Client` | `IGameClient?` | 削除に関連するクライアント |
| `NominationData` | `IMcsNominationData` | 削除されたノミネーションのデータ |
| `EnforcedByAdmin` | `bool` | 管理者による削除かどうか |
| `Enforcer` | `IGameClient?` | 削除を強制した管理者 |

### OnUnNominate

個々のクライアントがノミネーション参加者リストから除外されたときに発火します。自発的な解除 (別マップへの切り替え等) やプレイヤー切断時のクリーンアップが対象です。

このイベントはクライアント単位で発火します。最後の参加者が離脱して非管理者ノミネーション自体が削除される場合は、このイベントの後に追加で `OnNominationRemoved` も発火します。

- **戻り値**: `void` (通知イベント)
- **パラメータ**: `IUnNominateParams` (継承: `IEventBaseParams`, `IMcsNominationEventBaseParams`)

| プロパティ | 型 | 説明 |
|---|---|---|
| `Client` | `IGameClient?` | 関連するクライアント。切断済みの場合は `null` の可能性がある |
| `NominationData` | `IMcsNominationData` | 対象のノミネーションデータ |
| `Slot` | `int` | 除外されたクライアントのプレイヤースロット。`Client` が `null` でも常に有効 |
| `Reason` | `UnNominateReason` | 解除の理由 (`Normally` または `PlayerDisconnect`) |

---

## IRockTheVoteEventListener

RTV (Rock The Vote) に関連するイベントのリスナーです。`IMapChooserSharpShared.McsRtvController.InstallEventListener()` で登録します。

### OnClientRtvCast

プレイヤーが RTV 投票を行ったときに発火します。

- **戻り値**: `bool` (キャンセル可能 -- `true` で RTV 投票をキャンセル)
- **パラメータ**: `IClientRtvCastParams`

| プロパティ | 型 | 説明 |
|---|---|---|
| `IsRtvTrigger` | `bool` | この投票で RTV 閾値に到達する場合 `true` |
| `Client` | `IGameClient` | RTV を投じたクライアント |

### OnClientRtvUnCast

プレイヤーが RTV 投票を取り消したときに発火します。

- **戻り値**: `bool` (キャンセル可能 -- `true` で取り消しをキャンセル = RTV を維持)
- **パラメータ**: `IClientRtvUnCastParams` (継承: `IEnforceableEvent`)

| プロパティ | 型 | 説明 |
|---|---|---|
| `Client` | `IGameClient` | RTV を取り消したクライアント |
| `EnforcedByAdmin` | `bool` | 管理者による強制取り消しかどうか |
| `Enforcer` | `IGameClient?` | 強制した管理者 |

### OnForceRtv

管理者が強制 RTV を実行したときに発火します。

- **戻り値**: `bool` (キャンセル可能 -- `true` で強制 RTV をキャンセル)
- **パラメータ**: `IForceRtvParam` (継承: `IEnforceableEvent`)

| プロパティ | 型 | 説明 |
|---|---|---|
| `Client` | `IGameClient?` | 強制 RTV を実行したクライアント。コンソールの場合は `null` |
| `IsSilent` | `bool` | サイレント実行かどうか |
| `EnforcedByAdmin` | `bool` | 管理者による操作かどうか |
| `Enforcer` | `IGameClient?` | 操作を行った管理者 |

### OnRtvConfirmed

RTV が確定した (閾値到達または強制 RTV) ときに発火します。MapVoteController がこのイベントをリッスンしてマップ投票を開始します。

- **戻り値**: `void` (通知イベント -- キャンセル不可)
- **パラメータ**: `IRtvConfirmedParams` (継承: `IEnforceableEvent`)

| プロパティ | 型 | 説明 |
|---|---|---|
| `Client` | `IGameClient?` | RTV 閾値を超えたクライアント、または強制 RTV を実行した管理者。コンソールの場合は `null` |
| `IsForced` | `bool` | 強制 RTV コマンドによる確定かどうか |
| `EnforcedByAdmin` | `bool` | 管理者操作かどうか |
| `Enforcer` | `IGameClient?` | 操作を行った管理者 |

---

## 特殊イベントパターンのまとめ

MCS のイベントには、標準的なキャンセル可能/通知以外にいくつかの特殊パターンがあります。

### 編集可能イベント (OnMapCooldownApply)

パラメータオブジェクトのプロパティに setter が公開されており、リスナー側で値を書き換えられます。`IsCancelled` を `true` にすればクールダウン適用自体をスキップでき、`Cooldown` や `TimedCooldownDuration` を変更すれば適用される値をカスタマイズできます。

### オーバーライドイベント (OnRandomMapPick)

戻り値のリストが空かどうかで挙動が分岐します。空リストを返すとデフォルトのランダム選択が実行され、空でないリストを返すとその内容が投票候補として使われます。独自のマップ選択アルゴリズムを実装するための拡張ポイントです。

### ゲートイベント (OnNominationCheckPassed)

MCS 内部のバリデーションを全て通過した後に呼ばれ、外部モジュールが追加のバリデーションを挟むためのフックです。`true` を返すとノミネーションが拒否されます。`OnNomination` や `OnAdminNomination` とは別のタイミングで発火する点に注意してください。

---

## 完全な実装例

以下は 4 つのリスナーインターフェースを全て実装するモジュールの例です。

```csharp
using System.Collections.Generic;
using MapChooserSharpMS.Shared;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.Events.MapCycle.Params;
using MapChooserSharpMS.Shared.Events.MapVote;
using MapChooserSharpMS.Shared.Events.MapVote.Params;
using MapChooserSharpMS.Shared.Events.Nomination;
using MapChooserSharpMS.Shared.Events.Nomination.Params;
using MapChooserSharpMS.Shared.Events.RockTheVote;
using MapChooserSharpMS.Shared.Events.RockTheVote.Params;
using MapChooserSharpMS.Shared.MapConfig;
using Microsoft.Extensions.Logging;
using Sharp.Shared;

public class MyMcsListener : IModSharpModule,
    IMapCycleEventListener,
    IMapVoteEventListener,
    INominationEventListener,
    IRockTheVoteEventListener
{
    public string DisplayName => "My MCS Listener";
    public string DisplayAuthor => "author";
    public int ListenerVersion => 1;

    // 高い値ほど先に実行される
    public int ListenerPriority => 100;

    private readonly ISharedSystem _sharedSystem;
    private readonly ILogger _logger;

    public MyMcsListener(ISharedSystem sharedSystem, /* ... */)
    {
        _sharedSystem = sharedSystem;
        _logger = sharedSystem.GetLoggerFactory().CreateLogger(DisplayName);
    }

    public bool Init() => true;
    public void PostInit() { }

    public void OnAllModulesLoaded()
    {
        var mcs = _sharedSystem.GetSharpModuleManager()
            .GetRequiredSharpModuleInterface<IMapChooserSharpShared>(
                IMapChooserSharpShared.ModSharpModuleIdentity).Instance!;

        // 4 つのコントローラにリスナーを登録
        mcs.MapCycleController.InstallEventListener(this);
        mcs.McsMapVoteController.InstallEventListener(this);
        mcs.McsNominationController.InstallEventListener(this);
        mcs.McsRtvController.InstallEventListener(this);

        _logger.LogInformation("MCS イベントリスナーを登録しました");
    }

    public void Shutdown() { }

    // ── キャンセル可能イベントの例 ──

    public bool OnMapVoteStart(IMapVoteStartParams p)
    {
        _logger.LogInformation("マップ投票開始: 候補数={Count}", p.MapsToVote.Count);
        return false; // キャンセルしない
    }

    // ── オーバーライドイベントの例 ──

    public List<IMapConfig> OnRandomMapPick(IMapVoteRandomMapPickParams p)
    {
        // 空リストを返すとデフォルトの選択が使われる
        return [];
    }

    // ── 編集可能イベントの例 ──

    public void OnMapCooldownApply(IMapCooldownApplyEventParams p)
    {
        // 特定マップのクールダウンを動的に変更
        if (p.AppliesTo.MapName == "de_dust2")
        {
            p.Cooldown = 5;
        }
    }

    // ── ゲートイベントの例 ──

    public bool OnNominationCheckPassed(INominationCheckPassedEventParams p)
    {
        // 追加のバリデーション (true で拒否)
        return false;
    }

    // ── 通知イベントの例 ──

    public void OnNextMapConfirmed(INextMapConfirmedEventParams p)
    {
        _logger.LogInformation("次のマップが確定: {Map}", p.NextMap.MapName);
    }

    public void OnRtvConfirmed(IRtvConfirmedParams p)
    {
        _logger.LogInformation("RTV 確定: Forced={Forced}", p.IsForced);
    }
}
```

必要なメソッドだけをオーバーライドすれば十分です。インターフェースにはデフォルト実装が用意されているため、全メソッドを実装する必要はありません。
