# Kifuwarabe Go 2026 v2.7.0

大会ルール、GTPエンジン、CGOS接続先を、使いたい順番へ簡単に整理できるようにしたリリースです。

## 主な変更

- 大会ルール設定、GTPエンジン設定、CGOS接続先へ共通の `ORDER` ボタンを追加
- 隣接する2ページを見渡せる2列カードボードで順序を編集
- マウスのドラッグ＆ドロップによる直感的な並べ替え
- `TO TOP`、`PAGE UP`、`UP`、`DOWN`、`PAGE DOWN` による確実なボタン操作
- `CANCEL` では元の順序を維持し、`SAVE` で設定ファイルへ確定
- 並べ替え後も、選択中の大会ルール、黒白・CGOS黒白のエンジン、CGOS接続先を維持
- リリース初期値を `default-settings.json` へ集約
- 大会ルール、GTPエンジン、CGOS接続先の初期値をJSONから調整可能
- `default-settings.json.README.md` にスキーマVersion 1の仕様と記入例を追加
- 大会ルールの時間を `TimeControl.Main` の `時:分:秒` 形式へ整理
- 旧時間形式と旧ルール名表記を互換読込

## テスト状況

- 7プロジェクト全体のReleaseビルドを実施
- 移植性スモークとWindowsスモークを実施
- 順序移動、ドラッグ、確定、キャンセル、選択対象の維持を自動回帰検査

## 配布物

- GUI版: `KifuwarabeGo2026.GameOasis.Gui-v2.7.0-win-x64.zip`
- Engine版: `KifuwarabeGo2026.Engine-v2.7.0-win-x64.zip`

GUIとEngineの両方を使う場合は、2つともダウンロードしてください。

## SHA-256

- GUI版: `2981DB53C2E70F2AA16B283BC388B2A76F8F10195845404894119AA51854EDB4`
- Engine版: `BDAB11032E958E2FF461AFDBF638B9197DCA1AB2435D071C2A5AF384032C2DC3`

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime
- フレームワーク依存配置
