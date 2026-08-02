# ポン抜きPlayer実装リファレンス

## 1. Playerの責務

`ponnuki player`は、GUIから渡された盤面と手番を受け取り、標準GTPの`genmove`などを使って合法な着手を生成します。

Playerは初期局面の生成、アゲハマの集計、ポン抜き固有の終局判定を担当しません。これらはProviderとGUIの責務です。

> **引用用要件：ポン抜きPlayer**  
> ポン抜きPlayerは与えられた局面から着手を生成する。`kfw-listen-move`はProvider向けの通知であり、Playerの必須コマンドではない。

## 2. 指定局面への対応

ポン抜きでは、通常の空盤開始とは異なる初期配置が渡されます。Playerは次の原子的指定局面コマンド一式へ対応してください。

```text
kfw-begin-position
kfw-add-black <vertex>
kfw-add-white <vertex>
kfw-set-to-play <black|white>
kfw-commit-position
kfw-abort-position
```

対応する全コマンドを`known_command`と`list_commands`で公開します。一部だけへ対応したエンジンは、原子的指定局面へ対応しているとは見なしません。

## 3. 受信例

```text
kfw-begin-position
kfw-add-black D4
kfw-add-black E4
kfw-add-white D5
kfw-add-white E5
kfw-set-to-play black
kfw-commit-position
```

`kfw-add-black`と`kfw-add-white`は着手ではなく、準備盤の空点へ石を直接配置するコマンドです。コウ、交互着手、直前の手などを仮定してはいけません。

`kfw-commit-position`が成功した時点で、準備盤を実対局盤として採用します。その後は標準GTPの`play`と`genmove`で対局を進めます。

## 4. 原子性

`kfw-begin-position`を受信したら、現在の実対局盤とは別に準備盤を作ります。準備中は、石の追加や手番指定によって実対局盤を変更してはいけません。

次のいずれかが起きた場合、準備盤全体を破棄し、`kfw-begin-position`より前の実対局盤を維持します。

- 不正な座標を受け取った。
- 同じ交点へ複数の石を配置しようとした。
- 必須の手番指定がないままcommitされた。
- commit時の検証または反映に失敗した。
- `kfw-abort-position`を受け取った。

> **引用用要件：原子的指定局面**  
> 指定局面は準備盤で構築し、`kfw-commit-position`が成功した瞬間だけ実対局盤へ反映する。構築中のエラーまたはabortでは準備盤全体を破棄し、以前の実対局盤を変更してはならない。

## 5. 各コマンドの要点

### `kfw-begin-position`

新しい準備盤を開始します。盤サイズは、その前に成功した`boardsize`の値を使用します。

### `kfw-add-black <vertex>`

準備盤の空点へ黒石を一つ配置します。

### `kfw-add-white <vertex>`

準備盤の空点へ白石を一つ配置します。

### `kfw-set-to-play <black|white>`

指定局面を採用した直後の手番を設定します。

### `kfw-commit-position`

準備盤を検証し、成功した場合だけ実対局盤へ反映します。

### `kfw-abort-position`

準備盤を破棄します。回復処理から安全に呼べるよう、準備中でない場合も成功させます。

## 6. 実装確認表

- 原子的指定局面の全6コマンドを能力情報で公開している。
- `kfw-begin-position`以前の実対局盤を準備中に変更しない。
- 黒石と白石を交互着手ではなく直接配置できる。
- 重複座標や盤外座標をエラーにできる。
- 手番が指定されるまでcommitを成功させない。
- エラー時に準備盤全体を破棄できる。
- `kfw-abort-position`を繰り返し安全に呼べる。
- commit後に標準GTPの`play`と`genmove`を利用できる。
