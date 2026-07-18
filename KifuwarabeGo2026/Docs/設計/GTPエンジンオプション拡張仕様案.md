# GTPエンジンオプション拡張仕様案

## この文書について

この文書は、USIの `option`・`setoption` に相当するエンジン設定機能を、KifuwarabeGo2026のGTP通信へ追加するための設計草案です。

現時点では確定仕様ではありません。最初の実装と相互運用試験を通して修正することを前提とします。

## 目的

- 思考エンジンが対応する設定項目をGUIへ通知できるようにする。
- GUIが設定項目の型に応じた入力部品を自動生成できるようにする。
- GUIから思考エンジンへ設定値を送信できるようにする。
- 設定内容をGTPエンジンプロファイルごとに保存できるようにする。
- 通常のGTP Version 2エンジンとの互換性を維持する。

この通信はGUIとローカル思考エンジンの間で使用します。CGOSサーバーへ送信するコマンドではありません。

## 参考仕様

- USIプロトコル解説: https://shogidokoro2.stars.ne.jp/usi.html
- GTP Version 2仕様: https://www.lysator.liu.se/~gunnar/gtp/gtp2-spec-draft2.pdf
- GNU Go GTP文書: https://www.gnu.org/s/gnugo/gnugo_19.html
- GNU Go GTP文書の日本語訳: https://curren-note.seesaa.net/article/398077858.html

## 基本方針

USIでは初期化中にエンジンが複数の `option` 行をGUIへ送ります。本拡張ではGTPの要求・応答形式に合わせ、GUIから問い合わせを送る方式にします。

GTP Version 2はGNU Go 3.4の実装が事実上の参照実装になっており、共通コマンド以外には私的拡張が多く存在します。現在もKataGoなどのエンジンや各GUIが、それぞれ独自の接頭辞を持つコマンドを追加しています。

本拡張は特定の思考エンジン固有の機能ではなく、GUIとエンジンの間で設定画面を構築するための機能です。この役割をコマンド名から判断できるよう、`gui_` 接頭辞を使用します。

## Version 1のコマンド

```text
gui_options
gui_getoption <id>
gui_setoption <id> [value]
```

### 対応確認

GUIはGTP標準の `known_command` を使用して、エンジンが本拡張へ対応しているか確認します。

```text
> known_command gui_options
< = true
```

`false` が返る場合、GUIはエンジン設定画面を表示しません。

現在の `KifuwarabeGo2026.Engine` には、GTP Version 2必須コマンドの `known_command` と `list_commands` がありません。本拡張の実装前に、この2コマンドを追加する必要があります。

## オプション一覧の取得

### 要求

```text
gui_options
```

### 成功応答例

応答本文にはJSONを使用します。表示名、説明、文字列、ファイルパスなどに空白が含まれても、曖昧にならず解析できるようにするためです。

```text
= {
  "version": 1,
  "options": [
    {
      "id": "use_book",
      "label": "Use Book",
      "type": "check",
      "default": true,
      "value": true
    },
    {
      "id": "threads",
      "label": "Threads",
      "type": "spin",
      "default": 1,
      "value": 4,
      "min": 1,
      "max": 32
    },
    {
      "id": "style",
      "label": "Style",
      "type": "combo",
      "default": "normal",
      "value": "normal",
      "vars": ["solid", "normal", "risky"]
    }
  ]
}
```

GTP応答は通常どおり `=` から始め、空行で終了します。

### IDと表示名

- `id` は通信と設定保存に使用する安定した識別子です。
- `label` はGUIに表示する人間向けの名前です。
- `id` は空白を含まないASCIIの英数字、アンダースコア、ハイフン、ピリオドに制限する案とします。
- `label` には空白や日本語を使用できます。

表示名を変更しても保存済み設定との対応が壊れないよう、GUIは `id` をキーとして扱います。

## オプション型

Version 1ではUSIと同じ6種類を採用します。

| type | GUI部品 | 主な項目 |
|---|---|---|
| `check` | チェックボックス | `default`, `value` |
| `spin` | 数値入力 | `default`, `value`, `min`, `max` |
| `combo` | 選択リスト | `default`, `value`, `vars` |
| `button` | 実行ボタン | 値なし |
| `string` | テキストボックス | `default`, `value` |
| `filename` | パス入力と参照ボタン | `default`, `value` |

将来追加を検討する任意項目は次のとおりです。

```json
{
  "description": "探索に使うスレッド数",
  "category": "Search",
  "requiresRestart": false,
  "readOnly": false
}
```

未知の任意項目を受信したGUIは、その項目を無視できるものとします。

## 現在値の取得

