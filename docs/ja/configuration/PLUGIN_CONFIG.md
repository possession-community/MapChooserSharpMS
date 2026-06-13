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
GroupConfigDirectoryPath = "groups/"
```

| キー | 型 | デフォルト | 説明 |
|---|---|---|---|
| FallbackMaxExtends | int | 3 | config 未定義マップのデフォルト最大延長回数 |
| FallbackMaxExtCommandUses | int | 1 | config 未定義マップの !ext 最大使用回数 |
| FallbackExtendTimePerExtends | int | 15 | 延長 1 回あたりの延長時間 (分) |
| FallbackExtendRoundsPerExtends | int | 5 | 延長 1 回あたりの延長ラウンド数 |
| ShouldStopSourceTvRecording | bool | false | マップ変更前に tv_stoprecord を実行するか (SourceTV クラッシュ防止) |
| MapConfigExecutionType | enum | ExactMatch | マップ cfg 実行マッチング方式。`ExactMatch` / `StartWithMatch` / `PartialMatch` |
| MapConfigDirectoryPath | string | maps/ | マップ config ディレクトリのパス (モジュールディレクトリからの相対パス) |
| GroupConfigDirectoryPath | string | groups/ | グループ config ディレクトリのパス (モジュールディレクトリからの相対パス) |

## Nomination

```toml
[Nomination]
MenuType = "Default"
PerGroupNominationLimit = 0
```

| キー | 型 | デフォルト | 説明 |
|---|---|---|---|
| MenuType | enum | Default | ノミネーションメニューの表示方式 |
| PerGroupNominationLimit | int | 0 | 同一グループからのノミネート数制限。0 = 無制限 |

## MapVote

```toml
[MapVote]
MaxVoteElements = 5
ShouldPrintVoteToChat = true
ShouldPrintVoteRemainingTime = true
CountdownUiType = "CenterHtml"

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
| CountdownUiType | enum | CenterHtml | 投票開始前カウントダウンの表示方式。`None` / `CenterHud` / `CenterAlert` / `CenterHtml` / `Chat` |
| SoundFile | string | "" | .vsndevts ファイルパス (precache 用)。他プラグインで済んでいれば空欄可 |
