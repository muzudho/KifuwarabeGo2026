# Game Oasis GUI 内の責務配置

このプロジェクトは、既存製品と発行経路の互換性を保つための移行用MonoGameシェルです。新しいLobby GUI／Engineと囲碁Play Room GUI／Engineの正本は専用プロジェクトへ分離済みで、このプロジェクトには旧画面を動かす構成、設定、外部サービス接続、未移行画面だけを残します。

| 物理配置 | 名前空間 | 所有する責務 |
|---|---|---|
| `Application/LobbyGuiComposition.cs` | `KifuwarabeGo2026.GameOasis.Gui.Application` | 専用Lobby GUI／Engineを互換実行ファイルへ組み込む構成点 |
| `PlayRoom/Launching` | `KifuwarabeGo2026.PlayRoom.Launching` | 旧`GoAppSession`を公開Play Room起動要求へ写す互換アダプター |
| `Application/Local/Playing`、盤面・棋譜・レビュー関連 | `KifuwarabeGo2026.GameOasis.Gui...` | 専用Hostへ未移行のBoard Editor、Review、CGOSおよび同一プロセス互換経路 |
| `Presentation`、`Infrastructure`、`Game1` | `KifuwarabeGo2026.GameOasis.Gui...` | 公開実行名を維持するMonoGameシェル、OS境界、画面遷移の組み立て |

依存方向は次のとおりです。

```text
Game1 / 互換ロビー画面
  -> KifuwarabeGo2026.LobbyGui
      -> LobbyEngine.JsonLines（登録済み参加者の読取。ホスト同梱時）
          -> 標準入出力 JSON Lines -> LobbyEngine.JsonLinesHost
      -> LobbyEngine（通信障害時の復旧および変更操作）
          -> GameOasis.Application の保存境界
          -> GameOasis.Storage の同一プロセス実装

Game1 / 互換画面
  -> PlayRoom.Launching（旧セッションから公開要求への変換だけ）
      -> GameOasis.Contracts.PlayRoom
      -> Reference.PlayRoomGui.Go.Windows（Local Match）
      -> 同一プロセス互換Host（Board Editor、Review、Ponnuki）
```

`GameOasis.Application` と `GameOasis.Storage` はカタログと永続化を共有し、`GameOasis.Concierge` と `GameOasis.Contracts` は Protocol G/S/P/M およびプレイルーム起動契約を共有します。このため、これらはロビー専用名へ変更しません。

`KifuwarabeGo2026.GameOasis.Gui.Windows.exe`、設定ファイルの場所、発行成果物の別名 `KifuwarabeGo2026.Gui.exe` は既存利用者とランチャーの互換経路です。別実行ファイル化を行う段階までは変更しません。
