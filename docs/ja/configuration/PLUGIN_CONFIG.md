# プラグインコンフィグ (config.toml)

## General

```toml
[General]
ShouldUseAliasMapNameIfAvailable = true
VerboseCooldownPrint = true
WorkshopCollectionIds = []
ShouldAutoFixMapName = true
SteamWebApiKey = ""
RtvMapChangeBehaviour = "ImmediatelyWithTime"
```

| キー | 型 | デフォルト | 説明 |
|---|---|---|---|
| ShouldUseAliasMapNameIfAvailable | bool | true | MapNameAlias が存在する場合に表示名として使用するか |
| VerboseCooldownPrint | bool | true | クールダウン中に残り秒数を表示するか |
| WorkshopCollectionIds | string[] | [] | 自動同期するワークショップコレクションIDの配列 |
| ShouldAutoFixMapName | bool | true | マップ開始時にワークショップマップの config ファイル名を実マップ名に自動修正するか |
| SteamWebApiKey | string | "" | Steam Web API キー。空欄の場合は環境変数 `STEAM_WEB_API_KEY` にフォールバック。[取得はこちら](https://steamcommunity.com/dev/apikey) |
| RtvMapChangeBehaviour | enum | ImmediatelyWithTime | RTV 可決後のマップ変更方法。`ImmediatelyWithTime` (即時+カウントダウン) / `Cs2EndMatchScreen` (CS2終了画面経由) |

## MapCycle

```toml
[MapCycle]
FallbackMaxExtends = 3
FallbackMaxExtCommandUses = 1
FallbackExtendTimePerExtends = 15
FallbackExtendRoundsPerExtends = 5
ShouldStopSourceTvRecording = false
MapConfigExecutionType = "ExactMatch"
MapConfigDirectoryPath = "maps/"
```

| キー | 型 | デフォルト | 説明 |
|---|---|---|---|
| FallbackMaxExtends | int | 3 | config 未定義マップのデフォルト最大延長回数 |
| FallbackMaxExtCommandUses | int | 1 | config 未定義マップの !ext 最大使用回数 |
| FallbackExtendTimePerExtends | int | 15 | 延長 1 回あたりの延長時間 (分) |
| FallbackExtendRoundsPerExtends | int | 5 | 延長 1 回あたりの延長ラウンド数 |
| ShouldStopSourceTvRecording | bool | false | マップ変更前に tv_stoprecord を実行するか (SourceTV クラッシュ防止) |
| MapConfigExecutionType | enum | ExactMatch | マップ cfg 実行マッチング方式。`ExactMatch` / `StartWithMatch` / `PartialMatch` |
| MapConfigDirectoryPath | string | maps/ | マップ TOML config ディレクトリのパス (モジュールディレクトリからの相対パス) |
| PauseMapCycleWhenServerEmpty | bool | false | 有効にすると、サーバーに実プレイヤーがいない間はマップ遷移とクールダウン消費をスキップする |

### マップ config 実行 (cfg ファイル)

マップ開始時に、現在のマップおよび所属グループにマッチする `.cfg` ファイルを自動実行します。cfg ファイルは Sharp ルート配下の固定ディレクトリから読み込まれます。

```
sharp/configs/mcsms/cfgs/
├── maps/       # マップ固有の cfg ファイル
│   ├── de_dust2.cfg
│   └── ze_example_v1.cfg
└── groups/     # グループ固有の cfg ファイル
    ├── ze_maps.cfg
    └── surf_maps.cfg
```

**実行順:** グループ cfg → マップ cfg の順で実行。マップの設定がグループを上書きします。

**マッチングモード** (`MapConfigExecutionType`):
- `ExactMatch` — `<マップ名>.cfg` のみ実行 (大文字小文字区別なし)
- `StartWithMatch` — マップ名がファイル名で始まる cfg を全て実行 (例: `de_dust2` に対して `de_.cfg` と `de_dust2.cfg`)、短いプレフィックス順
- `PartialMatch` — マップ名にファイル名が含まれる cfg を全て実行

**exec 展開:** cfg ファイル内に `exec <パス>` 行がある場合、参照先ファイルは同ディレクトリ基準で解決され (`csgo/cfg/` ではなく)、インラインで展開されます。再帰的な include は最大8段までサポート。

## MapVote

```toml
[MapVote]
MaxVoteElements = 5
ShouldPrintVoteToChat = true
ShouldPrintVoteRemainingTime = true
CountdownUiType = "Center"

[MapVote.Sound]
SoundFile = ""
InitialVoteCountdownStartSound = ""
InitialVoteStartSound = ""
InitialVoteFinishSound = ""
InitialVoteCountdownSound1 = ""
# ... (1-10)
RunoffVoteCountdownStartSound = ""
RunoffVoteStartSound = ""
RunoffVoteFinishSound = ""
RunoffVoteCountdownSound1 = ""
# ... (1-10)
```

| キー | 型 | デフォルト | 説明 |
|---|---|---|---|
| MaxVoteElements | int | 5 | 投票メニューに表示するマップ数 (Extend/DontChange 含む) |
| ShouldPrintVoteToChat | bool | true | プレイヤーが投票した際にチャットに表示するか |
| ShouldPrintVoteRemainingTime | bool | true | 投票中に残り時間をチャットに表示するか |
| CountdownUiType | enum ([Flags]) | Center | カウントダウンのデフォルト表示方式。`None` / `Hint` / `Center` / `Chat`。カンマ区切りで複数指定可 (例: `"Center,Chat"`)。プレイヤーは `!mcs_settings countdown` で個別に変更可能 (ICookie 永続化) |
| SoundFile | string | "" | .vsndevts ファイルパス。毎マップ開始時に自動 precache。他プラグインで済んでいれば空欄可 |
