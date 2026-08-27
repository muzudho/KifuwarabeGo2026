# Kifuwarabe Go 2026 v4.0.6

ランチャーのＧＵＩとエンジンを役割別に分割し、将来ほかの言語でも接続できる標準入出力通信の基礎を追加したリリースです。

> [!IMPORTANT]
> 通常利用者は `KifuwarabeGo2026.Launcher-v4.0.6-win-x64.zip` をダウンロードしてください。

## Launcher

- ランチャーを `LauncherGui` と `LauncherEngine` の役割別プロジェクトへ分割しました。
- 描画・入力、ＧＵＩ用OS処理、更新・インストール・起動、エンジン用OS処理の境界を整理しました。
- 利用者向け実行ファイル名 `KifuwarabeGo2026.Launcher.exe`、既存設定、インストール済み製品、更新経路の互換性を維持します。
- 通常起動では、従来どおり安定した同一プロセスエンジンを使用します。

## 標準入出力通信

- UTF-8の標準入出力とJSON Linesを使うランチャーエンジンホストを試験導入しました。
- 状態、インストール済み一覧、現在版フォルダー、設定変更、検証付きアンインストールを言語非依存のメッセージで扱えます。
- 通信障害時はランチャーを終了させず、同一プロセスエンジンへ自動的に復旧します。
- 補助EXEがアプリケーション制御ポリシーで拒否される環境を考慮し、ホストDLLを `dotnet` 経由で起動します。
- 試験機能はランチャーへ `--engine-stdio` を付けた場合に使用します。

## 次の開発計画

- ランチャー分割を完了し、ロビーＧＵＩとロビーエンジンの分割計画を追加しました。
- 今後は現行Game Oasis GUIに同居するロビーとプレイルームを段階的に分けます。

## 互換性と配布物

- v4.x.x移行期間として、v3.x.xランチャー向けの資産名と旧GUI公開名を維持します。
- `KifuwarabeGo2026.Launcher-v4.0.6-win-x64.zip`
- `KifuwarabeGo2026.Gui-v4.0.6-win-x64.zip`
- `KifuwarabeGo2026.GameOasis.Gui-v4.0.6-win-x64.zip`（旧公開名互換）
- `KifuwarabeGo2026.Engine-v4.0.6-win-x64.zip`

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime（配布物はframework-dependentです）

## SHA-256

- Launcher版: `B203082694283CB9B8F2E4E528E3391588F3F3DA59EE83CF9105E651833BB32D`
- GUI版: `2EA6BA4F9BC5A7F04098B4C8B79404764A77015684BDA660374E44659D4CC4C4`
- 旧公開名互換GUI版: `2EA6BA4F9BC5A7F04098B4C8B79404764A77015684BDA660374E44659D4CC4C4`
- Engine版: `E34DBBD08DB472F4A8CF8550F2306231F08A7F4DAC9278E3BA2292C6015FF197`
