# ConVars (サーバー変数)

ランタイムで変更可能な設定。`server.cfg` や RCON から設定可能。

## MapVote

| ConVar | デフォルト | 範囲 | 説明 |
|---|---|---|---|
| mcs_vote_shuffle_menu | 0 | 0-1 | 投票メニューの選択肢をプレイヤーごとにシャッフルするか |
| mcs_vote_end_time | 15.0 | 5-120 | 投票の制限時間 (秒) |
| mcs_vote_countdown_time | 13 | 0-120 | 投票開始前のカウントダウン (秒)。0 で即開始 |
| mcs_vote_runoff_map_pickup_threshold | 0.3 | 0-1 | Runoff 投票に進むマップの最低得票率 |
| mcs_vote_winner_pickup_threshold | 0.7 | 0-1 | この得票率以上で即勝者確定 |
| mcs_vote_exclude_spectators | 0 | 0-1 | 観戦者を投票から除外するか |
| ~~mcs_vote_change_map_immediately_rtv_vote_success~~ | - | - | **廃止** → `mcs_rtv_immediate_change_threshold` に移行 |

## MapCycle

| ConVar | デフォルト | 範囲 | 説明 |
|---|---|---|---|
| mcs_mapcycle_mode | 0 | 0-2 | マップサイクルモード。0=Time, 1=Round, 2=TimeRound |
| mcs_mapcycle_vote_start_time_threshold | 180 | 0-3600 | Time モードでの投票開始残り時間 (秒) |
| mcs_mapcycle_vote_start_round_threshold | 3 | 0-100 | Round モードでの投票開始残りラウンド数 |
| mcs_ext_user_vote_threshold | 0.6 | 0-1 | !ext の必要投票率 |
| mcs_vote_extend_success_threshold | 0.6 | 0-1 | !ve (延長投票) の可決閾値 |
| mcs_vote_extend_vote_time | 30.0 | 5-120 | !ve の投票時間 (秒) |
| mcs_map_transition_retry_attempts | 3 | 1-10 | マップ変更リトライ回数 |
| mcs_map_transition_retry_interval | 30.0 | 5-300 | リトライ間隔 (秒) |
| mcs_map_transition_fallback_map | de_dust2 | - | リトライ失敗時のフォールバックマップ |

## RTV

| ConVar | デフォルト | 範囲 | 説明 |
|---|---|---|---|
| mcs_rtv_command_unlock_time_next_map_confirmed | 0.0 | 0-1200 | 次マップ確定後の RTV コマンド解禁までの秒数 |
| mcs_rtv_command_unlock_time_map_dont_change | 0.0 | 0-1200 | マップ変更なし後の RTV 解禁秒数 |
| mcs_rtv_command_unlock_time_map_extend | 0.0 | 0-1200 | マップ延長後の RTV 解禁秒数 |
| mcs_rtv_command_unlock_time_map_start | 0.0 | 0-1200 | マップ開始時の RTV 解禁秒数 |
| mcs_rtv_vote_start_threshold | 0.5 | 0-1 | RTV 可決に必要な投票率 |
| mcs_rtv_map_change_timing | 3.0 | 0-60 | RTV 成功後のマップ変更までの秒数。0 で即変更 |
| mcs_rtv_minimum_requirements | 0 | 0-64 | RTV 開始に必要な最低投票数。0 で無効 |
| mcs_rtv_broadcast_player_cast | 1 | 0-1 | RTV 投票時に全体通知するか |
| mcs_rtv_immediate_change_threshold | 0.0 | 0-1 | 投票完了後の RTV 参加率がこの値以上で即時マップ変更。0 = 無効 (常にラウンド終了) |
| mcs_rtv_threshold_decay_time | 0.0 | 0-3600 | RTV 閾値を 100% から設定値へ減衰させる秒数。0 = 無効 |

### RTV 2段階閾値 (`mcs_rtv_immediate_change_threshold`)

投票完了後、NextMap が確定した状態での `!rtv` の動作を参加率で変えます。

1. `mcs_rtv_vote_start_threshold` (通常閾値, e.g. 0.5) 到達 → ラウンド終了時にマップ変更
2. `mcs_rtv_immediate_change_threshold` (即時閾値, e.g. 0.8) 到達 → 即時マップ変更に昇格
3. 0.0 に設定すると即時変更は無効 (常にラウンド終了変更)

通常閾値到達後も `!rtv` は引き続き受け付けられ、即時閾値に達すると自動的に昇格します。

### RTV 閾値の時間減衰 (`mcs_rtv_threshold_decay_time`)

マップ開始時は RTV に 100% の参加率を要求し、時間経過で `mcs_rtv_vote_start_threshold` の設定値まで線形に減衰します。

例: `threshold=0.5`, `decay_time=600` (10分) の場合
- 0分: 100% 必要 (10人中10人)
- 5分: 75% 必要 (10人中8人)
- 10分: 50% 必要 (10人中5人 = 通常値)

0.0 に設定すると減衰なし (最初から設定値の閾値が適用)。

## Nomination

| ConVar | デフォルト | 範囲 | 説明 |
|---|---|---|---|
| mcs_nomination_broadcast_enabled | 1 | 0-1 | ノミネーション時の全体通知を有効にするか |
| mcs_nomination_confirm_menu | 0 | 0-1 | ノミネーション時に確認メニューを表示するか |

## ChatListener

| ConVar | デフォルト | 範囲 | 説明 |
|---|---|---|---|
| mcs_block_chat_during_vote | 0 | 0-1 | 投票中のチャットをブロックするか (AntiCanvas) |
| mcs_rtv_immediate_change_threshold | 0.0 | 0-1 | 投票完了後の RTV 参加率がこの値以上で即時マップ変更。0 = 無効 (常にラウンド終了) |
| mcs_rtv_threshold_decay_time | 0.0 | 0-3600 | RTV 閾値を 100% から設定値へ減衰させる秒数。0 = 無効 |
