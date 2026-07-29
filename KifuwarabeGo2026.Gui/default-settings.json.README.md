# `default-settings.json` 仕様

`default-settings.json` は、GUI版のリリースへ同梱する初期設定です。

開発者がリリース時の大会ルール、GTPエンジン、CGOS接続先を調整するときに編集します。利用者がGUIから変更した設定は別のファイルへ保存されるため、このファイルへは書き戻されません。

## 適用されるタイミング

- 新規環境で利用者設定がまだ存在しないときだけ、初期値として読み込みます。
- 既存の利用者設定は上書きしません。
- GUI版のビルド出力とpublish出力では、実行ファイルと同じディレクトリーへコピーされます。
- 開発中は、リポジトリ直下の `KifuwarabeGo2026.Gui/default-settings.json` を読み込みます。

利用者設定の保存先は次のとおりです。

```text
%LOCALAPPDATA%\KifuwarabeGo2026\application-settings.json
%LOCALAPPDATA%\KifuwarabeGo2026\GtpEngines\gtp-engine-list.json
```

## 仕様バージョン

ルートの `SchemaVersion` が、このJSON自体の仕様バージョンです。

```json
{
  "SchemaVersion": 1
}
```

- 現在の仕様番号は `1` です。
- 公開後に互換性のない構造変更を行う場合は、番号を増やします。
- 項目の追加でも、旧プログラムが安全に無視できない変更なら番号を増やします。
- 対応している番号より大きなJSONを、旧プログラムが誤って読み込まないために使用します。

## 全体構造

```json
{
  "SchemaVersion": 1,
  "TournamentRuleSettings": {
    "TournamentRules": []
  },
  "EngineSettings": {
    "GtpEngines": []
  },
  "CgosConnectionSettings": {
    "CgosConnections": []
  }
}
```

配列の並び順が、GUIの一覧に表示される初期順序です。

## 大会ルール

```json
{
  "DisplayName": "Yamashita CGOS Server 9-ro",
  "Rule": "Chinese",
  "BoardSize": 9,
  "Komi": 7.0,
  "TimeControl": {
    "Main": "00:15:00"
  },
  "MoveLimit": 9999
}
```

| 項目 | 必須 | 説明 |
| --- | --- | --- |
| `DisplayName` | はい | GUIに表示する大会ルール名 |
| `Rule` | はい | `Chinese`、`Japanese`、`PureGo` |
| `BoardSize` | はい | `9`、`13`、`19` |
| `Komi` | はい | コミ。例: `7.0`、`6.5` |
| `TimeControl.Main` | はい | 秒読み前の持ち時間。`時:分:秒` |
| `MoveLimit` | はい | 最大手数。`0` は無制限、最大 `9999` |

`TimeControl.Main` の範囲は次のとおりです。

- 時: `0`～`999`
- 分: `0`～`59`
- 秒: `0`～`59`
- 推奨表記: `"00:15:00"`
- 最大値: `"999:59:59"`
- `"00:00:00"` は持ち時間なし

大会ルールの内部IDは自動生成するため、`Id` を記述する必要はありません。

現在のGUIが使用する時間設定は `Main` だけです。将来、秒読みやフィッシャー方式へ対応するときは、例えば `ByoYomi` や `Increment` を `TimeControl` の中へ追加します。その時点で、必要に応じて `SchemaVersion` を更新します。

### 旧形式の互換読込

次の旧表記も読み込めます。

- `MainTime`
- `MainTimeMinutes` と `MainTimeSeconds`
- `chinese`、`japanese`、`pureGo` などの小文字始まりのルール名

利用者設定をGUIから保存し直すと、`TimeControl.Main` とPascalCaseのルール名で出力します。

## GTPエンジン

### 最小構成

```json
{
  "DisplayName": "Kifuwarabe",
  "ExecutablePath": "KifuwarabeGo2026.Engine.exe",
  "WorkingDirectory": ".",
  "Arguments": "",
  "EnableGtpLog": true
}
```

| 項目 | 必須 | 説明 |
| --- | --- | --- |
| `DisplayName` | はい | GUIに表示するエンジン名 |
| `ExecutablePath` | はい | GTPエンジンの実行ファイル |
| `WorkingDirectory` | 推奨 | エンジンを起動する作業ディレクトリー |
| `Arguments` | いいえ | 起動時のコマンドライン引数 |
| `EnableGtpLog` | いいえ | `true` ならGTP通信ログを保存 |

`ExecutablePath` と `WorkingDirectory` の相対パスは、`default-settings.json` のあるディレクトリーを基準にします。

`ExecutablePath` が空の項目は、有効なエンジンとしてGUIへ表示されません。

### CGOS用の初期資格情報

```json
{
  "DefaultCgosLoginName": "kifuwarabe",
  "DefaultCgosPlainTextPassword": ""
}
```

| 項目 | 説明 |
| --- | --- |
| `DefaultCgosLoginName` | CGOS接続画面へ初期表示するログイン名 |
| `DefaultCgosPlainTextPassword` | CGOS接続画面へ初期表示するパスワード |

パスワードは平文です。公開する `default-settings.json` には実パスワードを書かないでください。

### GUIオプション

```json
{
  "GuiOptions": {
    "RandomMove": "ChebyshevDistanceFromStar",
    "AvoidEyes": "true",
    "RandomSeed": "0",
    "EngineTag": "",
    "DebugLogFile": "",
    "ClearCache": "false"
  }
}
```

| 項目 | 値 |
| --- | --- |
| `RandomMove` | `Normal` または `ChebyshevDistanceFromStar` |
| `AvoidEyes` | `"true"` または `"false"` |
| `RandomSeed` | `0`以上の整数を文字列で指定。`"0"` はエンジン既定動作 |
| `EngineTag` | エンジンへ渡す任意文字列 |
| `DebugLogFile` | エンジン側のデバッグログファイル名 |
| `ClearCache` | `"true"` のとき、次回起動時にキャッシュ消去ボタン操作を送信 |

`GuiOptions` は標準GTPではありません。GUIは、エンジンが `gui_options` に対応していることを確認してから、対応する項目だけを送信します。一般的なGTPエンジンでは省略できます。

## CGOS接続先

```json
{
  "DisplayName": "Yamashita CGOS Server",
  "Host": "yss-aya.com",
  "Port": 6809,
  "Event": "",
  "Round": "-",
  "Note": "See also: yss-aya.com/cgos"
}
```

| 項目 | 必須 | 説明 |
| --- | --- | --- |
| `DisplayName` | はい | GUIに表示する接続先名 |
| `Host` | はい | CGOSサーバーのホスト名 |
| `Port` | はい | TCPポート。`1`～`65535` |
| `Event` | いいえ | 大会・イベント名 |
| `Round` | いいえ | ラウンド名や日程 |
| `Note` | いいえ | URLなどの補足 |

## 仕様変更履歴

### Version 1

- 初版。
- 大会ルール、GTPエンジン、CGOS接続先の初期値を一つのJSONへ集約。
- 大会ルールの時間を `TimeControl.Main` の `時:分:秒` 形式で定義。
- 大会ルールの内部IDはJSONへ記述せず、自動生成。
- ルール名はPascalCaseで記述。
- 旧 `MainTime`、`MainTimeMinutes`、`MainTimeSeconds` と小文字始まりのルール名を互換読込。

今後は `### Version 2`、`### Version 3` のように節を追加し、追加項目、削除項目、意味の変更、変換方法を記録します。
