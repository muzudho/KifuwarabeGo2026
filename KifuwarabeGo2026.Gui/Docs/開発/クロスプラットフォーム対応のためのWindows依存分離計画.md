# クロスプラットフォーム対応のための Windows 依存分離計画

最終更新: 2026-07-28

## 目的

正式な対応環境と配布物は Windows のままとしつつ、将来ほかの開発者が Linux 版や macOS 版を作る際に、ゲーム本体を書き換えず OS 固有機能だけを差し替えられる構成へ段階的に移行する。

実行時に任意の DLL を探索するプラグイン方式は採用しない。まずは通常のプロジェクト参照と依存性注入を使い、コンパイル時に対象 OS の実装を選ぶ。動的ロードは、実装を別配布する要求が生じた場合に改めて検討する。

## 決定事項

- OS 固有機能は、小さなインターフェースを介して利用する。
- `Game1`、画面描画、対局進行へ OS 判定を散在させない。
- Windows 固有 API と WinForms を Windows 実装プロジェクトへ集約する。
- 巨大な `IPlatformService` 一個にせず、役割別に分ける。
- Windows 実装のプロジェクト名は `KifuwarabeGo2026.Gui.Windows` を第一候補とする。
- 将来は `KifuwarabeGo2026.Gui.Linux`、`KifuwarabeGo2026.Gui.Mac` を同じ並びで追加できる形にする。
- Windows 実装を別プロジェクトへ移した後も、`AssemblyName` を `KifuwarabeGo2026.Gui` に指定し、利用者が起動するファイル名を `KifuwarabeGo2026.Gui.exe` のまま維持する。
- 既存の設定保存先と publish 内容も可能な限り維持する。

## 実装状況

### 2026-07-28: 第1段階の一部を完了

- `IClipboardService` を追加した。
- `IMessageDialogService` を追加した。
- `WindowsClipboardService` を `Infrastructure/Windows` へ追加し、`user32.dll` と `kernel32.dll` の呼び出しを集約した。
- `WindowsMessageDialogService` を `Infrastructure/Windows` へ追加し、WinForms `MessageBox` の呼び出しを集約した。
- `Program` を現在の構成地点とし、Windows 実装を生成して `Game1` へ渡すようにした。
- `Game1` と `TournamentRulesSetting` のクリップボード直接呼び出しを置換した。
- `Game1` の警告メッセージ直接表示を置換した。
- 旧 `Application/SystemClipboard.cs` を削除した。
- ソリューション全体の Debug ビルドが警告 0、エラー 0 で成功した。

### 2026-07-28: ファイル・フォルダー選択の分離を完了

- `IFileDialogService` を追加した。
- `OpenFileDialogOptions`、`SaveFileDialogOptions`、`FolderDialogOptions`、`FileDialogFilter` を OS 非依存の契約として追加した。
- `WindowsFileDialogService` に WinForms の Open、Save、FolderBrowser 実装を集約した。
- `Program` から `Game1` へファイルダイアログサービスを注入した。
- SGF の読込・保存をサービス経由へ置換した。
- GTP エンジン実行ファイルとファイル名オプションの選択をサービス経由へ置換した。
- GTP エンジン作業フォルダー、ログ保存先、SGF 保存先の選択をサービス経由へ置換した。
- WinForms の `OpenFileDialog`、`SaveFileDialog`、`FolderBrowserDialog` は `Infrastructure/Windows/WindowsFileDialogService.cs` だけに残っている。
- ソリューション全体の Debug ビルドが警告 0、エラー 0 で成功した。

### 2026-07-28: 文字列・整数入力の分離を完了

- `ITextInputDialogService` を追加した。
- `TextInputDialogOptions` と `IntegerInputDialogOptions` を OS 非依存の契約として追加した。
- `WindowsTextInputDialogService` に WinForms の文字列・整数入力フォームを移した。
- `Program` から `Game1` へ入力ダイアログサービスを注入した。
- GTP エンジンの文字列オプションと数値オプション編集をサービス経由へ置換した。
- 初期値、文字列最大長、数値の最小値・最大値、キャンセル時に値を変更しない挙動を維持した。
- WinForms の `Form`、`TextBox`、`NumericUpDown`、入力フォーム用 `Button` は `Infrastructure/Windows/WindowsTextInputDialogService.cs` だけに残っている。
- ソリューション全体の Debug ビルドが警告 0、エラー 0 で成功した。

