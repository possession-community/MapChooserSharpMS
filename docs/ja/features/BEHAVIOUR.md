# 内部動作

直接設定するものではないが、MCS の動作に影響する暗黙的な挙動を説明します。

## ブートフェーズ (`+host_workshop_map`)

サーバーが `+host_workshop_map <workshopId>` 付きで起動された場合、MCS は**ブートフェーズ**に入ります。この間:

- クールダウンの消費が抑制されます (マップクールダウンがデクリメントされない)
- Audit レコードが作成されません
- マップサイクルの遷移がトリガーされません

これにより、Workshop マップのダウンロード中に読み込まれる中間マップでクールダウンが消費されたり、Audit エントリが生成されたりするのを防ぎます。

ブートフェーズは、指定された Workshop ID に一致するマップがロードされた時点で終了します。そのマップを含め、以降は全システムが通常動作します。

起動引数に `+host_workshop_map` がない場合、ブートフェーズはスキップされます。

## サーバー空時の一時停止 (`PauseMapCycleWhenServerEmpty`)

`[MapCycle] PauseMapCycleWhenServerEmpty = true` で有効にした場合:

- 実プレイヤー (Bot・HLTV 除く) がサーバーにいない間、マップサイクルの遷移が一時停止します
- プレイヤーなしで終了したマップのクールダウン消費がスキップされます
- 内部のタイム/ラウンドリミットトラッカーも一時停止するため、プレイヤー参加時に正しく遷移が発火します

無人サーバーがマップを空回りしてクールダウンを浪費するのを防ぎます。プレイヤーが接続すると、1 秒以内に通常動作を再開します。

## タイムリミット管理

MCS はマップのタイム/ラウンドリミットを完全に管理します:

1. **コンフィグ適用**: マップ開始時に、マップコンフィグの `MapTime` (分) または `MapRounds` を `mp_timelimit` / `mp_maxrounds` に適用
2. **内部マネージャ**: MCS が ConVar の値を読み取り、内部 TimeLimitManager を初期化
3. **ConVar 上書き**: `mp_timelimit` と `mp_maxrounds` の両方を `99999999` に設定し、ゲーム側で勝手にマッチが終了するのを防止
4. **MCS 主導のライフサイクル**: 投票閾値やリミット到達イベントはゲームのネイティブチェックではなく、内部マネージャによって駆動

マップ config 実行 (cfg ファイル) は MapTime/MapRounds の適用**前**に実行されるため、`sv_airaccelerate` 等のゲーム関連 ConVar が先に設定されます。

## マッチ終了フロー

タイム/ラウンドリミットに到達し、次のマップが確定している場合:

- MCS はエンジンの `BeginIntermission` 関数をネイティブ関数ポインタ経由で直接呼び出します (起動時に `server.dll` から解決)
- `mcs_end_match_immediately = 1` (デフォルト): まず `TerminateRound` を呼んでラウンド終了演出 (Round Won/Lost) を表示し、次の tick で `BeginIntermission` を呼び出します
- `mcs_end_match_immediately = 0`: 現在のラウンドの自然終了を待ちます

冪等性ガードにより、複数のコードパスが intermission をトリガーしようとしても、マップごとに 1 回だけ発火します。

## マップ config 実行 (cfg ファイル)

マップ開始ごとに、`csgo/cfg/mcsms/maps/` と `csgo/cfg/mcsms/groups/` の `.cfg` ファイルを `exec` コマンドで実行します。マッチングモードやディレクトリ構成の詳細は [プラグインコンフィグ](../configuration/PLUGIN_CONFIG.md#マップ-config-実行-cfg-ファイル) を参照してください。

## サウンド Precache

`[MapVote.Sound] SoundFile` で指定された `.vsndevts` ファイルは、毎マップ開始時の `OnResourcePrecache` コールバックで自動的に precache されます。パスを設定するだけで追加のセットアップは不要です。

## SourceTV 録画

`[MapCycle] ShouldStopSourceTvRecording = true` の場合、マップ変更前に `tv_stoprecord` を実行して SourceTV のクラッシュを防止します。

## マップコンフィグの解決順序

現在のマップのコンフィグを解決する際、MCS は以下の順序でチェックします:

1. **Addon ID** (`GetAddonName()` から取得した Workshop ID) — Workshop マップで BSP 名がコンフィグ名と異なる場合に対応するため、最初にチェック
2. **マップ名** (`GetMapName()`) — 標準的な名前ベースの検索

これにより、サーバー内部のマップ名がコンフィグファイル名と一致しない場合でも、Workshop マップが正しくマッチングされます。
