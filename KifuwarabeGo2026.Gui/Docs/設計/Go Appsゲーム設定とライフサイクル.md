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

- `BoardSize`: 整数。初期値9。現行Ponnuki v1では9路盤。
- `InitialMoveCount`: 整数。初期値20。初期局面を作るランダム着手数。
- `RandomSeed`: 整数。初期値0。0は開始ごとに自動生成し、1以上は再現用の固定シード。

`kfw-describe-options play player`には`BoardSize`と`InitialMoveCount`を含めません。

## Providerセッション

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

## 互換性

旧`kfw-make-position ponnuki 1 9 20 [seed]`は既存GUIとProviderのために残します。新GUIは`known_command kfw-start-app`と`known_command kfw-end-app`を確認し、両方に対応するProviderでは新方式を優先します。未対応Providerだけ旧`kfw-make-position`へフォールバックします。

## GUIの責務

- app-idとroleに対応するオプション定義を取得し、編集可能なProvider設定を描画する。
- PlayとCGOSでは、外部で決まるゲーム条件を読み取り専用で表示する。
- Ponnukiの初期局面生成引数を組み立てない。
- Providerから返された初期局面を検証して表示・対局へ接続する。

## Providerの責務

- app/role別オプションを定義・検証する。
- 設定値から初期局面を原子的に生成する。
- 開始中の局面、手番、アゲハマ、終局状態を保持する。
- GUIから通知された着手を順番に反映する。
- 終了時に一局分の状態を破棄する。
