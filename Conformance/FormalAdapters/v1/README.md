# Formal Adapter baseline vectors v1

`FormalAdapter`への移行前に固定した、GTP、CGOS、SGFの匿名化回帰ベクトルです。

* `gtp-baseline.json`：ファイルパス引数、座標、パス、不正入力。
* `cgos-baseline.json`：helpの必須項目、ログイン、setup、生成着手、play、gameover、人間着手、投了、終了。
* `sgf-baseline.sgf`：対局情報、初期配置、着手、パス、残り時間、コメント、解析情報。
* `sgf-legacy-kfa.sgf`：旧`KFA`プロパティから`KFW`への名前更新と未知JSON保持。

実在する資格情報や対局者名は含めません。仕様移行時は期待結果を新実装へ合わせて書き換えず、意図的な互換性変更として別バージョンのベクトルを追加します。

