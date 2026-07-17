# 監査システム

MapChooserSharpMS はサーバーイベントを SurrealDB (Wuling 経由) に記録します。全監査テーブルは追記専用で、マルチサーバー環境向けに `server_id` フィールドを含みます。

## テーブル

### mcs_audit_map_play

マップ終了時に記録されます。

| フィールド | 型 | 説明 |
|---|---|---|
| map_name | string | マップ名 |
| workshop_id | int? | Workshop ID (Workshop マップでない場合 null) |
| group_names | string[] | マップが所属するグループ |
| peak_player_count | int | マップ中の最大プレイヤー数 |
| end_player_count | int | マップ終了時のプレイヤー数 |
| map_started_at | datetime | マップ開始時刻 |
| map_ended_at | datetime | マップ終了時刻 |
| map_end_reason | string | マップ終了理由 (timelimit, rtv など) |
| round_count | int | プレイされたラウンド数 |
| timelimit_type | string | リミットタイプ (timelimit / maxrounds) |
| configured_timelimit | float | 設定された時間/ラウンドリミット値 |
| extend_count | int | 延長の合計回数 |
| max_normal_extends | int | MaxExtends 設定値 |
| normal_extends_used | int | MapVote 延長の使用回数 |
| admin_vote_extend_count | int | 管理者投票延長 (!ve) の回数 |
| user_ext_extends_used | int | !ext 延長の使用回数 |
| max_user_ext_extends | int | MaxExtCommandUses 設定値 |
| server_id | string | サーバー識別子 |
| created_at | datetime | レコード作成時刻 |

### mcs_audit_nomination

マップ終了時に各ノミネーションが記録されます。

| フィールド | 型 | 説明 |
|---|---|---|
| nominated_at | datetime | ノミネート日時 |
| map_name | string | ノミネートされたマップ名 |
| workshop_id | int? | Workshop ID |
| nominator_steam_id | int? | ノミネーターの SteamID (コンソールの場合 null) |
| nomination_type | string | ノミネーション種別 (下記の値を参照) |
| nomination_result | string | 結果 (下記の値を参照) |
| group_name | string | マップのグループ名 |
| server_id | string | サーバー識別子 |
| created_at | datetime | レコード作成時刻 |

#### nomination_type の値

| 値 | 説明 |
|---|---|
| `user` | プレイヤーが `!nominate` でノミネート |
| `admin` | 管理者が `!nominate_addmap` / `!nominate_addwsmap` でノミネート |
| `console` | サーバーコンソールからノミネート |

#### nomination_result の値

| 値 | 説明 |
|---|---|
| `voted_won` | 投票候補に含まれ、当選した |
| `voted_lost` | 投票候補に含まれたが、落選した |
| `not_picked` | 投票候補に選ばれなかった (スロット上限や閾値でフィルタ) |
| `cancelled_by_admin` | 管理者がノミネートを削除した (`!nominate_removemap`) |
| `cancelled_by_self` | プレイヤーが自分のノミネートを取り消した (`!unnominate`) |

### mcs_audit_vote

マップ投票終了時に記録されます。

| フィールド | 型 | 説明 |
|---|---|---|
| vote_started_at | datetime | 投票開始時刻 |
| vote_ended_at | datetime | 投票終了時刻 |
| vote_result | string | 結果 (confirmed / extended / not_changed / cancelled) |
| map_vote_start_reason | string | 投票トリガー (timelimit / rtv) |
| vote_duration_config | float | 設定された投票時間 (秒) |
| total_players | int | 投票時のプレイヤー数 |
| total_votes | int | 投票数 |
| server_id | string | サーバー識別子 |
| created_at | datetime | レコード作成時刻 |

### mcs_audit_vote_candidate

マップ投票の各候補が記録されます。`vote_id` で `mcs_audit_vote` と紐付きます。

| フィールド | 型 | 説明 |
|---|---|---|
| vote_id | string | 親の投票レコード ID |
| map_name | string | 候補マップ名 |
| workshop_id | int? | Workshop ID |
| vote_count | int | 得票数 |
| is_winner | bool | この候補が当選したか |
| is_nominated | bool | この候補がノミネートされていたか |
| candidate_type | string | 候補の選出方法 (下記の値を参照) |
| created_at | datetime | レコード作成時刻 |

#### candidate_type の値

| 値 | 説明 |
|---|---|
| `extend` | 現在のマップを延長するオプション |
| `dont_change` | マップを変更しないオプション (RTV 投票時) |
| `map` | 通常のマップ候補 (ノミネートまたはランダム選出) |

