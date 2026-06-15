# メニュー連携

MCS はメニュー描画をプラグイン本体から切り離した抽象レイヤーを持っています。
サーバー運営者が好みのメニュープラグイン (FPM MenuManager、Wuling IMenu など) を選択できるよう、
MCS 内部では `McsMenuDefinition` を組み立てるだけで、実際の描画は `IMcsMenuCompat` の実装に委譲します。

---

## 全体の流れ

1. コンパニオンモジュール (例: `McsFPMCompat`) が `OnAllModulesLoaded` で `IMcsMenuCompat` の実装を生成する
2. `IMapChooserSharpShared.SetDefaultMenuCompat()` に渡して登録する
3. MCS がメニューを表示する際、登録された実装の `ShowMenu()` が呼ばれる

メニュー未登録のまま MCS がメニューを表示しようとした場合は `InvalidOperationException` がスローされます。

---

## IMcsMenuCompat

メニュー描画の抽象インターフェースです。メニュープラグインごとに 1 つの実装を用意します。

**名前空間**: `MapChooserSharpMS.Shared.Ui.Menu`

| メソッド | 説明 |
|---|---|
| `ShowMenu(IGameClient target, McsMenuDefinition menu)` | `target` に `menu` を表示する。同じクライアントに対して既にメニューが開かれている場合は、先に閉じてから新しいメニューを表示すること |
| `CloseMenu(IGameClient target)` | `target` に対して開いている MCS メニューを閉じる。メニューが開かれていない場合は何もしない |
| `Cleanup()` | 全てのキャッシュ済みメニュー状態を破棄する。プラグインのアンロードやマップ遷移時に MCS から呼び出される |

### MCS が各メソッドを呼び出すタイミング

- **ShowMenu** -- ノミネーションメニュー (`!nominate`)、管理者ノミネーションメニュー (`!nominate_addmap`)、ノミネーション削除メニュー (`!nominate_removemap`)、ノミネーション確認メニューの表示時
- **CloseMenu** -- MCS が明示的にメニューを閉じる必要がある場合 (現在は実装側の `OnSelect` コールバック内で閉じるパターンが主流)
- **Cleanup** -- プラグインのシャットダウンやマップ変更時にキャッシュをクリアする目的で呼ばれる

---

## McsMenuDefinition

メニュー 1 つ分の宣言的な定義です。MCS 内部で組み立てられ、`IMcsMenuCompat` の実装に渡されます。

**名前空間**: `MapChooserSharpMS.Shared.Ui.Menu`

| プロパティ | 型 | 説明 |
|---|---|---|
| `Title` | `string` | メニューのタイトル文字列 |
| `Items` | `IReadOnlyList<McsMenuItem>` | メニュー項目のリスト |

`Title` と `Items` はどちらも `required init` プロパティです。

---

## McsMenuItem

メニュー内の 1 行分の項目です。表示テキストは MCS 側で翻訳済みの文字列が設定されます (翻訳キーの間接参照はこのレイヤーでは行いません)。

**名前空間**: `MapChooserSharpMS.Shared.Ui.Menu`

| プロパティ | 型 | 説明 |
|---|---|---|
| `DisplayText` | `string` | 表示テキスト (翻訳済み) |
| `OnSelect` | `Action<IGameClient>?` | クライアントがこの項目を選択したときに呼ばれるコールバック。引数は `ShowMenu` に渡されたクライアントと同一。`null` の場合は選択しても何も起きない |

---

## SetDefaultMenuCompat の登録フロー

メニュー実装の登録は `IMapChooserSharpShared.SetDefaultMenuCompat(IMcsMenuCompat)` で行います。
呼び出しは `OnAllModulesLoaded` のタイミングで 1 回だけ行うのが基本です。
再度呼び出すと以前の実装が置き換えられます。

```csharp
public void OnAllModulesLoaded()
{
    var mcs = _sharedSystem.GetSharpModuleManager()
        .GetRequiredSharpModuleInterface<IMapChooserSharpShared>(
            IMapChooserSharpShared.ModSharpModuleIdentity).Instance!;

    mcs.SetDefaultMenuCompat(new MyMenuCompat());
}
```

登録された `IMcsMenuCompat` インスタンスは MCS 内部でのみ使用されます。
外部から取得する API は公開されていません。

---

## 実装例

以下は `IMcsMenuCompat` の完全な実装例です。
実際のメニュープラグインの API に合わせて描画処理を記述してください。

```csharp
using System.Collections.Generic;
using MapChooserSharpMS.Shared.Ui.Menu;
using Sharp.Shared.Objects;

public sealed class MyMenuCompat : IMcsMenuCompat
{
    // クライアントごとに現在表示中のメニューを追跡する
    private readonly Dictionary<IGameClient, object> _activeMenus = new();

    public void ShowMenu(IGameClient target, McsMenuDefinition menu)
    {
        // 既に開いているメニューがあれば閉じる
        CloseMenu(target);

        // メニュープラグインの API を使ってメニューを構築する
        // (ここでは仮のメニュービルダーで例示)
        var builder = new SomeMenuBuilder();
        builder.SetTitle(menu.Title);

        foreach (var item in menu.Items)
        {
            var onSelect = item.OnSelect;
            builder.AddItem(item.DisplayText, () =>
            {
                // メニューを閉じてからコールバックを実行する
                CloseMenu(target);
                onSelect?.Invoke(target);
            });
        }

        var built = builder.Build();
        _activeMenus[target] = built;

        // メニュープラグインの API で表示する
        SomeMenuPlugin.Display(target, built);
    }

    public void CloseMenu(IGameClient target)
    {
        if (!_activeMenus.TryGetValue(target, out var menu))
            return;

        // メニュープラグインの API で閉じる
        SomeMenuPlugin.Close(target, menu);
        _activeMenus.Remove(target);
    }

    public void Cleanup()
    {
        _activeMenus.Clear();
    }
}
```

### 実装上の注意

- `ShowMenu` が呼ばれた時点で、同じクライアントに対して前のメニューが残っている場合は先に閉じること
- `OnSelect` コールバックはゲームサーバーのメインスレッドから呼び出されることを想定している。メニュープラグインがワーカースレッドでコールバックを呼ぶ場合は、呼び出し側でメインスレッドへのディスパッチが必要になる
- `Cleanup` はプラグイン全体のライフサイクルに関わるため、ここで破棄を忘れるとマップ遷移後にメニュー状態が残留する可能性がある
