# Mapコンフィグのカスタマイズ

## Mapコンフィグの配置方法

このプラグインでは以下の2つの方法をサポートしています。

### 1. maps.tomlがある場合

`config/maps.toml`がある場合は、`maps.toml`から設定をロードします。

```
.
└── MapChooserSharp/
    ├── config/
    │   └── maps.toml
    └── MapChooserSharp.dll
```

### 2. xxx.tomlがある場合

一つのファイルに集約させるのではなく、以下のような形でコンフィグを分けることができます。
この方法を取る場合は、`maps.toml`はファイル名に使用しないてください。

また、最低限一つのtomlファイルは`config/`フォルダ直下に配置してください。

```
.
└── MapChooserSharp/
    ├── config/
    │   ├── ze_maps/
    │   │   └── ze_xxxxx_v1.toml
    │   ├── de_maps/
    │   │   ├── de_dust2.toml
    │   │   └── de_mirage.toml
    │   ├── surf_maps/
    │   │   ├── surf_xxxxx.toml
    │   │   └── surf_yyyyy.toml
    │   └── default.toml
    └── MapChooserSharp.dll
```

## 設定の適用順とグループについて

### マップの優先度

マップの設定優先度は `マップ > グループ > デフォルト` のようにマップの設定が一番優先度が高いです。

もし、`MinPlayers = 0`をデフォルトで設定し、グループでは`MinPlayers = 16`とし、マップの設定ではこのグループを継承し`MinPlayers = 32`と設定した場合、最終的な`MinPlayers`の値は`32`になります。

### マップの設定適用順

ここでは`ze_example_xyz`をマップコンフィグ例として解説します。

### 1. デフォルト値

まず最初に、マップコンフィグは、`[MapChooserSharpSettings.Default]`内の設定値を取得します。

この時点でのコンフィグをファイルに起こすと...

---

デフォルトファイルが次のような場合

```toml
[MapChooserSharpSettings.Default]
MinPlayers = 0
MaxPlayers = 0
OnlyNomination = false
```

以下のようになります。

```toml
[ze_example_xyz]
MinPlayers = 0
MaxPlayers = 0
OnlyNomination = false
```

### 2. グループ設定

次にMapChooserSharpではマップにグループ設定を適用することができます。

グループは`MapChooserSharpSettings.Groups.<GroupName>`で定義できます。
```toml
[MapChooserSharpSettings.Groups.Group1]
MinPlayers = 16
OnlyNomination = true
```

そして、`MapChooserSharpSettings.Groups`の後ろの`Group1`のところがグループ名となります。

```toml
[ze_example_xyz]
GroupSettings = ["Group1"]
```

### グループの設定方法

マップのグループは複数持つことができますが、一番最初のグループの設定の優先度が高くなります。


グループの使用は次のように行えます。

```toml
[ze_example_789]
GroupSettings = ["Group1", "Group2", "Group3"]
```

この際、Group設定の中ではGroup1が一番優先度が高いため、Group2やGroup3で適用された値を上書きします。

GroupはDefault設定より優先度が高いため、`MinPlayers`がデフォルトの`0`から`16`に変わります。

また、`OnlyNomination`も`false`から`true`になります。

ひとまず現時点で、プラグインにロードされたデータをコンフィグで再現すると次のようになります

```toml
[ze_example_xyz]
MinPlayers = 16
MaxPlayers = 0
OnlyNomination = true
```

### 3. マップ設定

次にプラグインはマップのコンフィグデータを読み取ります。

以下のような定義があったとします。

```toml
[ze_example_xyz]
MinPlayers = 32
```

そうなると、前述の通り優先度はマップが一番高いため、最終的にプラグインにロードされたデータをコンフィグで再現すると次のようになります。

```toml
[ze_example_xyz]
MinPlayers = 32
MaxPlayers = 0
OnlyNomination = true
```

### 4. 一部の例外

Extra設定は上書きではなく統合されます。

---

例えば、以下のようなコンフィグがあったとします。

グループ設定1

```toml
[MapChooserSharpSettings.Groups.Group1]
MinPlayers = 0

[MapChooserSharpSettings.Groups.Group1.extra.shop]
cost = 100
```
グループ設定2

