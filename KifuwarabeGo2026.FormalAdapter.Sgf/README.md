# KifuwarabeGo2026.FormalAdapter.Sgf

SGFとゲームオアシスの棋譜・局面契約を接続するフォーマル・アダプターのProjectFamily入口です。画面や対局状態へ依存しない文書解析、書出し、意味変換から段階的に集約します。

`Document`名前空間には、ゲーム固有の意味を適用しないSGF Collectionモデルがあります。

* `SgfDocument`は複数のゲーム木を順序どおり保持します。
* `SgfGameTree`はノード列と、その後から分岐する変化図を保持します。
* `SgfNode`はプロパティ順を保持します。
* `SgfProperty`は未知の識別子を含め、複数値とその順序を保持します。
* `SgfDocumentParser`と`SgfDocumentWriter`は、GUI、Go型、ファイルシステムに依存せず、SGFのエスケープとCollection全体を往復します。

現行GUIは薄い互換入口から`FormalAdapter.Sgf.Go`の中立棋譜へ変換します。GUI固有棋譜との写像だけをGUI側に残し、SGF字句、座標、プロパティ組立ては所有しません。

`Go`名前空間は、共有の`GoPoint`と`GoStone`だけに依存する囲碁向け境界です。`SgfCoordinate`、中立な`SgfGoGameRecord`、セットアップ石、着手・パス、持ち時間、コメント、解析JSON原文を扱います。`SgfGoGameRecordConverter`は最初のゲーム木の主手順を明示的に中立棋譜へ縮約し、文書モデルそのものに保持された変化図を破壊しません。
