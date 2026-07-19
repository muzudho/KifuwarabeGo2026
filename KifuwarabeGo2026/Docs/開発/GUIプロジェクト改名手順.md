# GUIプロジェクト改名手順

## 目的

GUI プロジェクトを次のように改名します。

- フォルダー: `KifuwarabeGo2026` → `KifuwarabeGo2026.Gui`
- プロジェクトファイル: `KifuwarabeGo2026.csproj` → `KifuwarabeGo2026.Gui.csproj`
- 出力ファイル: `KifuwarabeGo2026.exe` → `KifuwarabeGo2026.Gui.exe`

`KifuwarabeGo2026.Engine` と `KifuwarabeGo2026.Communication.Cgos` は改名しません。C# の名前空間も今回は `KifuwarabeGo2026` のままとします。名前空間まで一緒に変えると変更範囲が大きくなり、プロジェクト名を分かりやすくする目的とは別の作業になるためです。

## この手順の進め方

一度に全部実行せず、各ステップを一つずつ実行します。各ステップが終わったら Codex に「ステップ N が終わったぜ」と伝え、状態を確認してもらってから次へ進んでください。

コマンドはリポジトリ直下の次の場所で実行します。

```text
E:\github.com\muzudho\KifuwarabeGo2026
```

エラーが出た場合は次のステップへ進まず、エラー全文を Codex に見せてください。

## ステップ0: 作業前の状態を確認する

PowerShell で次を実行します。

```powershell
Set-Location E:\github.com\muzudho\KifuwarabeGo2026
git status --short
```

既存の変更は消したり戻したりしません。特に個人用チャット文書の変更と、先に追加した実行ファイル用アイコンの変更は保持します。

Codex への報告:

```text
ステップ0が終わったぜ。git statusを確認してくれだぜ
```

## ステップ1: ロックしそうなアプリを閉じる

次を手動で閉じます。

1. `KifuwarabeGo2026.exe`
2. `KifuwarabeGo2026.Engine.exe`
3. Visual Studio
4. Visual Studio Code など、プロジェクト内のファイルを開いているエディター
5. リポジトリ内をカレントフォルダーにしている別の PowerShell やコマンドプロンプト

Codex と作業している現在の PowerShell は、リポジトリ直下にいれば閉じなくて構いません。`KifuwarabeGo2026` フォルダーの中をカレントフォルダーにしてはいけません。

閉じた後、残っている関連プロセスを確認します。

```powershell
Get-Process | Where-Object {
    $_.ProcessName -like 'KifuwarabeGo2026*' -or
    $_.ProcessName -like 'devenv*'
} | Select-Object ProcessName, Id, Path
```

何も表示されなければ準備完了です。プロセスが表示された場合は勝手に強制終了せず、まず通常の画面操作で閉じます。

Codex への報告:

```text
ステップ1が終わったぜ。ロックしそうなプロセスを確認してくれだぜ
```

## ステップ2: GUIプロジェクトのフォルダーを改名する

リポジトリ直下で次を実行します。

```powershell
Rename-Item -LiteralPath '.\KifuwarabeGo2026' -NewName 'KifuwarabeGo2026.Gui'
```

確認します。

```powershell
Get-Item -LiteralPath '.\KifuwarabeGo2026.Gui' | Select-Object FullName
Test-Path -LiteralPath '.\KifuwarabeGo2026'
```

新フォルダーのパスが表示され、`Test-Path` が `False` なら成功です。この手順書もフォルダーと一緒に次の場所へ移動します。

```text
KifuwarabeGo2026.Gui\Docs\開発\GUIプロジェクト改名手順.md
```

`使用中` や `アクセスが拒否されました` と表示された場合は、繰り返し実行せずステップ1へ戻ります。

Codex への報告:

```text
ステップ2が終わったぜ。フォルダー名を確認してくれだぜ
```

## ステップ3: プロジェクトファイルを改名する

```powershell
Rename-Item -LiteralPath '.\KifuwarabeGo2026.Gui\KifuwarabeGo2026.csproj' -NewName 'KifuwarabeGo2026.Gui.csproj'
```

確認します。

```powershell
Get-Item -LiteralPath '.\KifuwarabeGo2026.Gui\KifuwarabeGo2026.Gui.csproj' | Select-Object FullName
```

Codex への報告:

```text
ステップ3が終わったぜ。.csproj名を確認してくれだぜ
```

## ステップ4: プロジェクト名と出力名を明示する

ここからのテキスト編集は Codex に依頼します。GUI の `.csproj` の最初の `PropertyGroup` に次を追加します。

```xml
<AssemblyName>KifuwarabeGo2026.Gui</AssemblyName>
<RootNamespace>KifuwarabeGo2026</RootNamespace>
```

`AssemblyName` により exe 名が `KifuwarabeGo2026.Gui.exe` になります。`RootNamespace` は既存の C# 名前空間を維持する意図を明示します。

Codex への依頼:

```text
ステップ4をやってくれだぜ。GUIのAssemblyNameとRootNamespaceを設定してくれだぜ
```

## ステップ5: プロジェクト参照とパスを直す

Codex に次の修正を依頼します。

1. `KifuwarabeGo2026.slnx`
   - `KifuwarabeGo2026/KifuwarabeGo2026.csproj`
   - → `KifuwarabeGo2026.Gui/KifuwarabeGo2026.Gui.csproj`
