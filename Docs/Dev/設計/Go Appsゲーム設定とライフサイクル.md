# Go Appsゲーム設定とライフサイクル

## 目的

GUIはゲーム画面と共通操作を提供し、Go App固有の初期局面生成・進行・設定はProviderが所有します。画面に表示する設定項目と、エンジンが所有するオプションは区別します。

## 設定値の決定元

| 利用場面 | BoardSizeの決定元 | GAME SETTINGSでの扱い | エンジンへの通知 |
|---|---|---|---|
| Play | 大会ルール | 読み取り専用。「大会ルールとして固定」と表示 | 標準GTP `boardsize` |
| CGOS | CGOSサーバーの対局条件 | 読み取り専用。「CGOSサーバーが指定」と表示 | CGOS対局条件に従う |
| Ponnuki Provider | Providerのapp/role別オプション | 編集可能 | `kfw-patch-options ponnuki provider` |

同名の値を画面へ表示しても、所有者と編集権限は利用場面ごとに異なります。`BoardSize`を`play player`のエンジンオプションへ追加してはいけません。

## Ponnuki Providerオプション

`kfw-describe-options ponnuki provider`は、少なくとも次を公開します。

- `BoardSize`: `binding: "gtp.boardsize"`を持つ盤サイズ候補。現行Providerは9路、13路、19路に対応する。
- `InitialMoveCount`: 整数。初期値20。初期局面を作るランダム着手数。上限は`BoardSize * BoardSize / 4`。
- `RandomSeed`: 整数。初期値0。0は開始ごとに自動生成し、1以上は再現用の固定シード。

`kfw-describe-options play player`には`BoardSize`と`InitialMoveCount`を含めません。

## 標準GTPへのバインド

オプションIDはProviderが自由に命名できるため、GUIは`BoardSize`という名前だけでは盤サイズとして扱いません。標準GTPの盤サイズへ結び付く設定は、オプション定義へ次を付けます。

```json
{"id":"BoardSize","type":"enum","default":"9","values":["9","13","19"],"binding":"gtp.boardsize","apply":"next-start"}
```

離散的な対応サイズは`enum`を推奨します。連続範囲を受け付けるProviderは`type: "integer"`と`minimum`、`maximum`を使えます。GUIは`binding`を見てProviderの候補とGUI自身の対応サイズを照合します。

- 両方が対応する値だけを選択可能にする。
- Providerは対応するがGUIが対応しない値も一覧へ残し、グレー表示と理由を示す。
- 選択を確定すると標準GTP `boardsize`で実際の盤サイズを設定する。
- `boardsize`の成功応答を得るまで盤サイズは確定しない。オプション値だけを実盤サイズとして信用しない。
- 未知の`binding`は通常のProvider固有オプションとして扱い、GUIの標準機能へ勝手に結び付けない。

## Providerセッション

設定画面での変更は、まず`kfw-evaluate-options ponnuki provider <json>`へ送ります。Providerは保持中の正式値を変更せず、候補値をコピーへ適用して依存関係を評価します。応答には評価後の全値、動的な制約を含む全スキーマ、調整理由の差分を返します。GUIはその応答を画面用キャッシュとして丸ごと採用します。

［OK］は評価済みキャッシュを設定へ保存し、次回開始時の正式な`kfw-patch-options`対象にします。［CANCEL］はキャッシュを破棄するため、Providerの正式値は変わりません。

正規の開始・終了コマンドはapp-idとroleを明示します。

```text
kfw-start-app ponnuki provider
kfw-listen-move D4
kfw-listen-move pass
kfw-end-app ponnuki provider
```

開始前にGUIが保存済みオプションを`kfw-patch-options ponnuki provider`で適用します。`kfw-start-app`の成功応答は、初期局面JSONを返します。Providerは開始から終了まで一局分の状態を保持します。

状態規則は次のとおりです。

- 未開始での`kfw-listen-move`はエラー。
- 開始中の二重`kfw-start-app`はエラー。暗黙に前局を破棄しない。
- `kfw-end-app`は開始中の状態を破棄する。未開始での終了も成功とする冪等操作。
- 開始処理が失敗した場合、途中状態を採用しない。
- GUIは正常終了、開始失敗、途中離脱、例外のいずれでも終了を試みる。

## GUIの責務

- app-idとroleに対応するオプション定義を取得し、編集可能なProvider設定を描画する。
- PlayとCGOSでは、外部で決まるゲーム条件を読み取り専用で表示する。
- 結果画面の領域、座標、最小サイズ、重なり、入力判定を管理する。
- Providerが将来宣言する結果コンポーネントを、GUIが用意した共通部品と空き領域へ配置する。
- Ponnukiの初期局面生成引数を組み立てない。
- Providerから返された初期局面を検証して表示・対局へ接続する。

## Providerの責務

- app/role別オプションを定義・検証する。
- 設定値から初期局面を原子的に生成する。
- 開始中の局面、手番、アゲハマ、終局状態を保持する。
- GUIから通知された着手を順番に反映する。
- 終了時に一局分の状態を破棄する。
- 将来の結果画面拡張では、表示したい意味的なコンポーネント種別と値を宣言する。ピクセル座標や画面サイズは指定しない。

現行Ponnuki結果画面はGUI側の組み込み構成として、縮小したトレンドチャートと共通アゲハマコンポーネントを表示します。将来Provider宣言へ移行しても、Providerは例えば`captures`を要求し、GUIが皿、捕獲石、個数の描画と配置を担当します。

## 互換性

旧`kfw-make-position ponnuki 1 9 20 [seed]`は既存GUIとProviderのために残します。新GUIは`known_command kfw-start-app`と`known_command kfw-end-app`を確認し、両方に対応するProviderでは新方式を優先します。未対応Providerだけ旧`kfw-make-position`へフォールバックします。

`binding`を持たない旧スキーマの`BoardSize`は、名前が同じでも標準GTP盤サイズとは見なしません。通常のProvider固有オプションとして表示します。
