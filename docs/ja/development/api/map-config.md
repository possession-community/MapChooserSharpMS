# マップ設定 API

MCS のマップ設定は TOML ファイルから読み込まれ、Default → Group → Map の優先順で合成されます。
ここでは、合成後の設定を読み取るための公開インターフェースを解説します。

設定ファイルの書き方については [MAP_CONFIG.md](../../configuration/MAP_CONFIG.md) を参照してください。

---

## IMcsMapConfigProvider

マップ設定の検索とリロードを行うプロバイダです。
`IMapChooserSharpShared.McsMapConfigProvider` からアクセスします。

| メンバー | 型 | 説明 |
|---|---|---|
| `ToolingService` | `IMapConfigToolingService` | 表示名解決やクールダウン集約などのユーティリティ |
| `ReloadConfigs()` | `void` | ディスクから全マップ設定を再読み込みする |
| `GetMapConfigs()` | `IReadOnlyDictionary<string, IReadOnlyCollection<IMapConfigOverrides>>` | 全マップ設定を取得する。DaySettings オーバーライドを含む |
| `GetGroupSettings()` | `IReadOnlyDictionary<string, IReadOnlyCollection<IMapGroupConfigOverrides>>` | 全グループ設定を取得する。DaySettings オーバーライドを含む |
| `TryGetMapConfig(string, out IMapConfig)` | `bool` | マップ名で設定を検索する。DaySettings を評価済みの最終設定を返す |
| `TryGetMapConfig(long, out IMapConfig)` | `bool` | Workshop ID で設定を検索する |

### IMapConfigToolingService

| メソッド | 説明 |
|---|---|
| `ResolveMapDisplayName(IMapConfig)` | `MapNameAlias` が設定されていればそれを、なければ `MapName` を返す |
| `GetHighestCooldown(IMapConfig)` | マップ本体とグループのクールダウンを比較して最も大きい値を返す |
| `FindMapsBySearchTag(string, IEnumerable<IMapConfig>)` | `SearchTags` に指定タグを含むマップのリストを返す |

---

## IMapConfig

個々のマップの設定を表します。`IBaseMapConfig` を継承しています。

| プロパティ | 型 | 説明 |
|---|---|---|
| `MapName` | `string` | マップの内部名 (TOML のセクション名) |
| `MapNameAlias` | `string` | 表示用の別名。空文字なら `MapName` がそのまま使われる |
| `MapDescription` | `string` | `!mapinfo` コマンドで表示される説明文 |
| `WorkshopId` | `long` | Steam Workshop ID。未指定の場合は `0` |
| `GroupSettings` | `List<IMapGroupConfig>` | このマップが所属するグループ設定のリスト。先頭のグループほど優先度が高い |
| `SearchTags` | `IReadOnlyList<string>` | ノミネーション検索用タグ (例: `!nominate <tag>`)。グループのタグはマップにマージされる |

`IBaseMapConfig` から継承されるプロパティ:

| プロパティ | 型 | 説明 |
|---|---|---|
| `IsDisabled` | `bool` | `true` の場合、ノミネーションやランダム選択の対象外になる |
| `MaxExtends` | `int` | マップ投票の Extend 選択肢で消費できる延長回数の上限。`0` で Extend 選択肢を非表示にする |
| `MaxExtCommandUses` | `int` | `!ext` コマンドで延長できる回数の上限 |
| `MapTime` | `int` | このマップの制限時間 (分)。マップ開始時に `mp_timelimit` に適用され、以降は MCS が内部で管理する |
| `ExtendTimePerExtends` | `int` | 延長 1 回あたりに追加される分数 |
| `MapRounds` | `int` | このマップのラウンド数。マップ開始時に `mp_maxrounds` に適用され、以降は MCS が内部で管理する |
| `ExtendRoundsPerExtends` | `int` | 延長 1 回あたりに追加されるラウンド数 |
| `RandomPickConfig` | `IRandomPickConfig` | ランダム選択時の重み付けや除外設定 |
| `NominationConfig` | `INominationConfig` | ノミネーションの制限設定 |
| `CooldownConfig` | `ICooldownConfig` | クールダウンの設定と現在の状態 |
| `ExtraConfiguration` | `IExtraConfigAccessor` | 外部プラグイン向けのカスタム設定セクション |

