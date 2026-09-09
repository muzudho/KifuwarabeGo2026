# 構成と責務の入口

整理日: 2026-09-09。分離計画の実施記録と各プロジェクトのREADMEを基にした案内です。過去の「3プロジェクト」「Go Core / Go Play / Go Apps」は現在の物理プロジェクト一覧として使いません。

| 所有者 | 主な責務 |
|---|---|
| `GameOasis.Contracts` | Protocol G/P/M/Sの公開意味契約 |
| `GameOasis.Concierge` | 公開契約を介したセッション・参加者の調停 |
| `LobbyGui` | 開始前のページ状態、表示モデル、意味入力 |
| `LobbyEngine` 系列 | カタログ、保存、構成、開始準備 |
| `Reference.PlayRoomGui.Go` | 囲碁表示状態、幾何モデル、Presenter、公開起動要求の解釈 |
| `Reference.PlayRoomEngine.Go` / `.Ponnuki` | 各ゲームの状態、行動、終局。Goの `Match` は互換GUIが利用する現役モデル |
| `Reference.PlayDomain.Go` | 囲碁の共有ドメイン |
| `FormalAdapter.Gtp` / `.Cgos` / `.Sgf` | 外部形式の解釈・意味変換 |
| `FormalAdapter.Gtp.PlayerEngine` | 外部GTPエンジンをProtocol Pへ適合 |
| `Reference.PlayerEngine.Go.Gtp` / `.Host` | 参照GTPサーバーと配布互換名 `KifuwarabeGo2026.Engine` の起動口 |
| `InstallerGui` / `InstallerEngine` | 導入・更新・起動の画面とエンジン |

名前は共通接頭辞 `KifuwarabeGo2026.` を省略しています。`GameOasis.Gui` は互換MonoGameシェルと構成・Adapterを含み、すべての画面が専用Hostへ移行済みという意味ではありません。

## 詳細の正本

- [Lobby GUI](../../../KifuwarabeGo2026.LobbyGui/README.md)
- [囲碁Play Room GUI](../../../KifuwarabeGo2026.Reference.PlayRoomGui.Go/README.md)
- [囲碁Play Room Engine](../../../KifuwarabeGo2026.Reference.PlayRoomEngine.Go/README.md)
- [用語と公開境界](GameOasisTerminology.md)
- [分離の到達点](LobbyPlayRoomSeparation.md)
- [FormalAdapterの到達点](FormalAdapterMigration.md)
- [開発環境・ビルド](../../../README.developer.md)

## CGOSの読み方

GUIが独立CGOS Hostを起動し、HostがTCP接続とGTP子プロセスを組み立てます。CGOSのプロトコル解析・接続・状態機械・GUI通知の変換は `FormalAdapter.Cgos` 側へ移行しています。GUIは構造化通知を通常経路とし、旧Hostの人間向けログは専用の互換アダプターを介します。

2026-07-18の[ソース概要・CGOS接続フロー](../Archive/SourceAndCgosFlow.md)は移行前の調査記録です。固定認証、単一ファイル内の状態機械、ログ文字列の直接解析を現行仕様として読みません。
