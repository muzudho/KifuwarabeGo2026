# Kifuwarabe Go 2026 v3.5.0

盤面の静的な構造を切り替えて観察できるBOARD LENSと、エンジン選択・チャート操作の使いやすさを改善したリリースです。

## BOARD LENS

- 従来の「連解析」を`BOARD LENS`へ改名しました
- 切替キーを`R`から`L`へ変更しました
- 切替時に、現在のレンズ名を画面上のバナーへ表示します
- `REN INDEX LENS`、`REN RECTANGLE LENS`、`REN GRAPH LENS - BASIC`、`REN GRAPH LENS - EYE MODE`を利用できます
- `REN RECTANGLE LENS`のバナーへ`[L] Graph  [2] Liberty`のガイドを表示します

## Liberty Number Lens

- `REN RECTANGLE LENS`で`2`を押すと`LIBERTY NUMBER LENS`へ切り替わります
- 黒白それぞれの連について、直接隣接する空点を重複なしで数えた呼吸点数を表示します
- 19路盤でも読みやすいよう、連インデックスを小さく、呼吸点数を大きな数字で表示します
- レンズを表示したまま棋譜やチャートをシークし、局面ごとの呼吸点変化を観察できます

## チャートと棋譜レビュー

- チャートポップアップで`Enter`を押すと、選択した局面へ移動した状態で閉じます
- ［CLOSE］ボタン、`Enter`、`Escape`で同じようにポップアップを閉じられます
- 棋譜レビュー欄へ`[L] BOARD LENS`のキーガイドを追加しました

## エンジン選択画面

- アプリ提供エンジン選択のボタンを［CLOSE］と［USE］へ変更しました
- 現在使用中のエンジンを緑色の`IN USE`で表示します
- プロパティ表示の操作対象を、水色の枠と指マークで示します
- 非対応エンジンも操作対象にして詳細を確認でき、確定操作だけを無効にします

## テスト状況

- ソリューション全体のReleaseビルド
- 移植性スモークとWindowsスモーク
- GUI版とEngine版のWindows x64 publish
- 同梱CGOS通信コンポーネントとEngine GTP応答のスモークテスト

## 配布物

- GUI版: `KifuwarabeGo2026.Gui-v3.5.0-win-x64.zip`
- Engine版: `KifuwarabeGo2026.Engine-v3.5.0-win-x64.zip`

GUIとEngineの両方を使う場合は、2つともダウンロードしてください。

## SHA-256

- GUI版: `D4A5BAD92B8A974777440B6AAD0C59334479D67E7EE7578A0FDE7C4FBE195932`
- Engine版: `4FB7B790A135EBFAAEE7B7EF5BC614B1A8335C1EDA5CF6B4624AC6F19BA96825`

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime
- フレームワーク依存配置