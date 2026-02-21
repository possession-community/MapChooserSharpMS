# ExtraConfigAccessor 設計まとめ

## 概要

```
┌─────────────────────────────────────────────────────┐
│  TOML設定ファイル                                    │
│                                                     │
│  [MapChooserSharpSettings.Default.extra.shop]       │
│  cost = 100                                         │
│  discount = 0.1                                     │
│                                                     │
│  [ze_series.extra.shop]      # Group                │
│  cost = 150                  # Override             │
│                                                     │
│  [ze_example.extra.shop]     # Map                  │
│  discount = 0.2              # Override             │
└─────────────────────┬───────────────────────────────┘
                      │
                      │ Tomlyn (型推論済み)
                      ▼
┌─────────────────────────────────────────────────────┐
│  ExtraConfigBuilder                                 │
│  .Merge(default) → .Merge(group) → .Merge(map)      │
│                                                     │
│  解決順: Default → Group → Map (後勝ち)              │
└─────────────────────┬───────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────┐
│  Dictionary<string, Dictionary<string, object>>     │
│  マージ済みデータ                                    │
│                                                     │
│  shop.cost = 150      (Groupで上書き)               │
│  shop.discount = 0.2  (Mapで上書き)                 │
└─────────────────────┬───────────────────────────────┘
                      │ Build()
                      ▼
┌─────────────────────────────────────────────────────┐
│  IExtraConfigAccessor                               │
│  Consumer向け型安全API                               │
└─────────────────────┬───────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────┐
│  Consumer (API利用者)                                │
│  int cost = accessor.GetValue<int>("shop", "cost", 0)│
└─────────────────────────────────────────────────────┘
```

---

## Interface定義（Shared）

```csharp
namespace MapChooserSharpMS.Shared.MapConfig;

public interface IExtraConfigAccessor
{
    /// <summary>
    /// 値を取得。存在しない or 変換失敗時はdefaultValueを返す
    /// </summary>
    T GetValue<T>(string section, string key, T defaultValue = default);

    /// <summary>
    /// 値の取得を試みる
    /// </summary>
    bool TryGetValue<T>(string section, string key, out T value);

    /// <summary>
    /// セクションが存在するか
    /// </summary>
    bool HasSection(string section);

    /// <summary>
    /// キーが存在するか
    /// </summary>
    bool HasKey(string section, string key);

    /// <summary>
    /// セクション内の全キーを取得
    /// </summary>
    IReadOnlyCollection<string> GetKeys(string section);

    /// <summary>
    /// 全セクション名を取得
    /// </summary>
    IReadOnlyCollection<string> GetSections();

    /// <summary>
    /// 配列を取得
    /// </summary>
    IReadOnlyList<T> GetArray<T>(string section, string key);
}
```

---

## Builder（Internal）

```csharp
using Tomlyn.Model;

namespace MapChooserSharpMS.Modules.MapConfig;

internal sealed class ExtraConfigBuilder
{
    private readonly Dictionary<string, Dictionary<string, object>> _data = new();

    /// <summary>
    /// TomlTableからマージ（後勝ち）
    /// </summary>
    public ExtraConfigBuilder Merge(TomlTable? extraTable)
    {
        if (extraTable is null)
            return this;

        foreach (var (sectionName, sectionValue) in extraTable)
        {
            if (sectionValue is not TomlTable sectionTable)
                continue;

            if (!_data.TryGetValue(sectionName, out var existingSection))
            {
                existingSection = new Dictionary<string, object>();
                _data[sectionName] = existingSection;
            }

            foreach (var (key, value) in sectionTable)
            {
                existingSection[key] = value;
            }
        }

        return this;
    }

    /// <summary>
    /// 別のAccessorからマージ
    /// </summary>
    public ExtraConfigBuilder Merge(IExtraConfigAccessor? other)
    {
        if (other is not ExtraConfigAccessor accessor)
            return this;

        foreach (var section in accessor.GetSections())
        {
            if (!_data.TryGetValue(section, out var existingSection))
            {
                existingSection = new Dictionary<string, object>();
                _data[section] = existingSection;
            }

            foreach (var key in accessor.GetKeys(section))
            {
                if (accessor.TryGetRaw(section, key, out var value))
                {
                    existingSection[key] = value;
                }
            }
        }

        return this;
    }

    public IExtraConfigAccessor Build()
    {
        return new ExtraConfigAccessor(_data);
    }
}
```

---

## Accessor実装（Internal）