### 2026-07-28: デスクトップ外部起動の分離を完了

- `IDesktopLauncher` と `DesktopOpenResult` を追加した。
- `WindowsDesktopLauncher` に既定アプリ、Notepad、VS Code、Explorer、PowerShellを使う処理を集約した。
- `Program` から `Game1` へデスクトップランチャーを注入した。
- GUIエラーログを開く処理、GUIログをVS Codeまたは既定アプリで開く処理、設定ファイルの場所をExplorerで示す処理をサービス経由へ置換した。
- `CgosConnectionProcess` に同じサービスを注入し、CGOSログを開く、ログフォルダーを開く、ログを追尾する操作を置換した。
- GTPエンジンとCGOS通信コンポーネントの起動は、標準入出力を制御する子プロセス通信であるため、デスクトップランチャーへ混ぜず専用クラスに残した。
- Notepad、Explorer、PowerShellの実行ファイル名は `Infrastructure/Windows/WindowsDesktopLauncher.cs` だけに残っている。
- ソリューション全体の Debug ビルドが警告 0、エラー 0 で成功した。

### 2026-07-28: 文字画像生成の分離を完了

- `ITextRasterizer` を追加した。
- OS非依存の入力を「文字列、ピクセル高、太字、描画領域、行間、ページ」とし、出力を透明背景のPNGバイト列とした。
- `WindowsTextRasterizer` にMeiryo、WinForms `TextRenderer`、`System.Drawing` の描画と折返し計測を集約した。
- `Program` から `Game1`、`GoScreenRenderer` へ文字ラスタライザーを注入した。
- GTPエンジン設定の動的オプション文字列をサービス経由へ置換した。
- 棋譜コメントの折返し、ページ数計算、ページ画像生成も同じサービス経由へ置換した。
- Presentation配下から `System.Windows.Forms` と `System.Drawing` の直接参照がなくなった。
- 実行中の `KifuwarabeGo2026.Gui.exe` が通常のDebug出力先をロックしていたため、TEMP内の別出力先へGUIプロジェクトをビルドし、警告0、エラー0を確認した。

まだ Windows 実装プロジェクトの分割は行っていない。残る主な画像系依存は `Infrastructure/WindowIcon.cs` の `System.Drawing` とSDL2呼び出しである。次はウィンドウアイコン適用をサービス境界へ移し、プロジェクト分割の事前条件を再確認する。

## 目標構成

```text
KifuwarabeGo2026.Gui.Core
    MonoGame のゲーム本体、画面、対局進行
    OS 非依存のインターフェース
    TargetFramework: net8.0

KifuwarabeGo2026.Gui.Windows
    Windows 用エントリーポイント
    WinForms、クリップボード、外部起動など
    TargetFramework: net8.0-windows
    UseWindowsForms: true

将来追加:
KifuwarabeGo2026.Gui.Linux
KifuwarabeGo2026.Gui.Mac
```

既存の `KifuwarabeGo2026.Gui` を一度に改名・分割すると、MonoGame Content、publish、CGOS 同梱、実行ファイル名へ同時に影響する。このため、サービス境界を作ってからプロジェクトを分ける。

## 現在確認できている Windows 依存

- `Game1.cs`
  - ファイルを開く、保存する、フォルダーを選ぶ WinForms ダイアログ
  - 警告メッセージボックス
  - 文字列入力、数値入力ダイアログ
- `Application/SystemClipboard.cs`
  - `user32.dll`、`kernel32.dll` の直接呼び出し
- `Presentation/Local/Resting/EngineSelect/GoScreenRenderer.GtpEngine.cs`
  - WinForms `TextRenderer` による文字画像生成
- `KifuwarabeGo2026.Gui.csproj`
  - `net8.0-windows`
  - `UseWindowsForms=true`

