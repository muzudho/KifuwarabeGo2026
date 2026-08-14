# Player Time Usage Bar のプログラム解説

`PlayerTimeUsageBar` は、対局者ごとの持ち時間を割合と固定位置の時刻で表示します。

- USEDは、その手番が始まる前までに確定した使用時間です。
- NOWは、USEDに現在の思考時間を加えた累計時間です。
- LIMITは、持ち時間の上限です。
- 棒はUSEDを黒、USEDからNOWまでを青、NOWからLIMITまでを水色で描きます。
- 時刻は左からUSED、NOW、LIMITの3列に固定し、常に `00:00:00` 形式で表示します。
- 棒の左端には、線だけで構成した時計アイコンを表示します。

LocalMatchでは `GoAppSession` が手番の切替時にUSEDを確定し、進行中の累計をNOWとして渡します。
