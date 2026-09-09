# きふわらべの碁２０２６ 開発者向けREADME

この文書は、ソースコードを取得してビルド、テスト、改造、移植、リリースする開発者向けです。

exeをダウンロードして使う方は、[利用者向けREADME](./README.md)をご覧ください。

## 開発環境

- .NET SDK 10.0.302、または互換性のある新しい10.0 feature band
- 対象フレームワーク: .NET 8

開発用SDKには、ソリューションの `.slnx` 形式を扱える.NET 10を使用します。アプリケーション本体と利用者向けランタイムは.NET 8です。

> [!Note]
> 現在、作者が動作確認できる環境はWindowsだけです。Linux版やmacOS版への移植協力を歓迎しています。
> 詳しくは [Linux・macOSへの移植の手引き](Docs/Dev/CurrentState/PortingGuide.md) をご覧ください。

## ソリューション構成

| プロジェクト | 役割 |
| --- | --- |
| `KifuwarabeGo2026.GameOasis.Gui` | OS非依存のGUI Core |
| `KifuwarabeGo2026.GameOasis.Gui.Windows` | Windows起動部分とWindows固有サービス |
| `KifuwarabeGo2026.Engine` | GTP思考エンジン |
| `KifuwarabeGo2026.Shared` | GUIとEngineの共有コード |
| `KifuwarabeGo2026.Reference.Communication.Cgos.Host` | CGOS通信コンポーネント |
| `KifuwarabeGo2026.Tests.GameOasis.Gui.Portability` | OS非依存部分の回帰検査 |
| `KifuwarabeGo2026.Tests.GameOasis.Gui.Windows` | Windows固有部分の回帰検査 |

## ビルド

```powershell
dotnet build KifuwarabeGo2026.slnx
```

## GUIを開発実行する

```powershell
dotnet run --project KifuwarabeGo2026.GameOasis.Gui.Windows\KifuwarabeGo2026.GameOasis.Gui.Windows.csproj
```

## GTPエンジンを確認する

```powershell
@('protocol_version','name','version','boardsize 9','clear_board','play black D4','genmove white','quit') |
    dotnet run --project KifuwarabeGo2026.Reference.PlayerEngine.Go.Gtp.Host\KifuwarabeGo2026.Reference.PlayerEngine.Go.Gtp.Host.csproj
```

## スモークテスト

```powershell
dotnet run --project KifuwarabeGo2026.Tests.GameOasis.Gui.Portability\KifuwarabeGo2026.Tests.GameOasis.Gui.Portability.csproj
dotnet run --project KifuwarabeGo2026.Tests.GameOasis.Gui.Windows\KifuwarabeGo2026.Tests.GameOasis.Gui.Windows.csproj
```

## リリースビルド

```powershell
dotnet publish KifuwarabeGo2026.GameOasis.Gui.Windows\KifuwarabeGo2026.GameOasis.Gui.Windows.csproj -c Release -r win-x64 --self-contained false
dotnet publish KifuwarabeGo2026.Reference.PlayerEngine.Go.Gtp.Host\KifuwarabeGo2026.Reference.PlayerEngine.Go.Gtp.Host.csproj -c Release -r win-x64 --self-contained false
```

GUIのpublish時には、CGOS通信コンポーネントも `Tools\Cgos` 以下へ自動的にpublishされます。

詳しい検査、ZIP作成、GitHub Release公開は、[リリース手順](./Docs/Dev/リリース手順.md)に従ってください。

## 開発文書

- [共有ドキュメントの目次](./Docs/README.md)
- [開発者向けドキュメントの目次](./Docs/Dev/README.md)
- [開発日誌](./Docs/Dev/Log/README.md)
- [現在の開発状況](Docs/Dev/CurrentState/Overview.md)
- [後続作業・将来構想](Docs/Dev/Plans/README.md)
- [構成と責務の入口](Docs/Dev/CurrentState/Architecture.md)
- [CGOS・GTP・SGFの境界と移行状況](Docs/Dev/CurrentState/FormalAdapterMigration.md)
- [きふわらべ式SGF形式仕様](Docs/Dev/CurrentState/SgfAnalysisFormat.md)
- [Linux・macOSへの移植の手引き](Docs/Dev/CurrentState/PortingGuide.md)
- [Windows GUI手動スモークテスト](./Docs/Dev/Windows%20GUI手動スモークテスト手順.md)

Linux版やmacOS版への移植協力を歓迎しています。