```toml
[MapChooserSharpSettings.Groups.Group2]
MinPlayers = 32

[MapChooserSharpSettings.Groups.Group2.extra.AnotherShop]
cost = 999
```

マップ設定

```toml
[ze_example_xyz]
MinPlayers = 40
GroupSettings = ["Group1", "Group2"]

[ze_example_xyz.extra.ExternalShop]
cost = 10000
```

これらは最終的に以下のような形になります。

```toml
[ze_example_xyz]
MinPlayers = 40

[ze_example_xyz.extra.ExternalShop]
cost = 10000

[ze_example_xyz.extra.AnotherShop]
cost = 999

[ze_example_xyz.extra.shop]
cost = 100
```

### 4-2. クールダウンについて

クールダウンの設定は、マップとグループで別の扱いになっており、別々に適用されます。

例えば、`Group1`のクールダウンが`15`で、`ze_example_xyz`のクールダウンが`20`だったとします。

そして、`ze_example_xyz`と`ze_example_abc`が同じグループを持っていたとします。

```toml
[ze_example_xyz]
Cooldown = 20
GroupSettings = ["Group1"]

[ze_example_abc]
Cooldown = 0
GroupSettings = ["Group1"]

[MapChooserSharpSettings.Groups.Group1]
Cooldown = 15
```

そうなると、`ze_example_xyz`がプレイされた際は、グループに対して`15`のクールダウンと、マップに対して`20`のクールダウンが別々に適用されます。

そして、このグループのクールダウンは、他の同一グループに所属しているマップにも適用され、この場合は`ze_example_abc`もグループクールダウンの影響を受けるため、実質的に`15`のクールダウンを持つことになります。

### 4-3. CooldownOverrideについて

グループ設定では`CooldownOverride`を指定できます。この値はマップの`Cooldown`よりも優先度が高く、グループに所属するマップのクールダウンを強制的に上書きします。

```toml
[MapChooserSharpSettings.Groups.Group1]
Cooldown = 15
CooldownOverride = 30

[ze_example_xyz]
Cooldown = 20
GroupSettings = ["Group1"]
```

この場合、`ze_example_xyz`のマップクールダウンは`CooldownOverride`により`30`となります。マップ側で`Cooldown = 20`と指定していても、グループの`CooldownOverride`が優先されます。

`CooldownOverride`はグループの`Cooldown`（グループクールダウン）とは別の値です。グループの`Cooldown`はグループ全体に対して適用されるクールダウンですが、`CooldownOverride`はグループに所属するマップの個別クールダウンを上書きするための設定です。

なお、`CooldownOverride`はグループ専用の設定であり、マップ設定では使用できません。

---

## DaySettings（オーバーライド設定）

`DaySettings`を使うと、特定の曜日・時間帯に応じてマップやグループの設定値を上書きできます。

### DaySettingsの書式

`[<マップ名またはグループ名>.DaySettings.<任意の名前>]`で定義します。`<任意の名前>`はラベルであり、特別な意味は持ちません。同一のマップ/グループに対して、いくつでもオーバーライドを定義できます。

### マップレベルのDaySettings

```toml
[ze_example_abc.DaySettings.WeekendNight]
Enabled = true
ForceOverride = false
OverridePriority = 1
TargetDays = ["saturday", "sunday"]
TargetTimeRanges = ["18:00-03:00"]

# 上書きしたい設定値
MaxExtends = 5
MinPlayers = 20
OnlyNomination = false

# Extra設定もDaySettings内で使用可能
[ze_example_abc.DaySettings.WeekendNight.extra.shop]
cost = 50
```

同じマップに異なるスケジュールのオーバーライドを追加する例:

```toml
[ze_example_abc.DaySettings.Weekday]
Enabled = true
ForceOverride = false
OverridePriority = 0
TargetDays = ["monday", "tuesday", "wednesday", "thursday", "friday"]

MaxPlayers = 32
MinPlayers = 5
```

### グループレベルのDaySettings

グループにもDaySettingsを定義できます。このオーバーライドは、そのグループに所属する全てのマップに適用されます。