---

## IMapGroupConfig

グループ設定を表します。`IBaseMapConfig` の全プロパティに加えて、グループ固有のプロパティを持ちます。

| プロパティ | 型 | 説明 |
|---|---|---|
| `GroupName` | `string` | グループの識別名 (TOML の `Groups.<名前>` に対応) |
| `ShortGroupName` | `string` | 投票画面で表示される短縮タグ。最大 4 文字。TOML で 5 文字以上を指定した場合は先頭 4 文字に切り詰められる |
| `MapCooldownOverride` | `int` | 正の値を指定すると、このグループに所属するマップのクールダウンをこの値で上書きする。マップ側の `Cooldown` よりも優先される |
| `NominationLimit` | `int` | このグループからノミネートできるマップの最大数。`0` は無制限。グループごとに異なる値を設定可能 |
| `SearchTags` | `IReadOnlyList<string>` | グループレベルのノミネーション検索用タグ。各マップの `SearchTags` にマージされる |

---

## INominationConfig

マップやグループに設定できるノミネーション制限のルールです。

| プロパティ | 型 | 説明 |
|---|---|---|
| `MaxPlayers` | `int` | サーバーのプレイヤー数がこの値以上のときノミネーション不可。`0` = 制限なし |
| `MinPlayers` | `int` | サーバーのプレイヤー数がこの値以下のときノミネーション不可。`0` = 制限なし |
| `ProhibitAdminNomination` | `bool` | `true` にすると `!nominate_addmap` による管理者ノミネーションも拒否する (コンソールからは可能) |
| `DaysAllowed` | `IReadOnlyList<DayOfWeek>` | ノミネーション可能な曜日。空リストなら全曜日で許可 |
| `AllowedTimeRanges` | `IReadOnlyList<ITimeRange>` | ノミネーション可能な時間帯。空リストなら全時間帯で許可 |
| `RestrictToAllowedUsersOnly` | `bool` | `true` の場合、`mcs.nominate.*.allow.*` 権限を持つプレイヤーのみがこのマップをノミネートできる。デフォルト `false` |

### ITimeRange

時間帯の範囲を表します。日付をまたぐ範囲 (例: `22:00-03:00`) もサポートしています。

| プロパティ / メソッド | 型 | 説明 |
|---|---|---|
| `StartTime` | `TimeOnly` | 開始時刻 |
| `EndTime` | `TimeOnly` | 終了時刻 |
| `IsInRange(TimeOnly time)` | `bool` | 指定した時刻がこの範囲内かどうかを返す |

---

## ICooldownConfig

マップやグループのクールダウン設定と、現在の状態を保持します。

MCS のクールダウンには 2 つの軸があります:
- **回数ベース**: マップがプレイされるたびに 1 ずつ減少するカウンタ
- **時限ベース**: 指定した時間が経過するまでクールダウン状態を維持する

| プロパティ | 型 | 説明 |
|---|---|---|
| `ConfigCooldown` | `int` | TOML で設定された回数ベースのクールダウン値 |
| `TimedCooldown` | `TimeSpan` | TOML で設定された時限クールダウンの長さ |
| `CurrentCooldown` | `int` | 実行時のメモリ上にある現在の回数クールダウン。マップがプレイされると `ConfigCooldown` の値がセットされ、他のマップがプレイされるたびに減少する |
| `LastPlayedAt` | `DateTime` | 最後にプレイされた UTC タイムスタンプ。時限クールダウンの終了判定に使用される |
| `ConfigNominationCooldown` | `int` | TOML で設定されたノミネーション専用の回数クールダウン。投票候補として消費されたときに付与される。`0` = 無効 |
| `NominationTimedCooldown` | `TimeSpan` | ノミネーション専用の時限クールダウンの長さ |