### mcs_audit_extend_vote

管理者延長投票 (!ve) 終了時に記録されます。

| フィールド | 型 | 説明 |
|---|---|---|
| vote_started_at | datetime | 投票開始時刻 |
| vote_ended_at | datetime | 投票終了時刻 |
| vote_result | string | 結果 |
| success_threshold | float | 可決に必要な割合 |
| yes_count | int | 賛成票 |
| no_count | int | 反対票 |
| total_players | int | プレイヤー数 |
| passed | bool | 投票が可決されたか |
| initiator_steam_id | int? | 投票を開始した管理者 |
| server_id | string | サーバー識別子 |
| created_at | datetime | レコード作成時刻 |

### mcs_audit_rtv

RTV がトリガーされた時に記録されます。レコード ID で `mcs_audit_rtv_vote` と紐付きます。

| フィールド | 型 | 説明 |
|---|---|---|
| triggered_at | datetime | RTV トリガー時刻 |
| threshold | int | 必要投票数 |
| immediate_threshold | int? | 即時変更閾値 (無効の場合 null) |
| is_forced | bool | 強制 RTV (!forcertv) かどうか |
| map_state | string | トリガー時のマップ状態 |
| server_id | string | サーバー識別子 |
| created_at | datetime | レコード作成時刻 |

### mcs_audit_rtv_vote

個別の RTV 投票。`rtv_id` で `mcs_audit_rtv` と紐付きます。

| フィールド | 型 | 説明 |
|---|---|---|
| rtv_id | string | 親の RTV レコード ID |
| steam_id | int | 投票者の SteamID |
| voted_at | datetime | 投票日時 |
| server_id | string | サーバー識別子 |
| created_at | datetime | レコード作成時刻 |

### mcs_audit_ext

!ext の閾値到達時に記録されます。レコード ID で `mcs_audit_ext_vote` と紐付きます。

| フィールド | 型 | 説明 |
|---|---|---|
| triggered_at | datetime | !ext トリガー時刻 |
| threshold | int | 必要投票数 |
| map_state | string | トリガー時のマップ状態 |
| server_id | string | サーバー識別子 |
| created_at | datetime | レコード作成時刻 |

### mcs_audit_ext_vote

個別の !ext 投票。`ext_id` で `mcs_audit_ext` と紐付きます。

| フィールド | 型 | 説明 |
|---|---|---|
| ext_id | string | 親の !ext レコード ID |
| steam_id | int | 投票者の SteamID |
| voted_at | datetime | 投票日時 |
| server_id | string | サーバー識別子 |
| created_at | datetime | レコード作成時刻 |

### mcs_audit_cooldown_expired

マップまたはグループのクールダウンが完全に解除された時に記録されます (カウント 0 かつ時限クールダウン期限切れ)。クールダウンサイクルごとに1回のみ発火します。

| フィールド | 型 | 説明 |
|---|---|---|
| name | string | マップ名またはグループ名 |
| cooldown_type | string | `map` または `group` |
| became_available_at | datetime | クールダウンが完全に解除された時刻 |
| server_id | string | サーバー識別子 |

## 非監査永続化テーブル

以下は監査ログではなく、運用データテーブルです。

### mcs_map_cooldown / mcs_group_cooldown

マップとグループの現在のクールダウン状態。マップ変更ごとに upsert されます。

| フィールド | 型 | 説明 |
|---|---|---|
| name | string | マップ名またはグループ名 |
| cooldown | int | 残りカウントクールダウン |
| timed_cooldown_end | datetime | 時限クールダウン期限 |
| last_played_at | datetime | 最後にプレイされた時刻 |
| unplayed_count | int | クールダウン明け後、プレイされずに経過したマップ変更回数 |
| nom_cooldown | int | 残りノミネーションクールダウン |
| nom_timed_cooldown_end | datetime | ノミネーション時限クールダウン期限 |
| last_nominated_at | datetime | 最後にノミネートされた時刻 |

### mcs_user_nom_cooldown

プレイヤーごとのノミネーションクールダウン状態。

| フィールド | 型 | 説明 |
|---|---|---|
| steam_id | int | プレイヤーの SteamID |
| remaining_count | int | 残りクールダウンカウント |
| cooldown_until | datetime | クールダウン期限 |
| server_id | string | サーバー識別子 |
| created_at | datetime | レコード作成時刻 |
