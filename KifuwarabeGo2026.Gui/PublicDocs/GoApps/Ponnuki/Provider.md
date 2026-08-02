# ポン抜きProvider実装リファレンス

> 対応バージョン：KifuwarabeGo2026 v3.3.0<br>
> 記事の更新日：2026-08-02

## 1. Providerの責務

`ponnuki provider`は、ポン抜きで使用する初期局面を作り、GUIから通知される着手を監視し、アゲハマ、終局、勝者を判定します。

Providerは対局者の次の着手を生成しません。着手生成はPlayerの責務です。

> **引用用要件：ポン抜きProvider**  
> ポン抜きProviderは初期局面とアプリ固有の進行判定を提供する。Playerの着手生成を代行してはならない。

## 2. 必須コマンド

Providerは次のコマンドへ対応し、`known_command`と`list_commands`で公開します。

```text
kfw-list-apps provider
kfw-make-position
kfw-listen-move
```

最初に`kfw-list-apps provider`を単体確認し、応答へ`ponnuki`が含まれることを確認します。次に`kfw-make-position`、最後に`kfw-listen-move`の順で実装すると、一段ずつ動作を確認できます。

## 3. 初期局面を作る

GUIは次の形式で初期局面を要求します。

```text
kfw-make-position ponnuki 1 9 20 [seed]
```

現在のポン抜き参照プロトコルでは、アプリIDは`ponnuki`、アプリバージョンは`1`、盤サイズは`9`です。`move-count`には局面生成で進める手数を0～200で指定します。上の例では20手進めます。`seed`は任意の32ビット符号付き整数で、同じ条件とseedから同じ局面を再現する用途に使用します。

Providerは要求を検証し、成功時はGTP成功応答の本文へJSONを返します。

```json
{
  "app": "ponnuki",
  "version": 1,
  "boardSize": 9,
  "black": ["A1", "C3"],
  "white": ["B1", "D3"],
  "toPlay": "black",
  "captures": {
    "black": 0,
    "white": 0
  },
  "seed": 12345
}
```

`black`と`white`には、生成した初期局面の石をGTP頂点表記で列挙します。`toPlay`は最初の手番です。seedを省略した場合も、Providerが選んだseedを応答へ含めます。呼び出し側はこの値を記録することで局面を再現できます。

実装者は、応答へ独自フィールドを追加できます。GUIは未知のフィールドを無視できるものとします。ただし、既存フィールドの意味を独自解釈で変更してはいけません。

## 4. 着手を通知する

対局開始後、GUIは盤上で受理された着手を順番にProviderへ通知します。

```text
kfw-listen-move D4
kfw-listen-move pass
```

座標はGTP頂点表記を使用し、パスは`pass`とします。

Providerは着手を現在の監視状態へ適用し、GTP成功応答の本文へ次の形式のJSONを返します。

```json
{
  "accepted": true,
  "gameOver": false,
  "winner": "",
  "reason": "",
  "blackCaptures": 3,
  "whiteCaptures": 2,
  "nextToPlay": "white"
}
```

現在の参照ルールでは、どちらかのアゲハマが20個に達すると終局します。終局時は`gameOver`を`true`にし、`winner`へ`black`または`white`、`reason`へ終局理由を返します。

終局していない場合、勝者と終局理由は空で構いません。終局した場合、勝者と理由を一緒に返します。

## 5. 状態遷移

Providerの一局分の状態は、次の順序で扱います。

```text
kfw-make-position
        ↓
初期局面と監視状態を作成
        ↓
kfw-listen-move を0回以上反復
        ↓
終局応答
```

新しい`kfw-make-position`が成功した時点で、以前の一局分の監視状態を置き換えます。`kfw-make-position`が失敗した場合は、途中まで作った状態を採用してはいけません。

`kfw-make-position`より先に`kfw-listen-move`を受信した場合は、GTPエラーを返します。

## 6. 実装確認表

- `known_command kfw-list-apps`が`true`を返す。
- `kfw-list-apps provider`が`ponnuki`を返す。
- `known_command kfw-make-position`が`true`を返す。
- `known_command kfw-listen-move`が`true`を返す。
- 3コマンドが`list_commands`に含まれる。
- 同じseedで局面を再現できる。
- 不正なアプリID、バージョン、盤サイズ、手数、seedをGTPエラーにできる。
- `kfw-listen-move`を順番どおり状態へ反映できる。
- アゲハマ、終局、勝者、理由をJSONで返せる。
- 診断ログが標準出力のGTP応答へ混ざらない。
