# Kifuwarabe Go 2026 v3.10.0

Client Identity をお気に入りとして扱いやすくし、Entry Profile の認証情報を直接確認・編集しやすくしたリリースです。

## Favorite Client Identities

- Client Identity の一覧を `FAVORITE CLIENT IDENTITIES` として整理しました。最大5件まで登録できます。
- 一覧は `HANDLE`、`PASSWORD`、`IN DEFAULT` の3列です。既定の入力値は緑背景と `IN DEFAULT` で分かります。
- `INPUT` は選んだ HANDLE / PASSWORD を編集欄へコピーします。`SET AS DEFAULT` は次回の編集開始時に自動入力する項目を設定します。
- 操作は `ADD`、`DUPLICATE`、`EDIT`、`SET AS DEFAULT`、`DELETE` の順です。`ADD` は空の認証情報を作り、`DUPLICATE` は選択項目を複製します。

## Entry Profile の認証情報

- Entry Profile で HANDLE と PASSWORD を直接入力できます。
- Local Match で空の PASSWORD は未使用欄として表示します。お気に入りから入力した PASSWORD は、そのまま確認・編集できます。
- PASSWORD 欄へ目アイコンを追加しました。閉じた目では伏せ字、開いた目では平文を表示します。どちらの状態でも編集できます。
- 編集内容が変わっていないときは `CLOSE` と Disabled の `DISCARD`、変更時は `SAVE & CLOSE` と有効な `DISCARD` を表示します。

## 配布物

- GUI版: `KifuwarabeGo2026.GameOasis.Gui-v3.10.0-win-x64.zip`
- Engine版: `KifuwarabeGo2026.Engine-v3.10.0-win-x64.zip`

GUIとEngineの両方をダウンロードしてください。

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime
- フレームワーク依存配布
