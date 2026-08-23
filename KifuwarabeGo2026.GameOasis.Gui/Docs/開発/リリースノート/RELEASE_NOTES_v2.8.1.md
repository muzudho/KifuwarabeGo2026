# Kifuwarabe Go 2026 v2.8.1

v2.8.0のテキスト編集を安定化し、操作性と版数表示を改善した修正版です。

## 主な変更

- テキストボックスでDeleteキーを長押ししたとき、選択範囲が文字列末尾を越えて強制終了する不具合を修正
- 選択範囲を常に現在の文字列内へ正規化し、連続削除を安全化
- `Ctrl+Z`／`Ctrl+Y` の長押しによるUndo／Redoのキーリピートに対応
- タイトル画面の製品名横へ、実行ファイルから取得したバージョン番号を表示
- v2.8.0のリリースページへ既知の不具合とv2.8.1への更新案内を追加

## テスト状況

- 7プロジェクト全体のReleaseビルドを実施
- 移植性スモークとWindowsスモークを実施
- Deleteキーリピート時の境界処理と、Undo／Redoキーリピートを自動回帰検査

## 配布物

- GUI版: `KifuwarabeGo2026.GameOasis.Gui-v2.8.1-win-x64.zip`
- Engine版: `KifuwarabeGo2026.Engine-v2.8.1-win-x64.zip`

GUIとEngineの両方を使う場合は、2つともダウンロードしてください。

## SHA-256

- GUI版: `4150897114D2A3A87A9F85C8C6B79CA70CBAF2A6CDBA917618A4217200715FB8`
- Engine版: `2DBD9DB4FF19107FA0279E0BF447C451BDBE46BCCB2C7856ACF6BCBE9C2BAB04`

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime
- フレームワーク依存配置
