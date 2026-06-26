# Rock The Vote API

RTV (Rock The Vote) は、プレイヤーの投票によりマップ変更投票を発動させる仕組みです。
外部プラグインからは `IMapChooserSharpShared` 経由で `IMcsRtvController` にアクセスできます。

---

## RTV の動作フロー

1. プレイヤーが `!rtv` コマンドで RTV に参加する
2. 参加者数が通常閾値に達すると、`OnRtvConfirmed` イベントが発火し、マップ投票が開始される
3. 参加者数が即時変更閾値に達すると、投票を経ずに即座にマップ変更が行われる

### 2 段階閾値システム

RTV には 2 つの閾値があります:

- **通常閾値** (`mcs_rtv_threshold`): この比率に達するとマップ投票が開始される
- **即時変更閾値** (`mcs_rtv_immediate_change_threshold`): この比率に達するとマップ投票をスキップし、即座にマップ遷移する。`0` で無効

### ステータスのライフサイクル

```
Enabled ──(マップ開始時クールダウン)──> InCooldown ──(時間経過)──> Enabled
   │
   ├──(!rtv 参加者が閾値到達)──> TriggeredWaitingForVote ──(投票完了)──> Enabled (次マップ)
   │
   └──(管理者が無効化)──> Disabled ──(管理者が有効化)──> Enabled
```

投票中は `AnotherVoteOngoing` になり、投票が完了またはキャンセルされると元のステータスに戻ります。
マップ遷移待ちの場合は `TriggeredWaitingForMapTransition` になります。

---

## IMcsRtvController

RTV モジュールの公開ファサードです。マネージャ・サービスへのアクセスとイベントリスナーの登録を提供します。

| メンバー | 型 | 説明 |
|---|---|---|
| `RtvManager` | `IRtvManager` | RTV の状態を照会するマネージャ |
| `RtvService` | `IRtvService` | RTV の操作 (参加・離脱・投票発動) を行うサービス |
| `InstallEventListener(IRockTheVoteEventListener)` | `void` | RTV イベントのリスナーを登録する |
| `RemoveEventListener(IRockTheVoteEventListener)` | `void` | 登録済みリスナーを解除する |

---

## IRtvManager

RTV の現在の状態を読み取るためのマネージャです。状態の変更は `IRtvService` を通じて行います。

| メンバー | 型 | 説明 |
|---|---|---|
| `RtvStatus` | `RtvStatus` | 現在の RTV ステータス |
| `RtvCommandUnlockTime` | `TimeSpan` | RTV コマンドがアンロックされるエンジン時刻。残り秒数は `RtvCommandUnlockTime - ISharedSystem.GetModSharp().EngineTime()` で算出できる |
| `RtvCounts` | `int` | 現在の RTV 参加者数 |
| `RequiredCounts` | `int` | 閾値到達に必要な参加者数 |
| `RtvCompletionRatio` | `float` | 閾値に対する現在の達成率 (`0.0` -- `1.0`) |
| `RtvParticipants` | `IReadOnlySet<int>` | RTV に参加しているプレイヤーのユーザースロット集合 |

---

## IRtvService

RTV の操作を行うサービスです。プレイヤーの参加・離脱、投票の発動、有効/無効の切り替えを提供します。

| メソッド | 戻り値 | 説明 |
|---|---|---|
| `AddClientToRtv(IGameClient)` | `RtvExecutionResult` | プレイヤーを RTV に参加させる |
| `AddClientToRtv(int)` | `RtvExecutionResult` | スロット番号でプレイヤーを RTV に参加させる |
| `RemoveClientFromRtv(IGameClient, IGameClient?)` | `bool` | プレイヤーを RTV から離脱させる。`enforcer` は強制離脱を行った管理者 |
| `RemoveClientFromRtv(int)` | `bool` | スロット番号でプレイヤーを RTV から離脱させる |
| `InitiateRtvVote()` | `void` | RTV 投票を発動する (通常閾値到達時に内部で呼ばれる) |
| `InitiateForceRtvVote(IGameClient?)` | `void` | 管理者による強制 RTV を発動する。キャンセル可能な `OnForceRtv` イベントを経由する |
| `EnableRtvCommand(IGameClient?, bool)` | `void` | RTV コマンドを有効化する。`silently = true` でブロードキャストメッセージを抑制できる |
| `DisableRtvCommand(IGameClient?, bool)` | `void` | RTV コマンドを無効化する。`silently = true` でブロードキャストメッセージを抑制できる |

---

## RtvStatus

RTV の現在の状態を表す列挙型です。

| 値 | 説明 |
|---|---|
| `Enabled` | 有効であり、`!rtv` を受付中 |
| `Disabled` | 管理者によって無効化されている |
| `InCooldown` | クールダウン中 (マップ開始直後など) |
| `AnotherVoteOngoing` | マップ投票など他の投票が進行中 |
| `TriggeredWaitingForVote` | RTV が発動し、マップ投票の開始を待機中 |
| `TriggeredWaitingForMapTransition` | RTV が発動し、マップ遷移を待機中 |

---

## RtvExecutionResult

`AddClientToRtv` の結果を表す列挙型です。

| 値 | 説明 |
|---|---|
| `Success` | RTV への参加に成功した |
| `AlreadyVoted` | 既に RTV に参加済み |
| `CommandInCooldown` | RTV コマンドがクールダウン中 |
| `CommandDisabled` | RTV コマンドが管理者によって無効化されている |
| `AnotherVoteOngoing` | マップ投票など他の投票が進行中 |
| `NotAllowed` | 何らかの理由でプレイヤーの RTV が許可されていない |
| `DisallowedByExternalConsumer` | 外部プラグインの API コンシューマーによって RTV が拒否された |
| `TriggeredWaitingForVote` | RTV は既に発動済みで、マップ投票の開始を待機中 |
| `TriggeredWaitingForMapTransition` | RTV は既に発動済みで、マップ遷移を待機中 |

---

## IRockTheVoteEventListener

RTV イベントのリスナーインターフェースです。`IMcsRtvController.InstallEventListener()` で登録します。
全メソッドにはデフォルト実装があるため、必要なイベントだけをオーバーライドすれば十分です。

| メソッド | 戻り値 | 説明 |
|---|---|---|
| `OnClientRtvCast(IClientRtvCastParams)` | `McsCancellableEvent` | プレイヤーが RTV に参加しようとしたとき。`Stop` を返すとキャンセル |
| `OnClientRtvUnCast(IClientRtvUnCastParams)` | `McsCancellableEvent` | プレイヤーが RTV から離脱しようとしたとき。`Stop` を返すとキャンセル |
| `OnForceRtv(IForceRtvParam)` | `McsCancellableEvent` | 強制 RTV が発動されようとしたとき。`Stop` を返すとキャンセル |
| `OnRtvConfirmed(IRtvConfirmedParams)` | `void` | RTV が確定したとき (閾値到達または強制 RTV)。キャンセル不可。MapVoteController はこのイベントを購読してマップ投票を開始する |
