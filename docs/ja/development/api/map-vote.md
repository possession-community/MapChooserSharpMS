# マップ投票 API

MCS のマップ投票を制御するための公開インターフェースを提供します。
投票の開始・キャンセル、クライアントの投票操作、進行中の投票状態の読み取りなどを行えます。

`IMapChooserSharpShared.McsMapVoteController` からアクセスします。

---

## IMcsMapVoteController

マップ投票モジュールの公開ファサードです。投票状態の参照、投票制御サービスへのアクセス、イベントリスナーの登録、および勝利閾値のカスタマイズを提供します。

| メソッド / プロパティ | 型 | 説明 |
|---|---|---|
| `VoteState` | `IMcsReadOnlyVoteState` | 現在の投票状態の読み取り専用ビュー |
| `MapVoteControllingService` | `IMapVoteControllingService` | 投票の開始・キャンセル・強制リセットを行うサービス |
| `InstallEventListener(IMapVoteEventListener)` | `void` | マップ投票イベントのリスナーを登録する |
| `RemoveEventListener(IMapVoteEventListener)` | `void` | リスナーの登録を解除する |
| `CustomWinnerThresholdProvider` | `Func<float>?` | 外部プラグインによる勝利閾値の上書き (後述) |

### CustomWinnerThresholdProvider

初回投票の勝利判定に使用する得票率の閾値を外部から差し替えるためのプロパティです。投票が開始されるたびにこのデリゲートが呼び出され、戻り値 (`0.0` -- `1.0`) が閾値として使用されます。

- `null` を設定すると、デフォルトの ConVar ベースの閾値に戻ります
- **決選投票 (Runoff) では無視されます** -- 決選投票は閾値に関係なく最多得票で決定されます

```csharp
// 例: 常に 60% の得票率を要求する
controller.CustomWinnerThresholdProvider = () => 0.6f;

// 例: デフォルトに戻す
controller.CustomWinnerThresholdProvider = null;
```

---

## IMapVoteControllingService

投票の開始・キャンセル・強制リセットを行うサービスです。

| メソッド | 戻り値 | 説明 |
|---|---|---|
| `InitiateVote(bool)` | `McsMapVoteState` | マップ投票を開始する。成功時は `InitializeAccepted` を返し、それ以外は現在の投票状態を返す |
| `CancelVote(IGameClient?)` | `McsMapVoteState` | 進行中の投票をキャンセルする。成功時は `Cancelling` を返す |
| `ForceResetVote()` | `bool` | 投票状態を強制的にリセットする |

### InitiateVote の isActivatedByRtv パラメータ