`ProcessStartInfo` は共通 API だが、シェル、引数、実行ファイル探索の OS 差を吸収する境界が必要になる。`WindowIcon` の SDL2 呼び出しは複数 OS で利用できる可能性があるため、各 OS で検証してから共通側か OS 別側かを決める。

## 用意するインターフェース

```csharp
public interface IFileDialogService
{
    string? OpenFile(FileDialogOptions options);
    string? SaveFile(FileDialogOptions options);
    string? SelectFolder(FolderDialogOptions options);
}

public interface IClipboardService
{
    bool TrySetText(string text);
}

public interface IMessageDialogService
{
    void ShowWarning(string title, string message);
}

public interface ITextInputDialogService
{
    string? Prompt(string title, string currentValue, int maximumLength);
    int? PromptNumber(string title, int currentValue, int minimum, int maximum);
}

public interface IDesktopLauncher
{
    bool TryOpenFile(string path);
    bool TryOpenDirectory(string path);
    bool TryOpenUrl(string url);
}
```

ファイルフィルターなどには WinForms の型を使わず、OS 非依存のオプション型を定義する。Linux や macOS の GUI ライブラリが非同期操作を要求する場合は、ダイアログ API を `Task<T>` に変更する。

## 実装段階

### 第1段階: サービス境界を作る

1. OS 非依存インターフェースとオプション型を追加する。
2. 現在の処理を移した Windows 実装クラスを作る。
3. 起動地点で Windows 実装を生成し、`Game1` へコンストラクター注入する。
4. `Game1` と設定画面から WinForms、`SystemClipboard` の直接呼び出しを除く。
5. 既存動作と配布物が変わらないことを確認する。

最初は同一プロジェクトの `Platform/Windows` フォルダーへ実装を置いてよい。共通コードから WinForms への直接参照が消えた時点で別プロジェクトへ移す。

### 第2段階: Windows 実装プロジェクトを分ける

1. `KifuwarabeGo2026.Gui.Windows` を追加する。
2. Windows 実装、エントリーポイント、専用リソースを移す。
3. Windows プロジェクトだけを `net8.0-windows`、`UseWindowsForms=true` とする。
4. 共通 GUI コードを `net8.0` の `KifuwarabeGo2026.Gui.Core` へ移す。
5. MonoGame Content のビルド場所と出力先を調整する。
6. `AssemblyName`、アイコン、バージョン、設定保存先を維持する。
7. GUI publish 時の CGOS `Tools/Cgos` 同梱処理を Windows 起動プロジェクトへ移す。

Windows 起動プロジェクトには次を明示する。

```xml
<AssemblyName>KifuwarabeGo2026.Gui</AssemblyName>
```

これにより、プロジェクト名が `KifuwarabeGo2026.Gui.Windows` でも、利用者が起動するファイルは `KifuwarabeGo2026.Gui.exe` になる。

既存名を Windows 実行プロジェクトと共通ライブラリのどちらに残すかは、実行ファイル名と publish 手順への影響を調査してから決める。

### 第3段階: Windows 依存を閉じ込める

1. 共通側から `System.Windows.Forms` 参照をなくす。
2. 共通側から `user32.dll`、`kernel32.dll` の P/Invoke をなくす。
3. OS 差がある外部起動処理を `IDesktopLauncher` へ移す。
4. WinForms `TextRenderer` を MonoGame または OS 別サービスへ移す。
5. SDL2 のアイコン設定を各 OS で検証する。

### 第4段階: 移植者向けの足場を作る

本物の Linux/macOS GUIを作る前に、テスト用フェイクまたは `UnsupportedPlatformServices` を用意する。これにより共通 GUI が Windows API なしでコンパイルできることを確認する。

利用できない機能は即時例外ではなく、画面上で「この環境では未対応」と通知できる設計にする。

## プロジェクト名について

`KifuwarabeGo2026.Windows` でも技術的には問題ないが、将来 Engine や通信部品にも OS 固有実装が現れた場合に対象範囲が曖昧になる。そのため GUI のデスクトップ機能だと分かる名前を推奨する。

