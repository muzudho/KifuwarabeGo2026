# Kifuwarabe Go 2026 v3.3.0

キーボード操作とCGOS観戦表示を強化し、Go App対応エンジンのPlayer／Providerロールを明確にしたリリースです。

## Tabキーによる画面操作

- 大会ルール、GTPエンジン、CGOS接続先、CGOS認証情報、人間プレイヤー名の入力欄を`Tab`／`Shift + Tab`で循環できます
- プロバイダー選択画面では有効なボタンだけを移動し、`Enter`／`Space`で実行できます
- 現在欄の前後へ`TAB`／`SHIFT + TAB`キーキャップを表示します
- 入力欄、選択ボタン、実行ボタンを見分けやすいフラットスタイルへ整理しました
- タイトル画面から開いたGTPエンジン編集にもキーボード入力が届くよう修正しました

## CGOS観戦表示

- サーバーから受信した確定消費時間を`USED`、受信後のGUI側経過を加えた現在値を`NOW`として表示します
- アゲハマを、茶色い皿、取った相手色の石、個数で視覚表示します
- すでに同じ対局を観戦中の場合、延期された［対局を観る］通知を再表示しません

## Go AppのPlayer／Providerロール

- `kfw-list-apps player`と`kfw-list-apps provider`で、ロール別の対応アプリIDを取得できます
- 引数なしの`kfw-list-apps`は後方互換として全ロールの和集合を返します
- 同梱エンジンはPlayerとして`play`と`ponnuki`、Providerとして`ponnuki`を公開します
- GUIはロール指定を優先し、v3.2.0形式のエンジンだけ引数なし照会へフォールバックします
- PublicDocsをロール別の実装・確認手順へ更新しました

## テスト状況

- ソリューション全体のReleaseビルド
- 移植性スモークとWindowsスモーク
- GUI版とEngine版のWindows x64 publish
- GUI、Core、Shared、Match、GtpExtensions、CGOS、Engineのファイルバージョン確認
- 同梱CGOS通信コンポーネントとEngine GTP応答のスモークテスト

## 配布物

- GUI版: `KifuwarabeGo2026.GameOasis.Gui-v3.3.0-win-x64.zip`
- Engine版: `KifuwarabeGo2026.Engine-v3.3.0-win-x64.zip`

GUIとEngineの両方を使う場合は、2つともダウンロードしてください。

## SHA-256

- GUI版: `5E525CB4CFBDA858EA9C2E97E1D4DAAEB2E58AA04F6019A297A49B4440ADF8D1`
- Engine版: `368C67939458F7E56DB29BEF3934C4D8D49388BC17722A0C21CE617DA71B48E1`

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime
- フレームワーク依存配置
