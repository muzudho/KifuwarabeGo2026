# プロトコル・棋譜形式の拡張方針

## 原子的指定局面

将来、引数や原子性を変更する場合は既存コマンドの意味を上書きせず、新しい能力確認方法または別コマンド名を追加する。

[現在の第1版](../CurrentState/AtomicPositionProtocol.md)

## SGF解析情報

`moves` の候補手を複数保存する拡張、`prior`、`ownership`、`comment` は、内部棋譜モデルが対応した将来のバージョンで追加できます。

[現在の解析形式](../CurrentState/SgfAnalysisFormat.md) / [SGF編集の計画](SgfEditing.md)