2. `KifuwarabeGo2026.Engine/KifuwarabeGo2026.Engine.csproj`
   - `..\KifuwarabeGo2026\Domain\*.cs`
   - → `..\KifuwarabeGo2026.Gui\Domain\*.cs`
3. `tools/Generate-ExecutableIcons.ps1`
   - GUI アイコンの出力先を `KifuwarabeGo2026.Gui\GuiIcon.ico` にする
4. ルートの `README.md`
   - 実行、発行、出力先、ドキュメントへのリンクを新しいパスへ直す
5. 個人用のリリース手順と開発日誌
   - 旧 `.csproj` パスや旧出力パスだけを新しいパスへ直す

Codex への依頼:

```text
ステップ5をやってくれだぜ。slnx、EngineのDomain共有、アイコン生成、README、文書中の旧パスを直してくれだぜ
```

## ステップ6: 古いパスの残りを検索する

Codex に検索を依頼するか、次を実行します。

```powershell
rg -n --glob '!**/bin/**' --glob '!**/obj/**' --glob '!artifacts/**' --glob '!Release/**' 'KifuwarabeGo2026[\\/]KifuwarabeGo2026\.csproj|KifuwarabeGo2026\.csproj|\.\.\\KifuwarabeGo2026\\Domain' .
```

検索結果がゼロになるのが目標です。ただし GitHub のリポジトリ名、C# の名前空間、製品名としての `KifuwarabeGo2026` は正しいため、一律に置換してはいけません。

Codex への報告:

```text
ステップ6をやってくれだぜ。古いプロジェクトパスの取り残しだけを調べてくれだぜ
```

## ステップ7: 中間生成物を掃除する

このステップではソースファイルを削除しません。改名前のビルド結果だけを掃除します。

まず対象を確認します。

```powershell
Get-ChildItem -LiteralPath '.\KifuwarabeGo2026.Gui\bin', '.\KifuwarabeGo2026.Gui\obj' -ErrorAction SilentlyContinue |
    Select-Object FullName
```

表示された場所が `KifuwarabeGo2026.Gui` 内の `bin` と `obj` だけであることを確認してから、Codex に削除を依頼します。手作業で削除する場合も、リポジトリ直下やプロジェクト本体を対象にしてはいけません。

Codex への依頼:

```text
ステップ7をやってくれだぜ。GUIプロジェクト内のbinとobjだけ確認してから削除してくれだぜ
```

## ステップ8: ソリューション全体をビルドする

```powershell
dotnet build .\KifuwarabeGo2026.slnx
```

目標は次の通りです。

- ビルド成功
- 警告 0
- エラー 0
- `KifuwarabeGo2026.Gui.exe` が生成される
- `KifuwarabeGo2026.Engine.exe` も引き続き生成される

確認例:

```powershell
Get-Item -LiteralPath `
    '.\KifuwarabeGo2026.Gui\bin\Debug\net8.0-windows\KifuwarabeGo2026.Gui.exe', `
    '.\KifuwarabeGo2026.Engine\bin\Debug\net8.0\KifuwarabeGo2026.Engine.exe' |
    Select-Object FullName, Length, LastWriteTime
```

Codex への報告:

```text
ステップ8が終わったぜ。ビルド結果とexe名を確認してくれだぜ
```

## ステップ9: GUIを起動して動作確認する

```powershell
dotnet run --project '.\KifuwarabeGo2026.Gui\KifuwarabeGo2026.Gui.csproj'
```

次を確認します。

1. GUI が起動する
2. ウィンドウの囲碁盤アイコンが維持されている
3. ローカル対局の最初の画面まで進める
4. GTP エンジン選択に `KifuwarabeGo2026.Engine.exe` を使用できる

確認後は GUI を通常操作で終了します。

Codex への報告:

```text
ステップ9が終わったぜ。GUIの起動確認ができたぜ
```

## ステップ10: Git上の改名を確認する

```powershell
git status --short
git diff --stat
```

Git は最初、削除と追加として表示することがあります。内容が同じファイルは、コミット時または `git diff --summary -M` で改名として認識されます。

確認します。

```powershell
git diff --summary -M
```

既存のユーザー変更が消えていないことも確認します。問題がなければ改名作業は完了です。コミットは別途、内容を確認してから行います。

Codex への報告:

```text
ステップ10が終わったぜ。最終状態を確認してくれだぜ
```

## ロックで改名できない場合

次の順で対処します。

1. エラーメッセージを保存する
2. Visual Studio と GUI を閉じたか再確認する
3. フォルダー内を開いているエクスプローラーを一度別の場所へ移動する
4. 別の端末のカレントフォルダーが対象フォルダー内でないか確認する
5. 数秒待って `Rename-Item` を一度だけ再実行する
6. まだ失敗する場合は再起動する前に Codex へ相談する

`Stop-Process -Force`、フォルダーの強制削除、`git reset --hard` は使いません。

## 完了条件

- ソリューション上の GUI プロジェクト名が `KifuwarabeGo2026.Gui`
- GUI の出力名が `KifuwarabeGo2026.Gui.exe`
- GTP エンジンの出力名が `KifuwarabeGo2026.Engine.exe`
- 囲碁盤とロボットのアイコンが維持されている
- ソリューション全体のビルドが警告 0、エラー 0
- 旧プロジェクトパスの参照が残っていない
- 作業前からあったユーザーの変更が保持されている