```text
KifuwarabeGo2026.Gui.Core
KifuwarabeGo2026.Gui.Windows
KifuwarabeGo2026.Gui.Linux
KifuwarabeGo2026.Gui.Mac
```

## 今回採用しない方式

### 実行時 DLL 差し替え

.NET アセンブリは Windows、Linux、macOS で利用できるが、手動差し替え方式には DLL の探索、API バージョン互換、ロード失敗時の復旧、安全性、単一ファイル publish との整合が必要になる。対象 OS ごとに再ビルドできるオープンソースプロジェクトでは、通常のプロジェクト参照の方が単純で安全である。

### 共通コード内の大量の OS 判定

`OperatingSystem.IsWindows()` を機能ごとに置くと、差分がゲーム本体へ広がり、移植者が変更箇所を見つけにくくなるため採用しない。

### 条件付きコンパイルだけで一プロジェクトに詰め込む

`#if WINDOWS` は小さな差には使えるが、ダイアログ一式や起動処理の分離には向かない。OS 別プロジェクトを境界とし、条件付きコンパイルは必要最小限にする。

## 完了条件

### 第1段階

- `Game1` が WinForms ダイアログを直接生成していない。
- クリップボード利用箇所が `IClipboardService` を呼んでいる。
- Windows で従来のダイアログとクリップボードが動く。
- Debug ビルドが警告 0、エラー 0。

### 第2段階

- Windows 実装が `KifuwarabeGo2026.Gui.Windows` にある。
- 共通 GUI が `net8.0` でビルドできる。
- Windows 用 publish 手順が動作し、文書化されている。
- CGOS 通信コンポーネントが `Tools/Cgos` に同梱される。
- 実行ファイルのアイコン、バージョン、設定保存先が維持されている。

### 第3段階

- 共通 GUI に `System.Windows.Forms`、`user32.dll`、`kernel32.dll` がない。
- Windows 固有コードの所在が容易に分かる。
- テスト用実装を注入して Windows API なしで主要画面を開始できる。

## 回帰確認

各段階で最低限、次を確認する。

```powershell
dotnet build KifuwarabeGo2026.slnx --no-restore
dotnet publish <Windows用GUIプロジェクト> -c Release -r win-x64 --self-contained false
```

- ローカル対局を開始できる。
- SGF の読込と保存ができる。
- エンジン実行ファイルとフォルダーを選択できる。
- CGOS 通信コンポーネントを起動できる。
- パスをクリップボードへコピーできる。
- 警告、文字入力、数値入力ダイアログが動く。
- アイコン、バージョン、設定保存先が変わっていない。
- publish 後の `Tools/Cgos` が欠落していない。

## 次に着手する作業

1. `Infrastructure/WindowIcon.cs` の `System.Drawing`、SDL2 P/Invoke、MonoGameウィンドウハンドルの責務を調査する。
2. `IWindowIconService` などの境界を作り、`Game1` が静的なWindows寄り実装を直接呼ばない形にする。
3. Windows実装を `Infrastructure/Windows` へ移す。
4. SDL2部分を共通実装として再利用できるかは、Linux/macOSでのライブラリ名とハンドル取得方法を確認してから決める。
5. 共通候補コードからWinForms、System.Drawing、Windows P/Invoke、Windowsシェル名が消えているか再検索する。
6. GTP 実行ファイルフィルターの `*.exe` を、将来の OS 別実装が自然に置換できる意味的な指定へ変更するか検討する。

上記の後、`KifuwarabeGo2026.Gui.Core` と `KifuwarabeGo2026.Gui.Windows` の物理的なプロジェクト分割へ進めるか判断する。

## 引継ぎ時の注意

- 現在の正式対応 OS は Windows であり、Linux/macOS 対応済みとは記載しない。
- GUI csproj の CGOS 同梱処理を、分割時に移し忘れない。
- WinForms 型をインターフェースの引数や戻り値へ含めない。
- `Game1` は大きいため、サービス単位の小さな変更を優先する。
- 判断変更や調査結果はこの文書へ追記する。
