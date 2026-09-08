# KifuwarabeGo2026.StationeryUI

2026-09-08: 共通コントロールとWindowsサービスを独立した [StationeryUI](https://github.com/muzudho/StationeryUI) へ移行しました。本プロジェクトは `StationeryUI.MonoGame` 0.1.0を参照し、きふわらべ固有の碁石・結果表示、背景、画面別の付箋配置、診断付きダイアログを接続するアダプターです。共通部品の実装は独立リポジトリーで変更してください。

初回パッケージはルートの `LocalPackages/StationeryUI` と `NuGet.Config` から復元します。以下は独立化前の役割の説明です。

［きふわらべの碁2026］と共通ランチャーから利用する、MonoGameベースの文房具UIライブラリーです。

このプロジェクトは、ボタン、見出し、付箋、下線入力、共通ダイアログ、Canvas、仮想画面、文字描画、およびUIが必要とするプラットフォーム抽象を所有します。
囲碁盤、対局、棋譜、CGOS、GUI更新などのアプリケーション固有機能は所有しません。

依存方向は利用側から本ライブラリーへの一方向です。

```text
KifuwarabeGo2026.GameOasis.Gui  --->  KifuwarabeGo2026.StationeryUI
KifuwarabeGo2026.Launcher  --->  KifuwarabeGo2026.StationeryUI（今後）
```