```csharp
using Tomlyn.Model;

namespace MapChooserSharpMS.Modules.MapConfig;

internal sealed class ExtraConfigAccessor : IExtraConfigAccessor
{
    private readonly Dictionary<string, Dictionary<string, object>> _data;

    public static ExtraConfigAccessor Empty { get; } = new(new());

    internal ExtraConfigAccessor(Dictionary<string, Dictionary<string, object>> data)
    {
        _data = data;
    }

    public T GetValue<T>(string section, string key, T defaultValue = default!)
    {
        if (TryGetValue<T>(section, key, out var value))
            return value;

        return defaultValue;
    }

    public bool TryGetValue<T>(string section, string key, out T value)
    {
        value = default!;

        if (!TryGetRaw(section, key, out var rawValue))
            return false;

        return TryConvert(rawValue, out value);
    }

    internal bool TryGetRaw(string section, string key, out object value)
    {
        value = default!;

        if (!_data.TryGetValue(section, out var sectionData))
            return false;

        return sectionData.TryGetValue(key, out value!);
    }

    public bool HasSection(string section) => _data.ContainsKey(section);

    public bool HasKey(string section, string key)
    {
        return _data.TryGetValue(section, out var sectionData)
            && sectionData.ContainsKey(key);
    }

    public IReadOnlyCollection<string> GetKeys(string section)
    {
        return _data.TryGetValue(section, out var sectionData)
            ? sectionData.Keys.ToList()
            : Array.Empty<string>();
    }

    public IReadOnlyCollection<string> GetSections() => _data.Keys.ToList();

    public IReadOnlyList<T> GetArray<T>(string section, string key)
    {
        if (!TryGetRaw(section, key, out var rawValue))
            return Array.Empty<T>();

        if (rawValue is not TomlArray array)
            return Array.Empty<T>();

        try
        {
            return array
                .Select(item => (T)Convert.ChangeType(item, typeof(T))!)
                .ToList();
        }
        catch
        {
            return Array.Empty<T>();
        }
    }

    private static bool TryConvert<T>(object rawValue, out T value)
    {
        value = default!;

        try
        {
            if (rawValue is T directMatch)
            {
                value = directMatch;
                return true;
            }

            value = (T)Convert.ChangeType(rawValue, typeof(T));
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

---

## IBaseMapConfigの変更

```csharp
public interface IBaseMapConfig
{
    // ... 他のプロパティ

    // Before
    // Dictionary<string, Dictionary<string, string>> ExtraConfiguration { get; }

    // After
    IExtraConfigAccessor ExtraConfiguration { get; }
}
```

---

## 使用例（パース時）

```csharp
// Config解決時にマージ
var builder = new ExtraConfigBuilder()
    .Merge(defaultConfig.ExtraTable)   // Default
    .Merge(groupConfig.ExtraTable)     // Group (Override)
    .Merge(mapConfig.ExtraTable);      // Map (Override)

IExtraConfigAccessor mergedAccessor = builder.Build();
```

---

## Consumer側の使用例

```csharp
// 基本的な取得
int cost = config.ExtraConfiguration.GetValue<int>("shop", "cost", 0);
bool enabled = config.ExtraConfiguration.GetValue<bool>("shop", "enabled", true);
string name = config.ExtraConfiguration.GetValue<string>("shop", "name", "Default");

// 存在チェック付き
if (config.ExtraConfiguration.TryGetValue<double>("shop", "discount", out var discount))
{
    ApplyDiscount(discount);
}

// セクション存在チェック
if (config.ExtraConfiguration.HasSection("shop"))
{
    // shop機能が有効
}

// 配列取得
var itemIds = config.ExtraConfiguration.GetArray<int>("rewards", "item_ids");

// デバッグ用
var sections = config.ExtraConfiguration.GetSections();
var keys = config.ExtraConfiguration.GetKeys("shop");
```

---

## Consumer側で拡張メソッドを作る場合

```csharp
// Consumer（外部プラグイン）が定義
public static class ShopConfigExtensions
{
    public static int GetShopCost(this IExtraConfigAccessor accessor)
        => accessor.GetValue<int>("shop", "cost", 0);

    public static bool IsShopEnabled(this IExtraConfigAccessor accessor)
        => accessor.GetValue<bool>("shop", "enabled", true);

    public static bool HasShopConfig(this IExtraConfigAccessor accessor)
        => accessor.HasSection("shop");
}

// 使用
int cost = config.ExtraConfiguration.GetShopCost();
```

---

## Tomlynの型対応表

| TOML | Tomlyn内部型 | GetValue<T>で使える型 |
|------|--------------|----------------------|
| `cost = 100` | `long` | `int`, `long`, `short`, `byte` |
| `rate = 1.5` | `double` | `double`, `float` |
| `enabled = true` | `bool` | `bool` |
| `name = "Shop"` | `string` | `string` |
| `items = [1, 2]` | `TomlArray` | `GetArray<int>()` |

---

## 責任境界

```
┌─────────────────────────────────────────────┐
│  MapChooserSharpMS (あなたの責任)            │
│  ─────────────────────────────────────────  │
│  ✓ 型安全に取り出せる仕組みを提供            │
│  ✓ Default → Group → Map のマージ処理       │
│  ✓ 存在チェックAPI                          │
│  ✓ デフォルト値サポート                      │
│  ✓ Tomlynの型変換を隠蔽                     │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│  Consumer (API利用者の責任)                  │
│  ─────────────────────────────────────────  │
│  ✗ セクション名/キー名の設計                 │
│  ✗ 型の整合性（intで定義したらintで取る）     │
│  ✗ デフォルト値の設計                        │
│  ✗ 便利な拡張メソッドを作るかどうか          │
└─────────────────────────────────────────────┘
```