---

## IRandomPickConfig

投票時のランダムマップ選択に関する設定です。

| プロパティ | 型 | 説明 |
|---|---|---|
| `MapSelectionWeight` | `uint` | ランダム選択の重み。値が大きいほど投票候補に選ばれやすい。`0` にするとランダム選択の対象外になる |
| `IsPickable` | `bool` | ランダム選択の対象になるかどうか。TOML の `OnlyNomination = true` を指定すると `false` になる |
| `BypassNominationRestriction` | `bool` | `true` の場合、ランダム選択時にノミネーション制限 (人数・曜日・時間帯等) を無視する |

---

## IExtraConfigAccessor

TOML の `[マップ名.extra.セクション名]` に定義したカスタム設定に型安全にアクセスするためのインターフェースです。
外部プラグインがマップごとの独自データを MCS の設定ファイルに相乗りさせる仕組みとして設計されています。

Extra 設定は Default → Group → Map の順で**統合** (マージ) されます。同じセクション・同じキーがある場合は後勝ちです。

| メソッド | 戻り値 | 説明 |
|---|---|---|
| `GetValue<T>(section, key, defaultValue)` | `T` | 指定したセクション・キーの値を型変換して返す。見つからなければ `defaultValue` |
| `TryGetValue<T>(section, key, out value)` | `bool` | 値が存在すれば `true` を返し、`value` に結果をセットする |
| `HasSection(section)` | `bool` | 指定したセクションが存在するか |
| `HasKey(section, key)` | `bool` | 指定したセクション内にキーが存在するか |
| `GetSections()` | `IReadOnlyCollection<string>` | 全セクション名を取得する |
| `GetKeys(section)` | `IReadOnlyCollection<string>` | セクション内の全キー名を取得する |
| `GetArray<T>(section, key)` | `IReadOnlyList<T>` | TOML 配列を型変換して返す |

---

## DaySettings オーバーライド

`GetMapConfigs()` や `GetGroupSettings()` が返すコレクションには、ベース設定に加えて DaySettings によるオーバーライド設定が含まれます。

### IBaseOverrideConfig

全てのオーバーライドエントリが持つ共通プロパティです。

| プロパティ | 型 | 説明 |
|---|---|---|
| `OverrideConfigName` | `string` | オーバーライド名。ベース設定の場合は `IBaseOverrideConfig.BaseConfigName` (定数) |
| `Enabled` | `bool` | このオーバーライドが有効かどうか |
| `ForceOverride` | `bool` | `true` の場合、通常の優先度を無視して最優先で適用される |
| `OverridePriority` | `int` | 複数の ForceOverride がマッチした場合の優先順位 (大きい値が優先) |
| `TargetDays` | `IReadOnlyCollection<DayOfWeek>` | このオーバーライドが適用される曜日 |
| `TargetTimeRanges` | `IReadOnlyCollection<ITimeRange>` | このオーバーライドが適用される時間帯 |

### IMapConfigOverrides : IBaseOverrideConfig

| プロパティ | 型 | 説明 |
|---|---|---|
| `MapConfig` | `IMapConfig` | このオーバーライドが適用された場合のマップ設定 |

### IMapGroupConfigOverrides : IBaseOverrideConfig

| プロパティ | 型 | 説明 |
|---|---|---|
| `GroupConfig` | `IMapGroupConfig` | このオーバーライドが適用された場合のグループ設定 |

通常は `TryGetMapConfig()` を使えば DaySettings が自動評価された最終設定が取得できます。
`GetMapConfigs()` を使うのは、全オーバーライドを一覧表示したい場合や、独自のスケジュール評価を行いたい場合です。
