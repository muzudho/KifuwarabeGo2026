# Game Oasisの横断的な後続課題

2026-08-23の引き継ぎと、その後の分離記録を照合するための一覧です。各項目の現在の完了状況は着手前に確認します。

| 後続候補 | 確認する境界 |
|---|---|
| 通常囲碁の時計・結果確定・裁定の運営責務 | 現役の囲碁MatchモデルとConciergeの所有範囲 |
| 人間・自動Game MasterのGUI接続 | Protocol M、運営画面、参照実装 |
| 外部ゲームの設定UI・操作アダプター登録 | カタログ発見後の公開構成点。未知ゲームへ公式設定を推測して送らない |
| 統合ApplicationSettingsの整理 | 物理ファイル操作、GUI設定、カタログ保存、既存JSONの移行境界 |
| 旧対局経路と新経路の状態重複 | PlayingScene、GoAppSession、各セッションの正本 |

旧Shared・Matchプロジェクトの廃止、GTP/CGOS/SGF移行、JSON Lines SDKの導入は後日の実施記録があるため、当初の未着手一覧を再実行しません。

- [現在の構成](../CurrentState/Architecture.md)
- [SDK・外部実装の整備](SdkDevelopment.md)
- [Lobby描画境界](LobbyRenderer.md)
- [当初の未決事項と実施の経緯](../Archive/GameOasisV4Migration.md)
