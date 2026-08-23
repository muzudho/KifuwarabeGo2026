# Kifuwarabe Go 2026 v3.4.0

ポン抜きGo AppとProvider Engineの連携を拡張し、スクリーンショット、結果表示、設定・選択画面の使いやすさを改善したリリースです。

## ポン抜きとProvider Engine

- Providerのオプションから［GAME SETTINGS］を動的に構成します
- `kfw-start-app`／`kfw-end-app`でProviderが初期局面と一局の状態を管理します
- 同梱Engineは9路、13路、19路に対応しました
- `InitialMoveCount`の最大値は9路20、13路42、19路90です
- 非破壊の`kfw-evaluate-options`で候補値を問い合わせ、返された全値・全スキーマを画面へ反映します
- 盤サイズは`binding: "gtp.boardsize"`で標準GTPへ結び付けます

## Provider選択と設定画面

- 互換性検査を非同期化し、待機中にローディングスピナーを表示します
- 最後に選択したProviderを保存し、次回起動時に復元・自動確認します
- Provider非対応の行もEDIT用に選択でき、確定する［SELECT］だけを無効にします
- GUIをEngineとして選んだ場合は起動前に判別し、明確なエラーを表示します
- 古いDebug出力には、利用可能な`win-x64`出力があれば候補パスを案内します
- 3番目の盤サイズ候補である19路をクリックできなかった入力判定を修正しました

## ポン抜きの画面とレビュー

- Provider確認画面は［NEXT］、実際に対局を始める画面は［START］としました
- 結果画面へ、皿・捕獲石・個数からなる共通アゲハマ表示を追加しました
- チャートポップアップで、クリック、ドラッグ、シークボタン、左右・Home・Endキー、長押し移動を利用できます
- 通常ローカル対局の内部名を`LocalGame`から`LocalPlay`へ統一しました

## スクリーンショット

- `Ctrl + P`でタイトルバーと外枠を含むゲームウィンドウ全体をPNG保存できます
- 保存先をタイトル画面の設定から変更できます
- DPIスケーリングを考慮したDWM物理座標を使い、正しい領域を撮影します
- 撮影時に視覚エフェクトと動的生成したシャッター音を再生します
- GUIログへウィンドウ座標、寸法、DPI、PNG寸法などを記録します

## テスト状況

- ソリューション全体のReleaseビルド
- 移植性スモークとWindowsスモーク
- GUI版とEngine版のWindows x64 publish
- 同梱CGOS通信コンポーネントとEngine GTP応答のスモークテスト

## 配布物

- GUI版: `KifuwarabeGo2026.GameOasis.Gui-v3.4.0-win-x64.zip`
- Engine版: `KifuwarabeGo2026.Engine-v3.4.0-win-x64.zip`

GUIとEngineの両方を使う場合は、2つともダウンロードしてください。

## SHA-256

- GUI版: `28B9A69E5078D7CEECDE9C36F0BECD748D76EBCA5D5791326389EA5B850A3450`
- Engine版: `82F5A48CC5FBA9B58E0572A53DAE74AA156E54A281047BCEBD99ED6982E88F3C`

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime
- フレームワーク依存配置
