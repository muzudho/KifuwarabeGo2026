# 開発状況の入口

整理日: 2026-09-09。既存文書の最新の実施記録を整理した入口です。整理日をビルド・実機の確認日にはしません。

| 領域 | 記録されている到達点 | 後続 |
|---|---|---|
| StationeryUI（9/8） | 独立リポジトリー・v0.1.0公開、両アプリ共通パッケージ化 | 実IME・DPI確認、NuGet.org |
| Installer（9/7） | 改名・ロビー起動導線・互換配布のローカル検証済み | 公開版による旧クライアント移行確認 |
| Lobby / Play Room（8/30） | 4役の境界、専用囲碁Host、発行物・障害復帰。Lobby描画移行は継続 | 段階2の第12縦切り |
| FormalAdapter（8/30） | GTP・CGOS・SGFの移行済み | 段階7のWindows最終回帰判定 |
| SGF（8/29） | 文書モデル・Go変換・GUI読込保存接続済み | 変化図のGUI編集 |

- [StationeryUI](StationeryUi.md)
- [Installer](Installer.md)
- [Lobby・Play Room](LobbyPlayRoomSeparation.md)
- [FormalAdapter](FormalAdapterMigration.md)
- [SGF文書](SgfDocument.md)
- [構成と責務の入口](Architecture.md)
- [次の作業・構想](../Plans/README.md)

過去の「未着手」と後日の完了記録が矛盾するときは、後日の対象範囲を確認します。以前の未確認項目を確認済みへ変えず、異なる変更・ビルドでのPASSを同一の証拠とは扱いません。

[旧引き継ぎ全文と過去のロードマップ](../Archive/HandoffHistory.md) / [開発日誌](../Log/README.md)