### 要求

```text
gui_getoption threads
```

### 成功応答

```text
= 4
```

### 失敗応答

```text
? unknown option: unknown_id
```

一覧応答にも現在値を含めるため、GUIの通常表示では一括取得だけで足ります。単項目取得は、デバッグや外部ツールからの確認に使用できます。

## 設定値の送信

### check

```text
gui_setoption use_book true
```

### spin

```text
gui_setoption threads 8
```

### combo

```text
gui_setoption style risky
```

### string・filename

`id` より後ろの残り全部を値として扱い、空白を含む文字列とパスを許可します。

```text
gui_setoption book_file C:\Program Files\Kifuwarabe\book.bin
```

空文字にはUSIと同じ `<empty>` を使用する案とします。

```text
gui_setoption log_directory <empty>
```

### button

`button` は値を付けずに送信します。

```text
gui_setoption clear_learning
```

### 成功応答

```text
=
```

### 検証エラー

```text
? option threads must be between 1 and 32
```

型、範囲、選択肢、パスの妥当性はエンジン側でも検証します。GUI側の入力制限だけを信用しません。

## 通信フロー案

```text
GUI                                      Engine
 |--- protocol_version ------------------>|
 |<-- = 2 --------------------------------|
 |--- known_command gui_options ---------->|
 |<-- = true -----------------------------|
 |--- gui_options ------------------------>|
 |<-- = { ... option definitions ... } ---|
 |--- gui_setoption threads 8 ------------>|
 |<-- = ----------------------------------|
 |--- boardsize 9 ------------------------>|
 |--- komi 7.0 --------------------------->|
 |--- clear_board ------------------------>|
```

## 設定可能なタイミング

Version 1では次の制約を設ける案とします。

- エンジン起動後、対局開始前に設定する。
- `genmove` の処理中には設定を変更しない。
- 対局中の変更を許可するかどうかは、将来の仕様で検討する。
- `requiresRestart` が `true` の設定は、GUIが次回起動時に適用する。
- `filename` は思考エンジンが動作するPCから参照できるパスとして解釈する。

## 設定値の保存

- GUIはGTPエンジンプロファイルごとに設定値を保存します。
- 保存キーには表示名ではなくオプションの `id` を使用します。
- エンジンが同じ設定を独自に永続化するかどうかは自由とします。
- エンジンがオプションを削除していた場合、GUIは対応しない保存値を送信しません。
- 新しいオプションが追加されていた場合、保存値がなければエンジンの `default` を使用します。

## 安全性

- `button` は学習データ消去などの破壊的操作に使われる可能性があります。
- 破壊的な `button` には確認ダイアログを表示できるメタデータの追加を検討します。
- `filename` の参照ボタンはファイル選択だけを行い、GUIがファイル内容を勝手に変更しないようにします。
- エンジンから受信した `label` や `description` は表示用文字列として扱い、コードとして実行しません。

## 実装順案

1. `KifuwarabeGo2026.Engine` に `known_command` と `list_commands` を追加する。
2. オプション定義モデルと6種類の型をエンジン側へ追加する。
3. `gui_options` を実装する。
4. `gui_getoption` と `gui_setoption` を実装する。
5. GUI側のGTPクライアントへ対応確認とJSON解析を追加する。
6. GUIへ型別の設定パネルを追加する。
7. GTPエンジンプロファイルへ設定値を保存する。
8. `check`・`spin`・`combo`・`button`・`string`・`filename` の往復試験を追加する。
9. Local対局とCGOSプレイヤー起動の両方で、対局開始前に設定が反映されることを確認する。

## 未決事項

- 拡張コマンド名を将来一般化するか。
- JSON応答を1行にするか、読みやすい複数行にするか。
- `string` の空文字を `<empty>` とするか、JSON文字列を引数として使うか。
- 対局中に変更可能なオプションを許可するか。
- オプションの表示順を配列順だけで決めるか、明示的な `order` を追加するか。
- カテゴリー表示をVersion 1へ含めるか。
- `button` の確認メッセージや危険度をメタデータ化するか。
- エンジン再起動が必要な設定を、GUIがどの時点で適用するか。

## 引き継ぎ時の注意

- これは草案であり、実装開始前にコマンド名と文字列値の表現を確定してください。
- GTP標準の要求・応答形式を維持し、一方的なUSI形式の出力を混在させないでください。
- CGOSサーバーへ本拡張コマンドを中継しないでください。
- LocalとCGOSで別実装を作らず、GUIとGTPエンジン間の共通設定処理として実装してください。
- テキストファイルはUTF-8 BOMなし、CRLFで保存してください。
