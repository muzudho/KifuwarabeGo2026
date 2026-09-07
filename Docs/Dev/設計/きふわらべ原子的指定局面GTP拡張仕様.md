# きふわらべ原子的指定局面GTP拡張仕様

## 目的

標準GTPの `play` では表現しにくい黒白混在局面や、途中状態では合法でない編集局面を、実対局盤を壊さず一括設定する。

本拡張はKifuwarabeGo2026独自仕様であり、標準GTPではない。対応エンジンは全コマンドを `list_commands` に含め、`known_command` へ正しく回答する。

## トランザクション

指定局面は準備盤で組み立て、`kfw-commit-position` が成功した瞬間だけ実対局盤へ反映する。

- `kfw-begin-position` より前の実対局盤を、処理中に変更しない。
- `kfw-add-black` と `kfw-add-white` は石の合法着手ではなく、空点への直接配置である。
- 呼吸点、自殺、劫、手順、連続する同色着手は検査しない。
- 盤外、重複、引数不正、未開始状態などのエラーでは、準備盤を破棄する。
- `kfw-commit-position` の検査または反映に失敗した場合も、準備盤を破棄し、実対局盤を変更しない。
- `kfw-abort-position` は準備盤を破棄する。準備中でなくても成功するため、回復処理から安全に呼べる。
- 準備中の `boardsize`、`clear_board`、`play`、`genmove`、`cgos-genmove_analyze` はエラーにする。実対局盤と準備盤は変更しない。
- `name`、`version`、`known_command`、`list_commands` など盤を変更しない照会は準備中も利用できる。

## コマンド

### `kfw-begin-position`

```text
kfw-begin-position
```

現在の盤サイズと同じ空の準備盤を新規作成する。既に準備中ならエラーにし、既存の準備盤を破棄する。

### `kfw-add-black <vertex>`

```text
kfw-add-black D4
```

準備盤の空点へ黒石を直接配置する。準備中でない、座標不正、盤外、既に石がある場合はエラーとなり、準備盤を破棄する。

### `kfw-add-white <vertex>`

`kfw-add-black` と同じ規則で白石を配置する。

### `kfw-set-to-play <color>`

```text
kfw-set-to-play white
```

開始手番を `black`、`white`、`b`、`w` のいずれかで指定する。指定はcommit前に必須である。不正値では準備盤を破棄する。

### `kfw-commit-position`

```text
kfw-commit-position
```

準備盤と開始手番が揃っていれば、実対局盤を準備盤へ原子的に置き換え、劫点を解除する。成功後は準備状態を終了する。

### `kfw-abort-position`

```text
kfw-abort-position
```

準備盤を破棄する。実対局盤は変更しない。準備中でない場合も成功する。

## 正常例

```text
boardsize 19
komi 6.5
clear_board
kfw-begin-position
kfw-add-black D4
kfw-add-white Q16
kfw-set-to-play black
kfw-commit-position
```

## エラー例

```text
kfw-begin-position
kfw-add-black D4
kfw-add-white D4
```

2回目の配置は重複座標なのでエラーとなり、準備盤全体を破棄する。`kfw-begin-position` より前の実対局盤は変化しない。

v2.9.0以前の `begin_position`、`add_black`、`add_white`、`set_to_play`、`commit_position`、`abort_position` は、v3.0.0でも互換エイリアスとして受け付ける。`list_commands` には `kfw-` 付きの正式名だけを掲載する。

## バージョン方針

現在はコマンド集合そのものを仕様バージョン1として扱う。将来、引数や原子性を変更する場合は既存コマンドの意味を上書きせず、新しい能力確認方法または別コマンド名を追加する。
