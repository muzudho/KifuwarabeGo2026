# KifuwarabeGo2026.Engine

`KifuwarabeGo2026.Engine` は、Kifuwarabe Go 2026 から起動できる GTP（Go Text Protocol）対応の囲碁思考エンジンです。

現在は合法手の中からランダムに着手する小さなエンジンですが、思考処理やエンジンオプションを改造するためのサンプルとしても利用できます。ライセンスはリポジトリ直下の [LICENSE.txt](../LICENSE.txt)（MIT License）を参照してください。

## ビルドと起動

リポジトリ直下で次のコマンドを実行します。

```powershell
dotnet build KifuwarabeGo2026.Engine/KifuwarabeGo2026.Engine.csproj
dotnet run --project KifuwarabeGo2026.Engine/KifuwarabeGo2026.Engine.csproj
```

起動後は標準入力へ GTP コマンドを入力できます。

```text
name
version
list_commands
quit
```

標準入力と標準出力はGTP通信専用です。また、標準エラー出力も診断情報やデバッグ情報の出力先として使用しないでください。囲碁GUIによっては、GTPエンジンが標準エラー出力へ何か書き込むと、内容にかかわらずエンジンエラーと判断する場合があります。

診断情報、デバッグ情報、例外の詳細を記録するときは、標準入力・標準出力・標準エラー出力のいずれにも書き込まず、エンジン専用のログファイルへ出力してください。ログファイルの保存先は、可能であれば起動引数や設定ファイルで変更できるようにしてください。

## GUIからオプションを設定する

Kifuwarabe Go 2026 では次の手順で設定します。

1. 対局準備画面またはCGOS接続画面から、GTPエンジンの選択画面を開く。
2. 対象エンジンを選び、編集画面を開く。
3. ［GUI OPTIONS］を押してエンジンオプション画面を開く。
4. `RandomMove` の［SELECT］を押す。
5. combo選択画面で値を選び、右上の［SELECT］で確定する。
6. エンジンオプション画面右上の［OK］で編集内容を確定する。
7. エンジン編集画面の［SAVE ENGINE］でプロファイルを保存する。

保存した値は、次回そのプロファイルからエンジンを起動するときに適用されます。combo候補とオプション項目が多い場合は、［PREV］［NEXT］でページを切り替えられます。

## エンジンオプションの仕組み

Kifuwarabe Go 2026 は、通常のGTPコマンドに加えて次の独自コマンドを使用します。

| コマンド | 用途 |
|---|---|
| `gui_options` | GUIが表示・検証するオプション定義をJSONで返す |
| `gui_getoption <id>` | 指定オプションの現在値を返す |
| `gui_setoption <id> <value>` | 指定オプションへ値を設定する |

対応コマンドは `list_commands` の結果へ含め、`known_command gui_options` に `true` を返してください。`gui_options` に対応していないエンジンは、従来のGTPエンジンとしてそのまま利用できます。

GUIに保存されたオプションはエンジン起動時に送信されます。概略は次の順序です。

1. GUIが `known_command gui_options` を送る。
2. 対応していれば `gui_options` を送る。
3. GUIがJSONのバージョンと保存値を検証する。
4. GUIが保存済みの各値を `gui_setoption <id> <value>` で送る。
5. その後、通常の対局初期化を行う。

## `gui_options` のJSON形式

現在の形式のバージョンは `1` です。このエンジンは次のようなJSONを1行で返します。

```json
{
  "version": 1,
  "options": [
    {
      "id": "RandomMove",
      "label": "RandomMove",
      "type": "combo",
      "default": "ChebyshevDistanceFromStar",
      "value": "ChebyshevDistanceFromStar",
      "vars": [
        "Normal",
        "ChebyshevDistanceFromStar"
      ]
    }
  ]
}
```

各フィールドの意味は次のとおりです。

| フィールド | 意味 |
|---|---|
| `id` | GTPコマンドで使用する一意な識別子。空白を含めないことを推奨 |
| `label` | GUIに表示する名前 |
| `type` | オプションの型。現在のGUI画面で編集できる型は `combo` |
| `default` | 既定値 |
| `value` | エンジン起動時点の現在値 |
| `vars` | `combo` で選択できる値の配列 |

`combo` の保存値が `vars` に存在しない場合、GUIはその値をエンジンへ送信しません。値の名前を変更するときは、既存設定との互換性に注意してください。

