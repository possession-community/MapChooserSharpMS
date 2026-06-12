# MCS 残実装プラン

> 作成: 2026-06-12 (Extend システム実装完了直後のセッションより)
> ブランチ: `feature/wuling-migration` (HEAD: fb216a5)
> このファイルはコミットしない。完了したフェーズから順次消し込み、全完了で削除する。
>
> 共通規律: TreatWarningsAsErrors で 0 warning / `dotnet test` 214+ 全パス /
> 大きめのフェーズ完了ごとに Fable モデルでレビュー → Critical 修正 → コミット。
> ModSharp API を触る前に `refs/modsharp-knowledge/catalog/_index.md` から該当
> namespace ファイルを読む (CLAUDE.md のワークフロー遵守)。

---

## ~~Phase 1: RTV 転換トリガー + RTV コマンド~~ ✅ 完了 (2026-06-12, 8a9fe26 + 次コミット)

- 1a: RTV 閾値トリガー配線済 (AddClientToRtv → threshold check → InitiateRtvVote)
- 1b: RTV 成功後転換配線済 (IsRtvVote on params, immediate/next-round-end via ConVar)
- 1c: RTV コマンド 5 種 + ChatListener モジュール (plain "rtv" in chat)
- 権限ノード全面リファクタ: `mcs.admin.command.<module>.<command>` 粒度に統一

---

## Phase 2: Cooldown 実装 (本命)

### ~~2b. サービス実装~~ ✅ 完了 (2026-06-12)

- QueryService / CommandService / LifecycleService 3分割
- Editable OnMapCooldownApply (Cooldown / TimedCooldownDuration / IsCancelled)
- NotImplementedException 解消、map deactivate で apply、map activate 前に decrement
- HasCooldown バグ修正 (.Second > 0 → > DateTime.UtcNow)
- Fable レビュー Critical 3件 + Warning 5件 修正済

### ~~2c. Nomination Cooldown (新軸)~~ ✅ 完了 (2026-06-12)

- count + timed 2軸 (TOML: NominationCooldown / NominationCooldownDateTime)
- nominated マップのみ対象、投票完了時に適用 (cancel 時はロールバック不要)
- NominationCheckResult.NominationCooldownActive 追加
- Fable レビュー W3 修正: random pick 候補への誤適用 + cancel 時未ロールバック

### 2a. 永続化層 (SurrealDB) — **Wuling 側先行タスクあり**

- **方針変更 (2026-06-12)**: MCS 自前の SurrealDB 接続ではなく、**Wuling.Abstract に
  `ISurrealAccess` (Query/Execute) IF を追加**して Wuling の既存 DB 接続を共有する。
  MCS 側は NuGet 追加不要、.surql スキーマファイルのみ自前管理。
- 先行: Wuling.Abstract に IF 追加 → Wuling 側で実装
- 後続: MCS 側で repository 実装、cooldown services を DB 対応に拡張
- Hybrid write policy は設計変更なし (write-through/write-behind/cache)
- 統計保存 (旧 2d) は外部プラグインで API 経由が候補、MCS 本体のスコープ外

---

## Phase 3: 残りコマンド — ✅ ほぼ完了 (2026-06-12, commit 66cf96f)