```toml
[MapChooserSharpSettings.Groups.HardZeMap.DaySettings.WeekendAfternoon]
Enabled = true
ForceOverride = false
OverridePriority = 1
TargetDays = ["saturday", "sunday"]
TargetTimeRanges = ["14:00-18:00"]

OnlyNomination = false
MinPlayers = 10

[MapChooserSharpSettings.Groups.HardZeMap.DaySettings.WeekendAfternoon.extra.shop]
cost = 50
```

### DaySettingsの優先度

DaySettingsを含めた全体の設定優先度は以下の通りです。

```
通常時: OverrideMap > Map > OverrideGroup > Group > Default
```

マップレベルのDaySettingsはマップ設定より優先されますが、グループレベルのDaySettingsはマップ設定より低い優先度になります。これにより、マップで明示的に設定した値がグループのDaySettingsに意図せず上書きされることはありません。

`ForceOverride`が有効な場合は、通常の優先度を無視して最優先になります。

```
ForceOverride有効時: ForceOverride(priorityで順位決定) > OverrideMap > Map > OverrideGroup > Group > Default
```

### DaySettingsのプロパティ

DaySettingsセクションでは、以下のオーバーライド制御用プロパティに加えて、`Cooldown`や`MinPlayers`等の既存のマップ/グループ設定プロパティを指定できます。指定されたプロパティが、条件にマッチした際にベースの設定値を上書きします。

### Enabled

このオーバーライドを有効にするかを指定します。

### ForceOverride

有効時、通常の優先度を無視して全ての設定値を強制的に上書きします。イベント等で一時的に設定を変更したい場合に使用します。複数の`ForceOverride`がマッチした場合は、`OverridePriority`の値で順位を決定します。

### OverridePriority

同一マップ/グループに複数のオーバーライドがマッチした場合、値が大きいほうが優先されます。

### TargetDays

適用する曜日を指定します。この値は必須です。

`TargetDays = ["saturday", "sunday"]` は、土曜日と日曜日にのみこのオーバーライドが適用されます。

### TargetTimeRanges

適用する時間帯を指定します。省略した場合は終日適用されます。

`TargetTimeRanges = ["18:00-03:00"]` は、18:00から翌日03:00の間にのみこのオーバーライドが適用されます。

---

## 設定値の詳細

```toml
[ze_example_abc]
MapNameAlias = "ze example a b c"
MapDescription = "This map contains a jump scare"
IsDisabled = false
WorkshopId = 1234567891234
OnlyNomination = false
MapSelectionWeight = 1
Cooldown = 60
CooldownDateTime = "2d"
NominationCooldown = 0
NominationCooldownDateTime = ""
MaxExtends = 3
MaxExtCommandUses = 1
ExtendTimePerExtends = 15
MapTime = 20
ExtendRoundsPerExtends = 5
MapRounds = 10
MaxPlayers = 64
MinPlayers = 10
ProhibitAdminNomination = false
DaysAllowed = ["wednesday", "monday"]
AllowedTimeRanges = ["10:00-12:00", "20:00-22:00", "22:00-03:00"]
GroupSettings = ["HardZeMap"]

[ze_example_abc.extra.shop]
cost = 100
```

グループ設定の例:

```toml
[MapChooserSharpSettings.Groups.HardZeMap]
ShortGroupName = "HD"
NominationLimit = 3
CooldownOverride = 30
Cooldown = 15
MinPlayers = 16
OnlyNomination = true
```

## マップ全般

### MapNameAlias

投票画面等で表示される名前を変更できます。

### MapDescription

`!mapinfo`コマンドで表示される内容をここで指定できます。

### IsDisabled

マップが有効か否かを指定します。
ここで無効化されている場合は、マップはノミネーションできず、投票でのランダムマップ選択にも選ばれません。

### WorkshopId

ワークショップの場合はこの値を指定することで、マップ名が変わってもコンフィグを編集する必要がなくなります。

このキーを設定しなかった場合、ワークショップマップは`ds_workshop_changelevel <keyName>` 公式マップは `changelevel <keyName>` でマップ変更がなされます。

また、マップ名としてトップのキーの名前を使用します。 例: `[ze_example_abc]` の場合は `ds_workshop_changelevel ze_example_abc` になります。