- `true` (RTV 起動): 最初の選択肢として「マップを変更しない (Don't Change)」が追加される
- `false` (通常起動): 最初の選択肢として「現在のマップを延長する (Extend)」が追加される

この選択肢の違いにより、RTV 投票では "現状維持" が、通常投票では "延長" が特別な選択肢として提示されます。

---

## IClientVoteHandlingService

個々のプレイヤーの投票操作を処理するサービスです。

| メソッド | 戻り値 | 説明 |
|---|---|---|
| `TryAddClientVote(IGameClient, IMapVoteOption)` | `bool` | プレイヤーの投票を追加する。成功時に `true` を返す |
| `RemoveClientVote(IGameClient)` | `void` | プレイヤーの投票を削除する |
| `RemoveClientVote(PlayerSlot)` | `void` | スロット番号でプレイヤーの投票を削除する。切断時など `IGameClient` が利用できない場合に使用する |
| `ClientReVote(IGameClient)` | `void` | プレイヤーの投票を削除し、投票メニューを再表示する。ネイティブ投票 UI の場合は無視される |

---

## IMcsReadOnlyVoteState

投票状態の読み取り専用ビューです。投票の開始可否を判定するだけの用途であれば、フルコントローラーではなくこの軽量インターフェースに依存することを推奨します。

| メンバー | 型 | 説明 |
|---|---|---|
| `CurrentVoteState` | `McsMapVoteState?` | 現在の投票状態。投票が未開始の場合は `null` |
| `IsVotingPeriod()` | `bool` | 投票が進行中かどうか。投票中・初期化中・決選投票中・確定処理中など、「別の投票を安全に開始できない」全ての状態で `true` を返す |

---

## IVoteControllingManager

現在の投票セッション情報を読み取るマネージャーです。

| プロパティ | 型 | 説明 |
|---|---|---|
| `CurrentVote` | `IMapVoteInformation?` | 現在アクティブな投票セッションの情報。投票が行われていない場合は `null` |

`IVoteControllingManager` は投票ごとに新しいインスタンスが生成される `IMapVoteInformation` を参照します。投票セッションは使い捨てであり、シングルトンのリセットではありません。

---

## IMapVoteInformation

個々の投票セッションの情報を表します。

| プロパティ | 型 | 説明 |
|---|---|---|
| `CurrentState` | `McsMapVoteState` | この投票セッションの現在の状態 |
| `VoteOptions` | `IReadOnlyCollection<IMapVoteOption>` | 投票の選択肢一覧 |
| `Winner` | `IMapVoteOption?` | 勝者の選択肢。投票が確定するまでは `null` |

---

## IMapVoteOption

投票の個別の選択肢を表します。

| プロパティ | 型 | 説明 |
|---|---|---|
| `MapName` | `string` | 選択肢の表示名 |
| `MapConfig` | `IMapConfig?` | 選択肢に対応するマップ設定 |
| `VoteParticipants` | `IReadOnlyCollection<PlayerSlot>` | この選択肢に投票しているプレイヤーのスロット一覧 |

### MapConfig が null になるケース

`MapConfig` は通常のマップ選択肢では常にマップ設定を返しますが、以下の特殊な選択肢では `null` になります:

- **Extend (延長)**: 現在のマップの延長を選ぶ選択肢。通常投票 (`isActivatedByRtv = false`) で追加される
- **Don't Change (変更しない)**: マップを変更しない選択肢。RTV 投票 (`isActivatedByRtv = true`) で追加される

特殊選択肢の判定には `MapConfig == null` のチェックを使用してください。`MapName` にはローカライズされた翻訳キーの表示文字列が入ります。

---

## McsMapVoteState

マップ投票の状態遷移を表す列挙型です。

| 値 | 説明 |
|---|---|
| `NoActiveVote` | アクティブな投票がなく、次のマップも未確定 |
| `Cancelling` | 投票のキャンセル処理中 |
| `InitializeAccepted` | 投票の初期化が受理され、開始準備中 |
| `Initializing` | 投票を初期化中 (候補マップの選定、メニュー構築等) |
| `Voting` | 投票進行中 |
| `RunoffVoting` | 決選投票進行中。初回投票で勝利閾値に達する選択肢がなかった場合に上位候補で実施される |
| `Finalizing` | 投票結果の確定処理中 (クールダウン付与、次マップ設定等) |
| `NextMapConfirmed` | 次のマップが確定済み。この状態では新たな投票は開始できない |
| `NotEnoughMapsToStartVote` | 有効なマップ設定が不足しており投票を開始できない |

### 投票のライフサイクル

投票は以下の順に状態遷移します:

```
NoActiveVote
  -> InitializeAccepted     (InitiateVote 受理)
    -> Initializing          (候補マップ構築、メニュー準備)
      -> Voting              (プレイヤーが投票を行う)
        -> RunoffVoting      (閾値未達の場合、任意)
          -> Finalizing      (結果確定処理)
            -> NextMapConfirmed  (次マップ決定)
```

キャンセルされた場合は `Cancelling` を経由して `NoActiveVote` に戻ります。候補マップが不足している場合は `NotEnoughMapsToStartVote` が返されます。

---

## イベントリスナー (IMapVoteEventListener)

`IMcsMapVoteController.InstallEventListener` で登録するリスナーインターフェースです。全メソッドにデフォルト実装があるため、必要なイベントだけをオーバーライドすれば十分です。

| メソッド | 戻り値 | 説明 |
|---|---|---|
| `OnMapVoteStart(IMapVoteStartParams)` | `bool` | 投票開始前に発火。`true` を返すとキャンセル |
| `OnRandomMapPick(IMapVoteRandomMapPickParams)` | `List<IMapConfig>` | ランダムマップ選択時に発火。空でないリストを返すと、その内容が投票候補として使用される |
| `OnMapVoteFinished(IMapVoteFinishedEventParams)` | `void` | 投票完了時に発火 |
| `OnMapVoteCancelled(IMapVoteCancelledParams)` | `void` | 投票がキャンセルされたときに発火 |
| `OnMapExtended(IMapVoteExtendParams)` | `void` | 投票結果がマップ延長になったときに発火 |
| `OnMapNotChanged(IMapVoteNotChangedParams)` | `void` | 投票結果が「変更しない」になったときに発火 |
| `OnMapConfirmed(IMapVoteMapConfirmedEventParams)` | `void` | 次のマップが確定したときに発火 |
