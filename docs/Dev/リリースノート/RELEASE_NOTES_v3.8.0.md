# Kifuwarabe Go 2026 v3.8.0

新UIを主要な設定・選択画面へ導入し、プレイヤー／エンジン／CGOS設定を一貫した操作と表示へ整理したリリースです。

## 新UI

- 値にマウスを重ねると `EDIT` または `CHANGE` バッジを表示し、クリックで編集・選択するUIへ統一しました。
- ENGINE OPTIONS、EDIT ENGINE (GTP)、EDIT SERVICE PROFILE、大会ルール編集、CGOS接続の各画面を新UIへ移行しました。
- SELECT ENGINE (GTP) ではエンジンをロボット顔アイコンで識別できます。
- PLAYER SELECT と ORDER では、プレイヤー名・人間／コンピューターの顔・エンジン名を列として見やすく表示します。同名の場合のエンジン名は `<SAME PLAYER NAME>` と表示します。

## CGOSと入力操作

- CGOS接続画面で、PLAYER、HANDLE、PASSWORD、管理者用BLACK／WHITEの操作を新UIへ移行しました。
- オンライン対局のプレイヤー選択タイトルを、色ではなく `PLAYER 1` / `PLAYER 2` で表すようにしました。
- StickyNoteは操作対象を覆わない画面端へ表示し、対象項目との接続線を描くようにしました。
- 前面のSELECT ENGINE (GTP)が背後のPLAYER SELECTより先にクリックを受け取るように修正しました。

## 引き継ぎ計画

- ダイアログが重なった場合の入力優先順位を根本的に整理するため、アクティブウィンドウ入力ストラテジー導入計画を追加しました。

## 配布物

- GUI版: `KifuwarabeGo2026.GameOasis.Gui-v3.8.0-win-x64.zip`
- Engine版: `KifuwarabeGo2026.Engine-v3.8.0-win-x64.zip`

GUIとEngineの両方をダウンロードしてください。

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime
- フレームワーク依存配布
