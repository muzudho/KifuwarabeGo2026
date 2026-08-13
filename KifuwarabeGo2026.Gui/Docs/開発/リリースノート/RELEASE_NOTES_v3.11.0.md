# Kifuwarabe Go 2026 v3.11.0

文房具UIを大会ルール設定とアプリケーション設定へ広げ、編集・閲覧・保存先選択の意味を統一したリリースです。

## 文房具UIと大会ルール設定

- KOMI、MOVES、TIME をクリックして直接編集できる下線入力に統一しました。
- KOMI は 0.5 刻み、MOVES は 100／10／1 刻みのスピン操作に対応しました。
- 数値・時刻入力ポップアップの操作ボタンを右上へ整理しました。
- 大会ルール設定ファイルは `OPEN` から場所を開けます。

## アプリケーション設定

- LOG、SGF、SCREENSHOT、APPLICATION、ENGINE を縦区画ラベルで整理しました。
- 保存先を変更できる項目は `BROWSE`、設定ファイルなど参照用の項目は `OPEN` として意味を統一しました。
- 最近のGUIログを `OPEN` で開けます。

## 編集画面の操作

- 内容が未変更のときは `DISCARD` を無効にし、`SAVE & CLOSE` を `CLOSE` と表示します。
- `CLOSE` は保存せず前の画面へ戻ります。

## 配布物

- GUI版: `KifuwarabeGo2026.Gui-v3.11.0-win-x64.zip`
- Engine版: `KifuwarabeGo2026.Engine-v3.11.0-win-x64.zip`

## テスト状況

- `dotnet build KifuwarabeGo2026.slnx --no-restore` 成功（警告 0、エラー 0）
- Windows x64 でGUI版・Engine版をpublish
- 同梱CGOS通信コンポーネントの `--help` 成功
- Engine の GTP `protocol_version`、`name`、`version`、`boardsize 9`、`clear_board`、`quit` 成功
- PortabilitySmoke と WindowsSmoke 成功

## SHA-256

- GUI版: `BAD4BA606BAE6D13B268DE874767D56C17C63727FB24FFCCE2FF8A61E2881AD2`
- Engine版: `E229A669F2E41966D66FA5840BE90322331C05B6B775A8E5678405FAA0D9B1CD`

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime
- フレームワーク依存配布
