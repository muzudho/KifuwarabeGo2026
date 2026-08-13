# 文房具UIと GoScreenRenderer 整理の完了項目

整理状態: 完了（2026-08-14）

## 文房具UI

- `LinkUnderline`、`SinglelineTextUnderline`、`MultilineTextUnderline` は、Bounds、ホバー、選択・編集中状態を各インスタンスで所有する。
- 各下線コントロールは `ActionBadge` を所有し、ホバー時だけ `EDIT`、`OPEN`、`BROWSE` などのバッジを表示する。
- `ActionBadge` はラベル、アンカー、文字スケールを生成時に受け取り、表示位置を外部から再計算しない。
- 数値入力は `PopupNumberUnderline` と `SpinButton` へ集約した。`SpinButton` は上下のワイヤーフレーム三角ボタンと中央のステップ値からなる。
- 時刻入力は `PopupTimeUnderline` へ集約した。時・分・秒をそれぞれ直接編集できる。

## 大会ルール設定

- KOMI、MOVES、TIME を文房具UIの下線入力へ移行した。
- KOMI は 0.0 から 99.5、0.5刻みで直接入力とスピン操作を行える。
- MOVES は 100、10、1 のスピン操作を行える。
- SETTINGS はファイルの場所を開く `OPEN` 操作とした。

## アプリケーション設定

- LOG を ROOT FOLDER と RECENT GUI LOGS の縦区画ラベルへ整理した。
- SGF、SCREENSHOT、APPLICATION、ENGINE を縦区画ラベルへ統一した。
- 変更可能な保存先フォルダーは `BROWSE`、アプリケーション自身が管理する設定ファイルは場所を開く `OPEN` とした。

## GoScreenRenderer から切り出した部品

- 大会ルール入力の配置・ヒットテストは `Pages/EditTournamentRule/TournamentRuleEditorLayout.cs` が所有する。
- 下線、入力ポップアップ、アクションバッジ、スピンボタンは `Presentation/StationeryUI/` の独立コンポーネントである。
- 画面遷移、一時表示、確認ダイアログは `Pages/` 配下の専用コンポーネントへ配置済みである。

## 確認

- `dotnet build KifuwarabeGo2026.Gui/KifuwarabeGo2026.Gui.Core.csproj --no-restore` が成功することを確認した。
