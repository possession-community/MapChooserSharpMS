# MapChooserSharpMS API ガイド

MapChooserSharpMS (以下 MCS) は、CS2 サーバー向けのマップ投票・選択プラグインです。
外部プラグインから MCS の機能を利用するには `MapChooserSharpMS.Shared` NuGet パッケージを参照します。

## セットアップ

### 1. NuGet パッケージの追加

```xml
<PackageReference Include="MapChooserSharpMS.Shared" Version="*" />
```

### 2. API の取得

ModSharp モジュールの `OnAllModulesLoaded` 内で、MCS のインターフェースを取得します。
`OnAllModulesLoaded` より前のタイミング (`OnInitialize` 等) では MCS がまだ初期化されていないため、取得できません。

```csharp
private IMapChooserSharpShared? _mcs;

protected override void OnAllModulesLoaded()
{
    _mcs = GetOptionalSharpModuleInterface<IMapChooserSharpShared>(
        "MapChooserSharpMS");

    if (_mcs is null)
    {
        Logger.LogWarning("MapChooserSharpMS が見つかりません");
        return;
    }

    // ここから MCS の API を利用可能
}
```

### 3. IMapChooserSharpShared の構成

取得した `IMapChooserSharpShared` から各サブシステムにアクセスできます。

| プロパティ | 型 | 用途 |
|---|---|---|
| `MapCycleController` | `IMapCycleController` | マップサイクル全般 (タイムリミット・クールダウン・マップ遷移) |
| `MapCycleExtendController` | `IMapCycleExtendController` | 延長の予算管理と延長投票の制御 |
| `McsNominationController` | `IMcsNominationController` | ノミネーションの操作・バリデーション・メニュー表示 |
| `McsMapVoteController` | `IMcsMapVoteController` | マップ投票の開始・キャンセル・イベント監視 |
| `McsRtvController` | `IMcsRtvController` | Rock The Vote の状態確認と操作 |
| `McsMapConfigProvider` | `IMcsMapConfigProvider` | TOML から読み込まれたマップ/グループ設定の検索 |

加えて、以下のメソッドでメニュー描画のカスタム実装を登録できます:

```csharp
_mcs.SetNominationMenuCompat(new MyNominationMenuCompat());
```

詳細は [メニュー連携](api/menu.md) を参照してください。

---

## よくある使い方

### 次のマップを外部から設定する

```csharp
var transition = _mcs.MapCycleController.MapTransitionManager;
if (transition.TrySetNextMap("ze_example_v1"))
{
    Logger.LogInformation("次のマップを ze_example_v1 に設定しました");
}
```

### 現在のノミネーション一覧を取得する

```csharp
var nominations = _mcs.McsNominationController.NominationManager.NominatedMaps;
foreach (var (mapName, data) in nominations)
{
    int count = data.NominationParticipants.Count;
    bool isAdmin = data.IsForceNominated;
    Logger.LogInformation("{Map}: {Count}人 (管理者: {Admin})", mapName, count, isAdmin);
}
```

### マップ設定を名前で検索する

```csharp
var configProvider = _mcs.McsMapConfigProvider;
if (configProvider.TryGetMapConfig("ze_example_v1", out var mapConfig))
{
    string display = configProvider.ToolingService.ResolveMapDisplayName(mapConfig);
    Logger.LogInformation("表示名: {Name}, Workshop: {Id}", display, mapConfig.WorkshopId);
}
```

### イベントを監視する

各サブシステムにはイベントリスナーを登録できます。
`McsCancellableEvent` を返すメソッドはキャンセル可能です。`Continue` で許可、`Handled` で処理済みマーク、`Stop` でアクションをキャンセルできます。

```csharp
public class MyVoteListener : IMapVoteEventListener
{
    public int ListenerPriority => 0;

    public void OnMapConfirmed(IMapVoteMapConfirmedEventParams @params)
    {
        // 投票で次のマップが決まったときに呼ばれる
        var mapName = @params.ConfirmedMap.MapName;
    }
}

// リスナーの登録 (OnAllModulesLoaded 内で)
_mcs.McsMapVoteController.InstallEventListener(new MyVoteListener());
```

### Extra 設定を活用する

TOML の `[マップ名.extra.セクション名]` に書いた値を型安全に読み取れます。
マップ固有の追加データを外部プラグインから参照する主な手段です。

```toml
# maps.toml
[ze_example_v1.extra.shop]
cost = 100
discount = 20
items = ["sword", "shield"]
```

```csharp
if (configProvider.TryGetMapConfig("ze_example_v1", out var cfg))
{
    var extra = cfg.ExtraConfiguration;

    int cost = extra.GetValue<int>("shop", "cost", 0);
    var items = extra.GetArray<string>("shop", "items");

    if (extra.HasSection("shop"))
    {
        var keys = extra.GetKeys("shop"); // ["cost", "discount", "items"]
    }
}
```

### クールダウンの詳細を問い合わせる

マップのクールダウン状態は、マップ自体のクールダウンとグループごとのクールダウンに分かれています。

```csharp
var validate = _mcs.McsNominationController.NominationValidateService;
var detail = validate.GetCooldownInformations(mapConfig);

if (detail.HasCooldown)
{
    // マップ自体のクールダウン
    int mapCd = detail.CooldownCount;
    DateTime mapTimedCd = detail.TimedCooldown;

    // グループごとのクールダウン
    foreach (var (groupName, count) in detail.GroupCooldowns)
    {
        Logger.LogInformation("グループ {Group}: 残り {Count} マップ", groupName, count);
    }
}
```

### RTV の状態を確認する

```csharp
var rtv = _mcs.McsRtvController.RtvManager;
if (rtv.RtvStatus == RtvStatus.Enabled)
{
    float ratio = rtv.RtvCompletionRatio; // 0.0 ~ 1.0
    Logger.LogInformation("RTV 進捗: {Ratio:P0} ({Current}/{Required})",
        ratio, rtv.RtvCounts, rtv.RequiredCounts);
}
```

---

## API リファレンス (モジュール別)

各モジュールの詳しいインターフェース定義は以下のページを参照してください。

| ページ | 内容 |
|---|---|
| [マップ設定](api/map-config.md) | `IMapConfig`, `IMapGroupConfig`, `INominationConfig`, `ICooldownConfig`, `IRandomPickConfig`, `IExtraConfigAccessor`, DaySettings オーバーライド |
| [ノミネーション](api/nomination.md) | `IMcsNominationController`, `IMapNominationService`, `INominationValidateService`, `IDetailedCooldownResult` |
| [マップ投票](api/map-vote.md) | `IMcsMapVoteController`, `IMapVoteControllingService`, 投票状態、投票オプション |
| [マップサイクル](api/map-cycle.md) | `IMapCycleController`, `IMapCycleExtendController`, タイムリミット、マップ遷移、クールダウン操作 |
| [Rock The Vote](api/rtv.md) | `IMcsRtvController`, `IRtvService`, `IRtvManager`, RTV ステータス |
| [イベント](api/events.md) | 全イベントリスナー、キャンセル可能イベント、編集可能イベント、イベントパラメータ一覧 |
| [メニュー連携](api/menu.md) | `IMcsNominationMenuCompat` の実装方法、`McsNominationMenuContext`, `McsNominationMenuItem` |
| [Workshop](api/workshop.md) | `IWorkshopFetchResult`, `ExistenceStatus`, Workshop ID によるマップ設定 |
