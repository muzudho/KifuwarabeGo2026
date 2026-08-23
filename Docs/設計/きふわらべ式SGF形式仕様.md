# きふわらべ式SGF形式仕様

## 目的

きふわらべ式SGF形式は、Smart Game Format FF[4] を基礎とする KifuwarabeGo2026 の拡張形式です。

最初の拡張として、CGOS の `cgos-genmove_analyze` で送受信した評価値と読み筋を、SGF 棋譜へ保存して再読込できるようにします。

SGF FF[4] の通常の着手やコメントとの互換性を保ち、未対応の SGF アプリケーションでも棋譜そのものは利用できる形式にします。

## プロパティ

### `KFAV`

ルートノードへ記録する KifuwarabeGo2026 Analysis Format Version です。

```sgf
KFAV[1]
```

解析情報を一つ以上保存するときだけ出力します。現在のバージョンは `1` です。

### `KFW`

解析対象となる着手ノードへ記録する KifuwarabeGo2026 固有データです。`KFW` は独自拡張 GTP コマンドの `kfw-` 接頭辞と揃えた名前です。値は改行なしの UTF-8 JSON とし、SGF の通常のエスケープ規則を適用します。

JSON のキーと意味は、zakki/cgos の Proposed GTP tournament game expansion ver 0.1 に合わせます。

```sgf
(;FF[4]GM[1]CA[UTF-8]AP[KifuwarabeGo2026]KFAV[1]SZ[9]KM[7]
;B[dd]KFW[{"moves":[{"move":"D6","winrate":0.532,"score":1.5,"pv":"E5 F3","visits":100}]}])
```

## JSON version 1

`moves` 配列には、そのSGFノードで実際に打たれた手を1件保存します。

| キー | 型 | 必須 | 意味 |
|---|---|---:|---|
| `move` | string | はい | 実際の着手。GTP座標または `pass` |
| `winrate` | number | いいえ | 着手したエンジン視点の勝率。`0.0`以上`1.0`以下 |
| `score` | number | いいえ | 着手したエンジン視点の予測得点差 |
| `pv` | string | いいえ | 実際の着手を除いた読み筋。GTP座標を半角空白で区切る |
| `visits` | integer | いいえ | 探索回数。0以上 |

値が取得できなかったキーは出力しません。`moves` の候補手を複数保存する拡張、`prior`、`ownership`、`comment` は、内部棋譜モデルが対応した将来のバージョンで追加できます。

## コメントとの関係

人間が記入した文章は標準の `C` プロパティへ保存します。機械的に再利用する解析値は `KFW` へ保存し、コメントを上書きしません。

未対応アプリケーションは `KFW` と `KFAV` を無視して構いません。対応アプリケーションは、認識できない版の解析情報を無理に解釈せず、通常のSGF棋譜として読み込みます。

## 旧 `KFA` との互換性

旧仕様の `KFA` は読み込みに対応します。保存時には `KFA` を出力せず、解釈できる解析 JSON は共通の `CC`、未解釈の JSON は値を保持したまま `KFW` として出力します。`ConvertKfaToKfw` を使うと、コメントや JSON 内の文字列には触れず、SGF 全体の `KFA` プロパティ名だけを `KFW` へ変換できます。

## 参照仕様

- zakki/cgos Wiki, Proposed GTP tournament game expansion ver 0.1
- Smart Game Format FF[4]
