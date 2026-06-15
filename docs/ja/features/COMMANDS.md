# コマンド一覧

全コマンドは `ms_` プレフィックス付きで登録されます (例: `ms_nominate`)。
チャットでは `!nominate` / `css_nominate` でも使用可能です。

## プレイヤーコマンド

| コマンド | エイリアス | 説明 |
|---|---|---|
| !nominate \<map\> | !nom | マップをノミネートする。引数なしで全マップ一覧、部分一致で複数ヒット時はメニュー表示 |
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

| コマンド | エイリアス | 権限ノード | 説明 |
|---|---|---|---|
| !nominate_addmap \<map\> | - | mcs.admin.command.nomination.addmap | マップを管理者ノミネーションとして追加 |
| !nominate_addwsmap \<workshopId\> | - | mcs.admin.command.nomination.addwsmap | Workshop ID でマップを管理者ノミネーション追加 (config 未登録でも API フェッチ) |
| !nominate_removemap \<map\> | - | mcs.admin.command.nomination.removemap | ノミネーションからマップを削除 |
| !map \<map\> | - | mcs.admin.command.mapcycle.map | マップへ即時変更 |
| !wsmap \<workshopId\> | - | mcs.admin.command.mapcycle.wsmap | Workshop ID でマップへ即時変更 (config 未登録でも API フェッチ) |
| !setnextmap \<map/workshopId\> | - | mcs.admin.command.mapcycle.setnextmap | 次のマップを設定 (Workshop ID も可) |
| !removenextmap | - | mcs.admin.command.mapcycle.removenextmap | 次のマップ設定を解除 |
| !extend \<amount\> | - | mcs.admin.command.mapcycle.extend | マップの時間/ラウンドを延長 (マイナスで短縮) |
| !voteextend \<minutes\> | !ve | mcs.admin.command.mapcycle.voteextend | 延長投票 (NVM YesNo 投票) を開始 |
| !setmapcooldown \<map\> \<cd\> | !setmapcd | mcs.admin.command.mapcycle.setmapcooldown | マップのクールダウン (回数) を設定 |
| !setgroupcooldown \<group\> \<cd\> | !setgroupcd | mcs.admin.command.mapcycle.setgroupcooldown | グループのクールダウン (回数) を設定 |
| !setmaptcd \<map\> \<duration\> | - | mcs.admin.command.mapcycle.setmaptcd | マップの時限クールダウンを設定 (例: 2h, 3d, 1w) |
| !setgrouptcd \<group\> \<duration\> | - | mcs.admin.command.mapcycle.setgrouptcd | グループの時限クールダウンを設定 (例: 2h, 3d, 1w) |
| !forceresetmcs | - | mcs.admin.command.mapvote.forceresetmcs | MCS の全状態を強制リセット (投票/RTV/ノミネーション) |
| !reloadmapcfgs | - | mcs.admin.command.mapconfig.reloadmapcfgs | マップ config をリロード |
| !forcertv | - | mcs.admin.command.rtv.forcertv | 強制的に RTV を発動 |
| !enablertv | - | mcs.admin.command.rtv.enablertv | RTV を有効化 |
| !disablertv | - | mcs.admin.command.rtv.disablertv | RTV を無効化 |
| !enableext | - | mcs.admin.command.mapcycle.enableext | !ext を有効化 |
| !disableext | - | mcs.admin.command.mapcycle.disableext | !ext を無効化 |
| !setext \<count\> | - | mcs.admin.command.mapcycle.setext | !ext の残り回数を設定 |

## 権限ノード (Nomination)

| ノード | 説明 |
|---|---|
| mcs.nominate.map.deny.\<map\> | 特定マップのノミネートを拒否 (exact match) |
| mcs.nominate.group.deny.\<group\> | 特定グループのノミネートを拒否 (exact match) |
| mcs.admin.command.nomination.nomlist.verbose | !nomlist full でノミネーター名表示 |
