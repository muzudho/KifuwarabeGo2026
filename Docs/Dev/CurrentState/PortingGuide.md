# GUI移植用の構成と検査範囲

[移植の実装・実機確認手順](../Plans/PortingImplementation.md) / [移植計画](../Plans/GuiPorting.md)

元文書の更新日: 2026-07-29。記載は当時の構成・検査範囲です。最新のプロジェクト名とビルド入口は[開発者README](../../../README.developer.md)を参照してください。

## はじめに

［きふわらべの碁2026］のGUI版は、現在Windowsだけを正式な動作対象にしています。

作者はWindows環境しか所有していないため、Linux版とmacOS版を作者自身で動作確認することはできません。その一方で、ほかの開発者がリポジトリをフォークし、LinuxやmacOSへ移植してくれることは大歓迎です。

移植しやすいように、ゲーム本体とWindows固有処理は別プロジェクトへ分けてあります。移植で分かったこと、修正、手順書、動作報告などの共有も歓迎します。

この文書は移植を保証するものではなく、移植作業の出発点を説明するものです。対象OSで必要になるパッケージ、ウィンドウシステム、ファイルダイアログ、フォントなどは、移植を担当する開発者が実機で確認してください。

## 最初に確認すること

リポジトリ直下の `global.json` は、開発用の基準SDKを.NET SDK `10.0.302` としています。同じ10.0系列の新しいfeature bandへのロールフォワードを許可し、プレビューSDKは選択しません。

アプリケーションの対象フレームワークと利用者向けランタイムは.NET 8のままです。開発用SDKに.NET 10を使う理由は、ソリューションの `.slnx` 形式をCLIで扱うためです。

使用されるSDKは次のコマンドで確認できます。

```powershell
dotnet --version
```

共通GUIがWindows APIなしでコンパイルできることは、次のコマンドで確認できます。

```powershell
dotnet build KifuwarabeGo2026.GameOasis.Gui\KifuwarabeGo2026.GameOasis.Gui.csproj
dotnet run --project KifuwarabeGo2026.GameOasis.Gui.PortabilitySmoke\KifuwarabeGo2026.GameOasis.Gui.PortabilitySmoke.csproj
```

2番目のコマンドはGUIウィンドウを開きません。移植用の最小構成がコンパイルできることだけを確認します。

LinuxやmacOSでは、シェルに合わせてパス区切りを `/` に読み替えてください。

移植性スモークは、次の項目を自動検査します。

- Coreの対象フレームワークが `net8.0` である。
- Coreが `System.Windows.Forms` と `System.Drawing.Common` を直接参照していない。
- CoreにP/Invokeメソッドが混入していない。
- 未実装機能の代替サービスが安全な値を返す。
- 対象OS用サービスを `Game1` へ注入する構成例がコンパイルできる。

検査に失敗した場合は、`FAIL:`に続けて理由を表示し、0以外の終了コードを返します。CIからも同じコマンドを利用できます。

## GitHub上の自動確認

`.github/workflows/portability.yml` は、プッシュ、プルリクエスト、手動実行をきっかけに次の3環境で移植性スモークを実行します。

- `ubuntu-latest`
- `macos-latest`
- `windows-latest`

各環境で `KifuwarabeGo2026.GameOasis.Gui.PortabilitySmoke` を復元、Releaseビルド、実行します。一つのOSで失敗しても、ほかのOSの結果を確認できるようにしています。

CIは.NET SDK `10.0.302` を明示的に導入します。`global.json`にも同じ基準バージョンを記載しているため、ローカルとCIのSDK選択を揃えられます。

3 OSのCore検査に加え、Windowsでは7プロジェクトのsolution全体をReleaseビルドし、Windows固有サービスの非対話スモークを実行してから、GUIを `win-x64` publishします。GUI、Core、Shared、Content、同梱CGOS通信コンポーネントの必須ファイルが揃っていることと、同梱CGOSの `--help` 起動を自動確認します。

このCIが成功しても、Linux版またはmacOS版のGUIが完成したことにはなりません。ウィンドウを開かないCoreの検査なので、画面、入力、音、フォント、ダイアログ、外部プロセスは対象OSの実機で確認してください。

## プロジェクト構成

### 共通GUI

`KifuwarabeGo2026.GameOasis.Gui/KifuwarabeGo2026.GameOasis.Gui.csproj`

- MonoGameのゲーム本体
- 画面表示と入力処理
- ローカル対局、棋譜、GTP、CGOS連携
- OS固有機能を呼び出すためのインターフェース
- 対象フレームワークは `net8.0`

### Windows起動プロジェクト

`KifuwarabeGo2026.GameOasis.Gui.Windows/KifuwarabeGo2026.GameOasis.Gui.Windows.csproj`

- Windows用エントリーポイント
- WinFormsのダイアログ
- Windowsのクリップボード、外部アプリ起動、文字画像生成
- ウィンドウアイコン
- Windows向けContentビルドとpublish

Windows版のプロジェクト名には `.Windows` が付きますが、利用者が起動するファイル名は `KifuwarabeGo2026.GameOasis.Gui.exe` です。

### 移植用スモークプロジェクト

`KifuwarabeGo2026.GameOasis.Gui.PortabilitySmoke/KifuwarabeGo2026.GameOasis.Gui.PortabilitySmoke.csproj`

- Windows APIを使わない最小実装
- `Game1`へプラットフォームサービスを渡す構成例
- CoreへWindows依存が戻っていないことを検査する実行可能な回帰チェック
- Linux版またはmacOS版の起動プロジェクトを作る際の雛形

ここにある無操作実装は、製品として利用するための実装ではありません。

## MonoGame Content

ゲーム本体はCoreにありますが、画像、フォント、効果音などのContentビルドは起動プロジェクトが担当します。

Windowsプロジェクトは、既存の `KifuwarabeGo2026.GameOasis.Gui/Content` と `KifuwarabeGo2026.GameOasis.Gui/Content/Content.mgcb` を参照しています。新しいOS用プロジェクトでも同じ素材と定義を再利用し、実行時に `Content.RootDirectory = "Content"` から読める配置を維持してください。

フォント、音声、画像形式の対応状況は対象OSで確認が必要です。

## 実行ファイルと外部プロセス

GTPエンジンとCGOS通信コンポーネントは、標準入力と標準出力を使う別プロセスとして起動します。

- Windowsでは通常、実行ファイル名に `.exe` が付きます。
- LinuxとmacOSでは、通常は `.exe` を付けません。
- 対象ファイルに実行権限が必要なOSでは、権限の設定とエラー案内が必要です。
- シェルで開く処理と、標準入出力を接続して起動する処理を混同しないでください。

実行ファイルの命名差は `IPlatformExecutableService` で吸収します。

## 関連資料

- [クロスプラットフォーム対応のためのWindows依存分離計画](../完了/20260728_クロスプラットフォーム対応のためのWindows依存分離計画.md)
- [Core・Windows分割後スモークテスト](../完了/20260729_Core・Windows分割後スモークテスト.md)
- [ソースコード概要とCGOS接続フロー](../Archive/SourceAndCgosFlow.md)

## 元の検討経緯

[分割前の計画・調査記録](../Archive/PortingGuide.md)