### OnlyNomination

ノミネート限定にするか否かを指定します。
ここで有効化された場合、投票でのランダムマップ選択にも選ばれません。

### MapSelectionWeight

ランダムマップ選択時の重み付けです。値が大きいほど選ばれやすくなります。デフォルトは `1` です。`0` にすると選ばれなくなります（`OnlyNomination` と同等）。

### Cooldown

マップがプレイされた後に適用されるクールダウンを指定します。
グループクールダウンとはまた別のため注意してください。

### CooldownOverride（グループ専用）

グループ設定でのみ使用できます。この値が指定された場合、グループに所属するマップの`Cooldown`をこの値で強制的に上書きします。マップ側の`Cooldown`設定よりも優先度が高くなります。

この値はグループの`Cooldown`（グループクールダウン）とは別です。グループの`Cooldown`はグループ全体に適用されるクールダウンであり、`CooldownOverride`はマップの個別クールダウンを上書きするための設定です。

### CooldownDateTime

通常のクールダウンに加えて適用される、時間ベースのクールダウンです。指定した時間が経過するまでクールダウン状態が解除されません。

使用できるサフィックス: `"h"`（時間）、`"d"`（日）、`"w"`（週）、`"m"`（月=30日）。適用しない場合は空欄にしてください。

例: `CooldownDateTime = "2d"` は2日間のクールダウン。

### NominationCooldown

マップがノミネーション候補として消費された後に適用されるノミネーション専用クールダウン (回数ベース) です。通常の `Cooldown` とは独立して動作します。デフォルトは `0`（無効）。

### NominationCooldownDateTime

ノミネーション専用の時間ベースクールダウンです。`CooldownDateTime` と同じサフィックスが使えます。

### MaxExtends

マップの最大延長回数を指定します。

### MaxExtCommandUses

`!ext`コマンドで行えるマップの最大延長回数を指定します。

### ExtendTimePerExtends

マップが時間ベースの場合に、延長される度に何分延長するかを指定します。

### MapTime

マップが時間ベースの場合に、マップの時間を指定します。

### ExtendRoundsPerExtends

マップがラウンドベースの場合に、延長される度に何ラウンド延長するかを指定します。

### MapRounds

マップがラウンドベースの場合に、マップのラウンド数を指定します。

## ノミネート関連

### MaxPlayers

サーバー内の人数がこの数値より大きい場合ノミネートができなくなります。

### MinPlayers

サーバー内の人数がこの数値より小さい場合ノミネートができなくなります。

### ProhibitAdminNomination

`!nominate_addmap` でのノミネートを禁止します。

なお、コンソールからのノミネートは禁止しません。

### DaysAllowed

ここに記載された日付のみノミネートを可能にします。 また、AllowedTimeRangesと同時に適用されるため日付の指定にはご注意ください。

`DaysAllowed = ["wednesday", "monday"]` は、水曜日と月曜日にのみノミネートが可能になります。

### AllowedTimeRanges

ここに記載された時間帯のみノミネートを可能にします。 また、DaysAllowedと同時に適用されるため時間帯の指定にはご注意ください。

`AllowedTimeRanges = ["10:00-12:00", "22:00-03:00"]` は 10:00 - 12:00 か 22:00 - 03:00 の間にのみノミネートが可能になります。

## グループ専用設定

以下の設定はグループ設定 (`[MapChooserSharpSettings.Groups.<GroupName>]`) でのみ使用できます。

### ShortGroupName

投票画面等でグループ名の短縮表示に使用されるタグです。最大4文字。

例: `ShortGroupName = "HD"`

### NominationLimit

このグループからノミネートできるマップの最大数です。`0` の場合は無制限です。デフォルトは `0`。

グループごとに異なる制限を設定できます。例えば、HardGroup は最大2マップ、EasyGroup は最大5マップという設定が可能です。

```toml
[MapChooserSharpSettings.Groups.HardGroup]
NominationLimit = 2

[MapChooserSharpSettings.Groups.EasyGroup]
NominationLimit = 5
```

### Extra 設定

[MCS API ドキュメント](../development/USING_MCS_API.md) を確認してください。
