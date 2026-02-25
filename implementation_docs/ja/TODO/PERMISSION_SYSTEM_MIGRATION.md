# TODO: 権限システム移行 (Permission System Migration)

## 概要
CSS(CounterStrikeSharp) ベースの `css/xxx` 権限から、Node-based 権限システム `mcs.*` に移行する。
同時に、Config ベースのユーザー制御 (SteamId / RequiredPermissions) を権限システムに統合して Config から削除する。

## 権限ノード体系

### ノミネーション関連
| Permission Node | 用途 |
|---|---|
| `mcs.nominate.map.allow.<map_name>` | 特定マップのノミネーション許可 |
| `mcs.nominate.map.deny.<map_name>` | 特定マップのノミネーション拒否 |
| `mcs.nominate.group.allow.<group_name>` | グループ内全マップのノミネーション許可 |
| `mcs.nominate.group.deny.<group_name>` | グループ内全マップのノミネーション拒否 |
| `mcs.nominate.generic` | 汎用ノミネーション権限 |
| `mcs.nominate.management` | 管理者レベルノミネーション権限 |

### 管理コマンド関連
| Permission Node | 用途 |
|---|---|
| `mcs.admin.nominate` | 管理者ノミネーションコマンド (`css_nominate_addmap`, `css_nominate_removemap`) |
| `mcs.admin.nominate.bypass-restriction` | `ProhibitAdminNomination` 制限を無視して管理者ノミネーション可能 |

## 解決順 (Priority)
```
(Map Deny || Group Deny) > (Map Allow || Group Allow)
```
- Deny が1つでもマッチすれば拒否（Map/Group 問わず）
- Deny がなく、Allow が1つでもマッチすれば許可（Map/Group 問わず）
- どちらもなし → デフォルト動作

### 例
- `mcs.nominate.group.allow.Premium` → Premium グループの全マップをノミネート可能
- `mcs.nominate.map.deny.ze_xxx` or `mcs.nominate.group.deny.HardZE` → 該当マップ/グループ拒否（Allow の有無に関係なく）

## Config から削除するプロパティ

以下のプロパティは権限システムで完全に代替されるため、Config (TOML) から削除する。

| プロパティ | 代替手段 |
|---|---|
| `AllowedSteamIds` | `mcs.nominate.map.allow.<map>` / `mcs.nominate.group.allow.<group>` |
| `DisallowedSteamIds` | `mcs.nominate.map.deny.<map>` / `mcs.nominate.group.deny.<group>` |
| `RestrictToAllowedUsersOnly` | 権限ノードの有無で制御 |
| `RequiredPermissions` | `mcs.nominate.group.allow.<group>` で代替 |
| `ProhibitAdminNomination` | Config に残す方向。マップ単位の制限フラグなので権限では代替不可。`mcs.admin.nominate.bypass-restriction` で突破可能 |

## 影響範囲

### 削除対象コード
- `ParsedProperties` から該当フィールド削除
- `TomlPropertyMapper.ExtractProperties()` から該当パース処理削除
- `MapConfigParsingService` のマージロジック (SteamId 累積処理等) 削除
- `INominationConfig` から該当プロパティ削除
- `NominationConfig` モデル更新

### 新規実装
- 権限チェックサービス (`IPermissionService` 等)
- ノミネーション時の権限解決ロジック (Any Deny > Any Allow > Default)
- `McsMapNominationCommands` の権限チェック更新

### テスト更新
- SteamId 累積テスト削除
- RequiredPermissions 関連テスト削除/更新
- 権限解決ロジックの新規テスト追加
- TOML リソースファイルから該当プロパティ削除

## 備考
- テスト用 TOML ファイル内のパーミッション文字列は既に `mcs.*` 形式に移行済み。
