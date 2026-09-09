# Installer改名・ロビー起動の実装状況

確認記録: 2026-09-07。実装・ローカル配布物・実画面の検証済み。GitHub公開版を使う移行確認とは区別します。[次回公開時の確認](../Plans/InstallerRelease.md)。

## 起動の設計

```text
デスクトップ［きふわらべの碁２０２６］
  → 固定配置の Installer.exe --launch-lobby
  → 現在使用する版のロビーGUI

ロビー［インストーラーを起動］
  → 固定配置の Installer.exe（起動専用引数なし）
  → インストーラー管理画面

インストーラー［ロビーを起動］
  → 現在使用する版のロビーGUI
```

上記 `Installer.exe` の正式名は `KifuwarabeGo2026.Installer.exe` とする。

### 起動専用モード

- `--launch-lobby` を追加し、通常時はインストーラーのウィンドウとMonoGameの描画ループを作らずロビーを起動して終了する。
- ショートカットからバージョン別フォルダーのロビーexeを直接指定しない。更新・旧版削除後も固定配置の起動入口を維持する。
- ロビーの起動先解決と現在版→直前版のフォールバックは、既存のエンジン側起動処理を共用する。
- ロビー未導入、または現在版・直前版とも起動不能の場合は、理由を表示してインストーラーの導入・復旧画面へ案内する。無断でダウンロードを始めず、起動要求を無言で捨てない。
- 引数なし起動は管理画面を開く。ロビーからインストーラーを開くときに `--launch-lobby` を付けて往復起動しない。
- 通常のロビー起動では毎回ネットワーク確認を必須にしない。導入済みならオフラインで起動できる経路を保つ。
- 管理画面用の単一起動制御と起動専用要求を区別する。管理画面が開いていてもロビー起動を受け付ける。管理画面の再要求は既存画面への案内・前面化を含めて扱う。
- 更新途中の配置を起動しない。既存の現在版切り替え手順を調査し、必要なら更新と起動の同期を追加する。
- 起動専用モードで起動したロビーが、インストーラープロセスの終了とともに終了しないことを確認する。

### ショートカット

- 標準表示名は［きふわらべの碁２０２６］。TargetPathは固定配置のインストーラーexe、Argumentsは `--launch-lobby`、WorkingDirectoryはその配置先とする。
- 説明はロビーを開く用途を示し、アイコンは製品のアイコンを使用する。パスと引数を別々に設定し、空白・日本語を含むパスで検査する。
- 現在の作成確認を［ロビーを起動するショートカットをデスクトップに作りますか？］という内容に変更する。
- 導入後にロビー用ショートカットを作成できる入口を確保する。既存のロビー内更新経路だけでなく、新規ユーザーがZIPから導入した場合の作成導線も確認する。
- 既存の製品用ショートカットはTargetPath・Argumentsを照合して移行する。利用者が管理用として残すショートカットとは区別する。
- 名前だけが一致する別アプリのショートカットを上書きしない。移行対象は既存の製品exeを指すと確認できたものとする。
- 一時ファイルへ作成→リンク先と引数の検証→置換とし、失敗時に元のリンクを残す。更新自体の成功とショートカット作成失敗を区別して伝える。

## 実装結果と検証

- 変更前・変更後とも、`dotnet build KifuwarabeGo2026.slnx -c Release` 相当の全体ビルドが警告0・エラー0。
- `dotnet run --project KifuwarabeGo2026.Tests.InstallerEngine -c Release --no-build` — PASS。旧設定、現在版・直前版、更新と起動の排他、更新後の再試行、JSON Linesの正常・異常系を確認。
- `dotnet run --project KifuwarabeGo2026.Tests.GameOasis.Gui.Windows -c Release --no-build` — PASS。`.lnk` のターゲット・起動引数、繰り返し作成、旧標準リンクの移行、独自管理リンクの保護を確認。
- `dotnet run --project KifuwarabeGo2026.Tests.GameOasis.Gui.Portability -c Release --no-build` — PASS。
- InstallerGuiとJsonLinesHostを `artifacts/installer-smoke` へpublishし、旧名互換ファイルを同梱。ZIPへ圧縮して `artifacts/installer-extracted` に展開した。
- `dotnet run --project KifuwarabeGo2026.Tests.GameOasis.Gui.Windows -c Release -- --installer-package artifacts/installer-extracted 4.0.7` — PASS。展開した配布物の必須ファイルとバージョン検証。
- `dotnet run --project KifuwarabeGo2026.Tests.GameOasis.Gui.Windows -c Release -- --installer-upgrade artifacts/installer-smoke.zip 4.0.7` — PASS。GitHub応答をローカルfixtureへ置き換え、新規導入、旧Launcher名アセットへのフォールバック、旧名だけのCurrentからの更新、チェックサム不一致時の既存版維持、固定ショートカットの継続、旧インストーラー削除後のリンクを検査。実際の公開GitHubへの通信は行わない。
- `dotnet run --project KifuwarabeGo2026.Tests.InstallerEngine -c Release --no-build -- --installer-entry-smoke artifacts/installer-extracted` — PASS。新旧両apphost、空白・日本語を含む保存先、管理画面Mutex取得中のロビー起動、現在版不在から直前版へのフォールバック、新旧環境変数の引き渡しを実プロセスで確認。
- Windows実画面: Installer／Lobbyの起動とボタン表示を撮影。隔離パッケージで［START LOBBY］→実ロビー→［インストーラーを起動］を操作し、ロビーが閉じて既存の管理画面へ戻ることを確認。画像は `artifacts/installer-ui/installer.png` と `lobby.png`。実行時に用いた補助スクリプトも同じartifacts配下にあり、配布対象ではない。
- 検査中に、未設定の特殊フォルダーで起動できない問題と、アイコン未設定の旧ショートカットを移行できない問題を見つけて修正し、再検査した。
- リリーススクリプトのPowerShell構文検査、文書リンク検査、差分検査を実施。GitHubへの公開や実ユーザーの導入先の更新は行っていない。

保存先を分ける起動例: `KifuwarabeGo2026.Installer.exe --local-application-data <directory>`。日常利用の標準ショートカットは `KifuwarabeGo2026.Installer.exe --launch-lobby`。`--engine-stdio` との併用時も指定保存先をホストへ渡す。

## 元の検討経緯

[分割前の計画・調査記録](../Archive/InstallerRename.md)
