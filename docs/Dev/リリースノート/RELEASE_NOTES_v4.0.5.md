# Kifuwarabe Go 2026 v4.0.5

ローカル対局を続けて開始するとGUIが強制終了する不具合を修正したパッチリリースです。

> [!IMPORTANT]
> 通常利用者は `KifuwarabeGo2026.Launcher-v4.0.5-win-x64.zip` をダウンロードしてください。

## GUI

- Local Matchの前局を終了してLobbyへ戻った直後に［START］を押しても、GUIが強制終了しないよう修正しました。
- 前局の終了処理中はLobbyに留まり、少し待ってから再度［START］を押すよう案内します。
- 終了処理が完了すると、アプリを再起動せず次局を開始できます。
- 開始処理が拒否された場合に、画面状態を不完全な`Playing`状態へ移さないようにしました。

## 互換性と配布物

- v4.x.x移行期間として、v3.x.xランチャー向けの資産名と旧GUI公開名を維持します。
- `KifuwarabeGo2026.Launcher-v4.0.5-win-x64.zip`
- `KifuwarabeGo2026.Gui-v4.0.5-win-x64.zip`
- `KifuwarabeGo2026.GameOasis.Gui-v4.0.5-win-x64.zip`（旧公開名互換）
- `KifuwarabeGo2026.Engine-v4.0.5-win-x64.zip`

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime（配布物はframework-dependentです）

## SHA-256

- Launcher版: `0D5B7C6D85B5BD17953AEBC310B698BFE8A59426433F8A312F35E235C9F49191`
- GUI版: `446FA00B8C498A82ED2C602333ED656B2ED879FBA47E3FDC2E35F033DD5B1573`
- 旧公開名互換GUI版: `446FA00B8C498A82ED2C602333ED656B2ED879FBA47E3FDC2E35F033DD5B1573`
- Engine版: `A3DA11A9149FCBEBEC8CB88057EB388589F1E43C80ADD3EF1542069A78B7C851`
