# Core・Windows分割後スモークテスト

最終更新: 2026-07-29

## 自動確認済み

- `dotnet restore KifuwarabeGo2026.slnx` が成功。
- 通常出力先で `dotnet build KifuwarabeGo2026.slnx --no-restore` が成功。
- 7プロジェクトすべて警告0、エラー0。
- Core単独の `net8.0` ビルドが成功。
- Windows起動プロジェクトのビルドが成功。
- `win-x64` publishが成功。
- publish出力に次が存在する。
  - `KifuwarabeGo2026.Gui.exe`
  - `KifuwarabeGo2026.Gui.Core.dll`
  - `KifuwarabeGo2026.Shared.dll`
  - `Content/Fonts/Ui.xnb`
  - `Content/Fonts/BoardCoordinate.xnb`
  - `Tools/Cgos/KifuwarabeGo2026.Gui.Communication.Cgos.exe`
- 新WindowsプロジェクトのDebug版を起動できた。
- プロセスが応答中で、ウィンドウタイトルは `Kifuwarabe Go 2026`。
- Content読込後もタイトル画面を維持した。
- クリック入力が新しいGUI操作ログへ記録された。
- 最新のアプリケーションエラーログに例外記録はない。
- ログ保存先は分割前と同じリポジトリ直下の `Logs/Gui`。
- `KifuwarabeGo2026.Gui.PortabilitySmoke` のDebug／Release実行が `PASS`。
- Windows・Ubuntu・macOSでCoreを検査するGitHub Actionsを追加済み。
- Windows CIでsolution全体のReleaseビルド、GUI publish、必須配布ファイル、同梱CGOSの `--help` を確認するジョブを追加済み。
- GitHub-hosted runner上の初回結果はpush後に確認する。
- Windows CIと同じRID付きrestore、GUI publish、9個の必須配布ファイル検査、同梱CGOSの `--help` をローカルで実行し、成功済み。
- `KifuwarabeGo2026.Gui.WindowsSmoke` で8個のWindowsサービス生成、`.exe`命名、実行ファイルフィルター、文字PNG生成、折返しページ画像、アセンブリ名、Coreとのバージョン一致、埋込みアイコン資源を確認済み。
- Windows非対話スモークはRelease構成で `PASS`。

## 作業中に修正したこと

- Visual Studioが保持していた旧solution状態から、削除済み `KifuwarabeGo2026.Gui.csproj` の参照が `.slnx` へ戻っていたため、Core／Windowsを含む構成へ修正した。
- `.slnx` に重複したWindowsプロジェクト参照を除去した。
- 新しいWindowsプロジェクトからMonoGameの `dotnet-mgcb` を復元できるよう、ローカルツールマニフェストを追加した。
- Core用の通常 `project.assets.json` を再生成するため、solutionをrestoreし直した。
- `.slnx` のCLI対応に合わせ、開発用SDKを.NET 10.0.302へ統一した。対象フレームワークと利用者向けランタイムは.NET 8のまま。
- solution restoreだけでは `win-x64` 用assetsがなく `NETSDK1047` になったため、CIへGUIのRID付きrestoreを追加した。
- Windows固有実装の非対話確認を繰り返せるよう、専用スモークプロジェクトをsolutionへ追加した。

## 手動確認すること

- タイトル画面と各画面の表示が崩れていない。
- タスクバーとウィンドウのアイコンが従来どおり表示される。
- ローカル対局を開始できる。
- SGFの読込・保存ダイアログが動く。
- GTPエンジンの実行ファイル・作業フォルダーを選択できる。
- GTPオプションの文字列・数値入力ダイアログが動く。
- パスをクリップボードへコピーできる。
- GUIログをVS Codeまたは既定アプリで開ける。
- 設定ファイルの場所をExplorerで表示できる。
- CGOS通信コンポーネントを探索・起動できる。
- CGOSログを開く、フォルダーを開く、PowerShellで追尾する操作が動く。

## 起動コマンド

```powershell
dotnet run --project KifuwarabeGo2026.Gui.Windows\KifuwarabeGo2026.Gui.Windows.csproj
```

既に起動しているDebug版がある場合は二重起動を避ける。
