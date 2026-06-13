# コマンド一覧

全コマンドは `ms_` プレフィックス付きで登録されます (例: `ms_nominate`)。
チャットでは `!nominate` / `css_nominate` でも使用可能です。

## プレイヤーコマンド

| コマンド | エイリアス | 説明 |
|---|---|---|
| !nominate \<map\> | - | マップをノミネートする。引数なしで全マップ一覧、部分一致で複数ヒット時はメニュー表示 |
| !nomlist | - | 現在のノミネーション一覧を表示。`!nomlist full` で管理者はノミネーター名表示 |
| !rtv | チャット "rtv" | Rock The Vote に投票する |
| !unrtv | - | RTV 投票を取り消す |
| !ext | - | 現在のマップの延長に投票する (RTV 式カウント) |
| !timeleft | チャット "timeleft" | 残り時間/ラウンドを表示 |
| !nextmap | チャット "nextmap" | 次のマップを表示 |
| !currentmap | チャット "currentmap" | 現在のマップを表示 |
| !mapinfo \<map\> | - | マップ情報を表示。引数なしで現在のマップ |
| !extends | - | 残りの延長回数を表示 |
| !thetime | チャット "thetime" | 現在のサーバー時刻を表示 |

## 管理者コマンド

| コマンド | 権限ノード | 説明 |
|---|---|---|
| !nominate_addmap \<map\> | mcs.admin.command.nomination.addmap | マップを管理者ノミネーションとして追加 |
| !nominate_removemap \<map\> | mcs.admin.command.nomination.removemap | ノミネーションからマップを削除 |
| !setnextmap \<map\> | mcs.admin.command.mapcycle.setnextmap | 次のマップを設定 |
| !removenextmap | mcs.admin.command.mapcycle.removenextmap | 次のマップ設定を解除 |
| !extend \<amount\> | mcs.admin.command.mapcycle.extend | マップの時間/ラウンドを延長 (マイナスで短縮) |
| !voteextend \<minutes\> | mcs.admin.voteextend | 延長投票 (NVM YesNo 投票) を開始 |
| !setmapcooldown \<map\> \<cd\> | mcs.admin.command.mapcycle.setmapcooldown | マップのクールダウンを設定 |
| !setgroupcooldown \<group\> \<cd\> | mcs.admin.command.mapcycle.setgroupcooldown | グループのクールダウンを設定 |
| !reloadmapcfgs | mcs.admin.command.mapconfig.reload | マップ config をリロード |
| !forcertv | mcs.admin.command.rtv.forcertv | 強制的に RTV を発動 |
| !enablertv | mcs.admin.command.rtv.enablertv | RTV を有効化 |
| !disablertv | mcs.admin.command.rtv.disablertv | RTV を無効化 |
| !enableext | mcs.admin.command.mapcycle.enableext | !ext を有効化 |
| !disableext | mcs.admin.command.mapcycle.disableext | !ext を無効化 |
| !setext \<count\> | mcs.admin.command.mapcycle.setext | !ext の残り回数を設定 |

## 権限ノード (Nomination)

| ノード | 説明 |
|---|---|
| mcs.nominate.map.deny.\<map\> | 特定マップのノミネートを拒否 (exact match) |
| mcs.nominate.group.deny.\<group\> | 特定グループのノミネートを拒否 (exact match) |
| mcs.admin.command.nomination.nomlist.verbose | !nomlist full でノミネーター名表示 |
