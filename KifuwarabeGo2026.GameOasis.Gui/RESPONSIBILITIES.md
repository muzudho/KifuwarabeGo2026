# Game Oasis GUI 内の責務配置

このプロジェクトは、既存製品と発行経路の互換性を保つため、ロビーとプレイルームを同じアセンブリに収める移行用シェルです。名前空間と物理フォルダーで、次の論理境界を表します。

| 物理配置 | 名前空間 | 所有する責務 |
|---|---|---|
| `LobbyGui/Application` | `KifuwarabeGo2026.LobbyGui.Application` | 開始前カタログの表示用投影と、ロビーGUIからロビーエンジンへ渡すコマンド |
| `PlayRoom/Launching` | `KifuwarabeGo2026.PlayRoom.Launching` | プレイルーム起動要求の生成と、起動先アダプター |
| `Application/Local/Playing`、盤面・棋譜・レビュー関連 | `KifuwarabeGo2026.GameOasis.Gui...` | 起動後のプレイルームGUIと移行中のゲーム固有状態 |
| `Presentation`、`Infrastructure`、`Game1` | `KifuwarabeGo2026.GameOasis.Gui...` | 共有MonoGameシェル、描画、OS境界、画面遷移の組み立て |

依存方向は次のとおりです。

```text
Game1 / ロビー画面
  -> LobbyGui.Application
      -> LobbyEngine.JsonLines（登録済み参加者の読取。ホスト同梱時）
          -> 標準入出力 JSON Lines -> LobbyEngine.JsonLinesHost
      -> LobbyEngine（通信障害時の復旧および変更操作）
          -> GameOasis.Application の保存境界
          -> GameOasis.Storage の同一プロセス実装

Game1 / ロビー画面
  -> PlayRoom.Launching
      -> GameOasis.Contracts.PlayRoom
      -> 同一プロセスのプレイルーム（移行用アダプター経由）
```

`GameOasis.Application` と `GameOasis.Storage` はカタログと永続化を共有し、`GameOasis.Concierge` と `GameOasis.Contracts` は Protocol G/S/P/M およびプレイルーム起動契約を共有します。このため、これらはロビー専用名へ変更しません。

`KifuwarabeGo2026.GameOasis.Gui.Windows.exe`、設定ファイルの場所、発行成果物の別名 `KifuwarabeGo2026.Gui.exe` は既存利用者とランチャーの互換経路です。別実行ファイル化を行う段階までは変更しません。