実装済: setnextmap / removenextmap / setmapcooldown / setgroupcooldown (name-keyed,
in-memory) / reloadmapcfgs / mapinfo (groups 行 + can-nominate 付き) / extends /
thetime (issue #13, chat trigger 対応)。
MapVote 状態同期も追加: OnNextMapConfirmed/Removed を MapVote controller が listen。
revote / cancelvote は **NVM 側にあるため対象外** (ユーザー確認済 2026-06-12)。

残り:
- `OnMapInfoCommandExecuted` イベント発火 (外部プラグインが mapinfo に行を足せる —
  FireCollect 設計検討。イベント params 新設が要るので別途)
- **掃除**: `Modules/Nomination/McsMapNominationCommands.cs` (Compile Remove 中の legacy) を削除、
  csproj の `<Compile Remove>` も撤去。

## Phase 3b: MapVote 残機能 (config キーはパース済み・消費側未実装)

- `ShouldPrintVoteToChat` → 投票時の `MapVote.Broadcast.VoteCast` 系チャット表示。
- `ShouldPrintVoteRemainingTime` → 投票残り時間の表示。
- `NotEnoughMapsToStartVote` ブロードキャスト (現状 log のみ)。
- Spectator 除外時の `MapVote.Notification.SpectatorIsExcluded` 通知。
- NominationList verbose 表示 (誰が nominate したか)。
- ~~Cs2EndMatchScreen~~ ✅ 実装済 (2026-06-12): `ForceEndMatch()` = one-shot mp_timelimit/
  mp_maxrounds=1 書込み (map end で復元) + TerminateRound → cs_intermission フックで転換。

## Phase 3c: Workshop 連携 (旧 WorkshopSync + AutoFixMapWorkshopId モジュール移植)

- **WorkshopSync**: `WorkshopCollectionIds` (パース済・未消費) → Steam collection ページを
  HTTP fetch → 収録マップの map config を自動生成 (自動 config gen)。
  旧: `Modules/WorkshopSync/McsWorkshopMapSynchronizer.cs` (HTML scrape + Tomlyn 書き出し)。
  新実装は CsToml 書き出し + ModSharp の async/timer 事情を確認して移植。
- **AutoFixMapWorkshopId**: `ShouldAutoFixMapName` (パース済・未消費) → マップ開始時に
  実際のマップ名で config の名前/WorkshopId を補正。
- **Workshop リモートフェッチ**: `TrySetNextMap(long)` の `FailedToFetchUnknown` 解消
  (SteamApi.Net ベース、据え置き決定済み)。

---

## Phase 4: Wuling 追従の残り

- **Client prefs → ICookie**: `McsCountdownUiController` の per-player countdown UI type を
  `IWuling.Cookie` (`GetCookie<T>(steamId, key)` / `SetCookie<T>`) に乗り換え。
  facade 取得: `OnAllModulesLoaded` で
  `GetOptionalSharpModuleInterface<IWuling>(IWuling.Identity)?.Instance`。
  Optional 扱い (Wuling 不在ならメモリのみ動作に degrade) か Required かは実装時にユーザー確認。
- **Permission allow 側**: 現在 deny のみ実装 (`mcs.nominate.map.deny.*` / `group.deny.*`)。
  設計: Any Deny > Any Allow > Default。
  - Allow ノード (`mcs.nominate.map.allow.<map>` / `group.allow.<group>`) のチェックを
    `NominationValidateService` に追加。
  - 「Allow が 1 つでも定義されているマップは allow 保持者のみ nominate 可」なのか
    「Allow は deny の例外」なのか — **解決セマンティクス未確定、実装前にユーザー確認**。

---

## Phase 5: 小物

- **翻訳キー残り**: `lang/en.json`+`ja.json` は作成済。コード中の全キーと突合して
  未定義キーを埋める (grep: `LocalizeWithPluginPrefix` / `LocalizeWithModulePrefix` /
  `LocalizeString` / `ForCulture(` / `ModuleChatPrefix`)。旧 MCS ~210 キーとの差分も確認。
- **`GetFormattedTimeLeft` 翻訳対応**: "ThresholdReached" 戻り値を翻訳キー化。
- **`TryGetMapConfig` の day/time 考慮**: naive first-match を DaySettings 対応に。
- **ChatListener FakeCommand**: `client.FakeCommand("ms_rtv")` が StringCommand
  コールバックに届かない件の MS 側ルーティング調査 (未解決 TODO)。
- **Wuling IMenu compat**: `IMcsMenuCompat` の Wuling `IMenu`/`IMenuInstance` バックエンド
  (McsFPMCompat の代替プラグイン)。別プロジェクト追加になるので独立タスク。

---

## 参考: 直近の実装状態 (2026-06-12)

- Extend システム完了 (5e081f1 + fb216a5)。予算 2 軸: MaxExtends=MapVote Extend 選択肢のみ消費 /
  MaxExtCommandUses=!ext。Admin パス (TryExtendCurrentMap, !ve 可決) は無消費。
  延長は内部 TimeLimit マネージャのみ (mp_* ConVar 不変)。
- TnmsPluginFoundation は Wuling 移行済み (ProjectReference 運用、LangVersion 14)。
  `Localizer[key, culture]` 形は**コンパイルが通る罠** — 必ず `ForCulture` を使う。
- vote state はプラグイン寿命 + `McsMapVoteController.OnGameActivate` で main slot リセット。
- RTV は extend vote 中 `AnotherVoteOngoing` (OnExtendVoteStarted/Finished/Cancelled 配線済み)。
- イベント発火済み: NextMapConfirmed/Removed, McsIntermission, ExtendVote*, ExtCommandExecute,
  TimeLimitReached, VoteStartThresholdReached。未発火: OnMapCooldownApply (Phase 2),
  OnMapInfoCommandExecuted (Phase 3)。
