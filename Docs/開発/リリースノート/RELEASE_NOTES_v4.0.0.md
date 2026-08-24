# Kifuwarabe Go 2026 v4.0.0

Kifuwarabe Go 2026を、交換可能なプレイヤー、ゲームマスター、GUI、ゲームスペースから構成されるゲームプラットフォーム「Kifuwarabe Game Oasis」へ再構成する最初のメジャーリリースです。

> [!IMPORTANT]
> 通常利用者は `KifuwarabeGo2026.Launcher-v4.0.0-win-x64.zip` をダウンロードしてください。v4.x.xはv3.x.xからの移行期間です。GUI版・Engine版の公開資産名と既存設定の互換経路を維持します。

## Game Oasis基盤

- Game Oasisの共通契約、コンシェルジュ、Application、Storageを責務別プロジェクトとして追加しました。
- GUI、プレイヤー、ゲームマスター、ゲームスペースの境界としてProtocol G、P、M、Sを追加しました。
- 通常囲碁とポン抜きの参照ゲームスペースを追加し、同じ公開契約から利用できるようにしました。
- ゲーム状態、着手履歴、時計、停止・再開、裁定、参加終了をプロトコル境界から扱えるようにしました。
- 公式識別子を`io.github.muzudho.kifuwarabego2026`配下へ整理しました。

## ローカル対局とエンジン

- 人間対人間、人間対コンピューター、コンピューター対コンピューターの通常囲碁ローカル対局をGame Oasis経路へ移行しました。
- 参照プレイヤーとGTP通信を分離し、標準入出力プロセスをProtocol Pへ接続しました。
- 既存GTP利用者との互換性のため、公式エンジンの出力名`KifuwarabeGo2026.Engine.exe`を維持します。
- 初期配置、パス、投了、棋譜履歴、SGF同期、エンジンオプションの既存経路を段階移行しました。

## GUIとデータ管理

- GUI実装を`KifuwarabeGo2026.GameOasis.Gui`とWindows実行ホストへ再構成しました。
- GTPエンジン、エントリー、Client Identity、CGOS接続先のカタログ処理をApplication／Storage境界へ移しました。
- ゲーム非依存のゲームスペース設定プロフィールとカタログ境界を追加しました。
- GUI固有の描画部品を`StationeryUI`としてゲーム規則から分離しました。

## v3互換移行

- v4.x.x全体をv3.x.xからの互換移行期間とします。
- ランチャーのGUI更新、Engine更新、既存のRelease資産名、設定ファイル、エンジン登録、SGF、GTP接続の互換経路を維持します。
- v3互換経路はv4.x.xでは廃止しません。廃止を検討できる最も早い製品世代はv5.0.0です。

## 対応環境と配布物

- 正式配布: Windows x64
- 必要環境: .NET 8 Desktop Runtime
- `KifuwarabeGo2026.Launcher-v4.0.0-win-x64.zip`
- `KifuwarabeGo2026.Gui-v4.0.0-win-x64.zip`
- `KifuwarabeGo2026.GameOasis.Gui-v4.0.0-win-x64.zip`（旧公開名互換）
- `KifuwarabeGo2026.Engine-v4.0.0-win-x64.zip`

通常利用者にはLauncher版を入口として推奨します。GUI版・Engine版の単独ZIPもv3互換用に配布します。

## 検証項目

- Releaseビルド
- Contracts、Concierge、Protocol G／P／Mの契約・結合試験
- 通常囲碁、ポン抜き、参照プレイヤー、GTP通信の試験
- GUI移植性、Windows実プロセス、Launcherのスモーク試験
- Windows x64向けLauncher版・GUI版・Engine版のpublishと配布物検査

## SHA-256

- Launcher版: `04333C64DE6D082B32CD2B645F1FFC8EC88F7DE38F10E8F3FC308D5B666847E3`
- GUI版: `C745A6EAFBAB270140D31F2593C35ECF5180B7B9EA505C38C23726C370D9470E`
- 旧公開名互換GUI版: `C745A6EAFBAB270140D31F2593C35ECF5180B7B9EA505C38C23726C370D9470E`
- Engine版: `C246D26B8C7FEDC8D26F2E58CA2134C3E9C8C837D536AA91EB5D147068BDFEAA`
