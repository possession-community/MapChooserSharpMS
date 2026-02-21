# MapConfig パーシング設計メモ

## 前提

- 24h稼働サーバー向け
- コンフィグは基本的にimmutable（Cooldown等の一部動的な値を除く）
- `ReloadConfigs()` でホットリロード対応

## TOMLキー構造

```
[MapChooserSharpSettings.Default]                             → デフォルト設定
[MapChooserSharpSettings.Groups.<GroupName>]                   → グループ設定
[MapChooserSharpSettings.Groups.<GroupName>.extra.<Name>]      → グループExtra
[MapChooserSharpSettings.Groups.<GroupName>.DaySettings.<Any>] → グループオーバーライド
[MapChooserSharpSettings.Groups.<GroupName>.DaySettings.<Any>.extra.<Name>] → グループオーバーライドExtra

[<map_name>]                                → マップ設定
[<map_name>.extra.<Name>]                   → マップExtra
[<map_name>.DaySettings.<Any>]              → マップオーバーライド
[<map_name>.DaySettings.<Any>.extra.<Name>] → マップオーバーライドExtra
```

## ファイルロード方式

### パターン1: 単一ファイル
- `config/maps.toml` が存在する場合、このファイルのみロード

### パターン2: 複数ファイル
- `config/` 配下の全 `.toml` を再帰的にロード
- `maps.toml` はファイル名として使用不可（パターン1と競合するため）
- 複数ファイルに同一キーがある場合のマージ/エラー方針は要検討

## パーシングフロー

### 1. TOMLパース

全tomlファイルを読み込み、セクションをキーパターンで分類する:

```
キー判定ロジック:
  "MapChooserSharpSettings.Default"                          → Default設定
  "MapChooserSharpSettings.Groups.*.DaySettings.*.extra.*"   → グループオーバーライドExtra
  "MapChooserSharpSettings.Groups.*.DaySettings.*"           → グループオーバーライド
  "MapChooserSharpSettings.Groups.*.extra.*"                 → グループExtra
  "MapChooserSharpSettings.Groups.*"                         → グループ設定
  "*.DaySettings.*.extra.*"                                  → マップオーバーライドExtra
  "*.DaySettings.*"                                          → マップオーバーライド
  "*.extra.*"                                                → マップExtra
  "*"                                                        → マップ設定
```

### 2. デフォルト設定の構築

`[MapChooserSharpSettings.Default]` から全プロパティのデフォルト値を構築する。

### 3. グループ設定の構築（マップより先にパース）

グループ設定はマップ設定より先にパースする。マップ設定の構築時に `GroupSettings[]` で参照するため、この時点で全グループが解決済みである必要がある。

各 `[MapChooserSharpSettings.Groups.<GroupName>]` をパースし、DaySettings/Extraをそれぞれ紐付ける。

### 4. マップ設定の構築

各マップセクションに対して以下の順で値を解決する:

```
1. Default値をベースにする
2. GroupSettings[] の逆順でグループ設定を適用（先頭グループが最優先になるよう後勝ちで上書き）
3. マップ設定を適用（最優先）
4. 統合プロパティ（AllowedSteamIds, DisallowedSteamIds, Extra）は全レイヤーからマージ
5. CooldownOverride がグループにあれば、マップのCooldownを上書き
6. DaySettingsはベース設定とは別に保持（実行時に動的解決）
```

## 設定値の優先度

### ベース設定（DaySettingsなし）

```
Map > Group > Default
```

- 複数グループ: 配列の先頭が最優先
- 統合プロパティ: AllowedSteamIds, DisallowedSteamIds, Extra はマージ
- Extra統合時の注意: 同一ExtraSection・同一キーの場合は通常のOverride順（OverrideMap > Map > OverrideGroup > Group > Default）で上書きされる
  （例: Groupとマップが両方 `extra.shop` の `cost` を持つ場合、マップの値で上書き）
- CooldownOverride（グループ専用）: マップのCooldownを強制上書き
- Cooldown: マップとグループで独立追跡

### DaySettings込みの実行時優先度

```
通常時:       OverrideMap > Map > OverrideGroup > Group > Default
ForceOverride: ForceOverride(priority順) > OverrideMap > Map > OverrideGroup > Group > Default
```

- OverrideMap: マップの設定より優先、グループオーバーライドより常に優先
- OverrideGroup: グループの設定より優先だが、マップの設定よりは低い
- ForceOverride=true: 通常の優先度を無視して最優先（イベント等の一時的な強制適用向け）
- ForceOverride同士: OverridePriority の値が大きい方が優先

## DaySettingsのデータ構造

DaySettingsの名前ごとに1つのOverride Configが生成される。

```
例:
  [ze_example_abc.DaySettings.WeekendNight]  → OverrideConfig "WeekendNight"
  [ze_example_abc.DaySettings.Weekday]       → OverrideConfig "Weekday"
  → 合計2つのOverrideConfigがze_example_abcに紐付く
```

各OverrideConfigはベース設定と同じプロパティ構造を持ち、DaySettingsセクション内で指定された値のみを保持する。

### Extra設定のオーバーライド

DaySettings内のExtra設定は、同一のExtraセクション名であればベースのExtra設定を上書きする。

```
例:
  ベース設定:
    [ze_example_abc.extra.shop]
    cost = 100
    discount = 10

  DaySettings:
    [ze_example_abc.DaySettings.WeekendNight.extra.shop]
    cost = 50

  → WeekendNight適用時、extra.shop の cost は 50 に上書きされる
  → 異なるExtraセクション名（例: extra.AnotherShop）はオーバーライド対象外でそのまま残る
```

## DaySettingsの実行時解決

DaySettingsはconfigロード時にはベース設定にマージせず、別途保持する。
設定値の取得時に現在の曜日・時刻で条件判定を行い、該当するDaySettingsを優先度に従って適用する。

```
解決手順:
  1. 現在の曜日・時刻を取得
  2. マップ/グループの全DaySettingsから条件マッチするものを抽出
     （Enabled=true かつ TargetDays/TargetTimeRanges に合致）
  3. ForceOverride=true のエントリを分離
  4. ベース設定を優先度順に構築: Default → Group → Map
  5. OverrideGroup（ForceOverride=false）を適用: Groupの値を上書き、Mapの値は上書きしない
  6. OverrideMap（ForceOverride=false）を適用: Map/Groupの値を上書き
  7. ForceOverride=true のエントリをOverridePriority順に適用: 全てを上書き
```

## Cooldownの実装上の注意

- グループCooldownとマップCooldownは独立して追跡する
- CooldownOverride はパース時にマップのCooldown値を上書きする（実行時ではなくロード時解決）
- ConfigCooldown / TimedCooldown（CooldownDateTime） → immutable（configから読み取った値）
- CurrentCooldown / LastPlayedAt → mutable（実行時の動的状態）
- ReloadConfigs時にCooldownの動的状態がリセットされないように注意

## TODO

- [ ] TOMLパーサーライブラリの選定（Tomlyn等）
- [ ] 複数ファイル時の同一キー重複検出・エラーハンドリング
- [ ] DaySettingsの実行時解決キャッシュ（毎回解決は無駄なので、分単位等でキャッシュ検討）
- [ ] ForceOverrideの統合プロパティ（AllowedSteamIds等）に対する挙動の決定
- [ ] ReloadConfigs時のCooldown状態保持
