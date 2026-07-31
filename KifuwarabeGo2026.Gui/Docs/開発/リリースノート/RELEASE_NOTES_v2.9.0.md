# Kifuwarabe Go 2026 v2.9.0

盤面編集で作った指定局面から、GTPエンジンとの対局を始められるようにしたリリースです。

## 主な変更

- 置き碁、自由置き碁、黒白混在の編集局面からコンピューター対局を開始
- 黒番と白番のエンジンを別々に調査する［指定局面コンシェルジュ］を追加
- 対応方式、成功、未確認、非対応、不一致、通信失敗を画面へ表示
- ［別の方法を試す］［このまま続ける］［GTP LOG］［CANCEL］を追加
- 成功した方式をエンジン名・version・Profileごとに自動保存
- Generic、Kifuwarabe、KataGo、Leela Zero、GNU Goの互換Profileを追加
- きふわらべEngineへ、黒白の石を原子的に配置する独自GTP拡張を追加
- ローカル対局の中核を、将来のLinux囲碁サーバーでも再利用できる `KifuwarabeGo2026.Match` へ分離

## きふわらべEngineの指定局面コマンド

- `begin_position`
- `add_black`
- `add_white`
- `set_to_play`
- `commit_position`
- `abort_position`

失敗時は準備中の局面を破棄し、実対局盤を変更しません。

## テスト状況

- 9プロジェクト全体のReleaseビルド
- 移植性スモークとWindowsスモーク
- 同梱Engine.exeを外部プロセスとして起動する指定局面統合検査
- 利用者による、きふわらべを使った指定局面戦の実画面確認

KataGo、Leela Zero、GNU Goはこの開発環境に実行ファイルがなかったため、未実機・未成功として扱っています。起動時の能力検査と［別の方法を試す］で互換方式を選択します。

## 配布物

- GUI版: `KifuwarabeGo2026.Gui-v2.9.0-win-x64.zip`
- Engine版: `KifuwarabeGo2026.Engine-v2.9.0-win-x64.zip`

GUIとEngineの両方を使う場合は、2つともダウンロードしてください。

## SHA-256

- GUI版: `78A326BCC9AE262B7CC1BDB669201CD5C11FDEB1636D1ADA8E9298BB79D05925`
- Engine版: `30A67AB5C39942DDA5AFFF661EC9978E3182148EF0EEED3FEF2CBAF851F365F7`

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime
- フレームワーク依存配置