## 現在の `RandomMove` オプション

| 値 | 動作 |
|---|---|
| `Normal` | 合法手全体からランダムに選ぶ |
| `ChebyshevDistanceFromStar` | 星からのチェビシェフ距離を使った領域選択を行う |

GTPで直接確認できます。

```text
gui_options
gui_getoption RandomMove
gui_setoption RandomMove Normal
gui_getoption RandomMove
```

成功応答の例です。

```text
= ChebyshevDistanceFromStar

=

= Normal
```

不明なIDや不正な値には、`?` で始まるGTPエラーを返してください。

```text
? option RandomMove must be Normal or ChebyshevDistanceFromStar
```

## comboの候補を追加する

`RandomMove` へ候補を追加する場合は、エンジンとGUIの候補配列を同時に更新します。

エンジン側では、主に [Program.cs](./Program.cs) の次の箇所を変更します。

1. `RandomMoveKind` に値を追加する。
2. `CreateGuiOptionsJson()` の `vars` に同じ文字列を追加する。
3. `ExecuteGuiSetOption()` で値を検証・反映する。
4. `ExecuteGenMove()` などで新しい値の動作を実装する。

GUI側では、`KifuwarabeGo2026.Gui/Application/GtpEngineGuiOptions.cs` の `RandomMoveValues` に同じ文字列を追加します。エンジンの `vars` とGUIの候補配列で、文字列と大文字・小文字を一致させてください。

候補選択画面は1ページ4件で、候補数に応じてページ数を自動計算します。候補が20件なら5ページ表示になります。

## 新しいオプションを追加する

現在のGUIは、エンジンから受け取った任意の定義を完全に自動描画する方式ではありません。`RandomMove` のような既知オプションをGUI側にも登録して表示します。

新しいオプションを追加するときは、エンジン側とGUI側の両方を更新してください。

### エンジン側

1. オプション値を保持するフィールドを追加する。
2. `CreateGuiOptionsJson()` の `options` に定義を追加する。
3. `ExecuteGuiGetOption()` でIDを処理する。
4. `ExecuteGuiSetOption()` でIDと値を検証して反映する。
5. 思考処理などから設定値を参照する。

### GUI側

主に次のファイルが関係します。

| ファイル | 役割 |
|---|---|
| `KifuwarabeGo2026.Gui/Application/GtpEngineGuiOptions.cs` | 既知オプションのIDと候補値 |
| `KifuwarabeGo2026.Gui/Application/GoAppSession.cs` | 編集中の値、選択、ページ状態 |
| `KifuwarabeGo2026.Gui/Presentation/Local/Resting/EngineSelect/GoScreenRenderer.GtpEngine.cs` | オプション画面とcombo選択画面 |
| `KifuwarabeGo2026.Gui/Game1.cs` | マウス入力処理 |
| `KifuwarabeGo2026.Gui/Gtp/GtpEngineClient.cs` | 起動時の定義取得、検証、値送信 |

親のエンジンオプション画面とcombo選択画面にはページャーがあり、どちらも項目数からページ数を計算する構造です。

## 実装時の注意

- `id`、`default`、`value`、`vars`、`gui_getoption`、`gui_setoption` の表記を一致させてください。
- 現在のコマンド解析は空白区切りです。IDや値には空白を含めないでください。
- オプション値はエンジンのプロセス内に保持されます。永続化はGUIのエンジンプロファイルが担当します。
- `gui_setoption` は不正なIDや値を黙って受理せず、GTPエラーを返してください。
- `gui_options` のJSONはGTP成功応答の本文として返してください。
- 標準入力と標準出力はGTP通信だけに使用してください。
- 標準エラー出力にも診断情報やデバッグ情報を書き込まないでください。囲碁GUIによっては、出力があるだけでエンジンエラーと判断されます。
- エラーログやデバッグログは、標準入出力および標準エラー出力を使わず、専用のログファイルへ書き出してください。

## 関連ソース

- [Program.cs](./Program.cs)：GTPコマンドとオプションの実装
- [StarRegionRandomMoveSelector.cs](./StarRegionRandomMoveSelector.cs)：星周辺を考慮する着手選択
- [KifuwarabeGo2026.Engine.csproj](./KifuwarabeGo2026.Engine.csproj)：プロジェクト設定
- [LICENSE.txt](../LICENSE.txt)：MIT License
