# きふわらべの碁２０２６ 開発者向けREADME

この文書は、ソースコードを取得してビルド、テスト、改造、移植、リリースする開発者向けです。

exeをダウンロードして使う方は、[利用者向けREADME](./README.md)をご覧ください。

## 開発環境

- .NET SDK 10.0.302、または互換性のある新しい10.0 feature band
- 対象フレームワーク: .NET 8
- 作者の確認環境: Windows

開発用SDKには、ソリューションの `.slnx` 形式を扱える.NET 10を使用します。アプリケーション本体と利用者向けランタイムは.NET 8です。

## ソリューション構成

| プロジェクト | 役割 |
| --- | --- |
| `KifuwarabeGo2026.Gui` | OS非依存のGUI Core |
| `KifuwarabeGo2026.Gui.Windows` | Windows起動部分とWindows固有サービス |
| `KifuwarabeGo2026.Engine` | GTP思考エンジン |
| `KifuwarabeGo2026.Shared` | GUIとEngineの共有コード |
| `KifuwarabeGo2026.Gui.Communication.Cgos` | CGOS通信コンポーネント |
| `KifuwarabeGo2026.Gui.PortabilitySmoke` | OS非依存部分の回帰検査 |
| `KifuwarabeGo2026.Gui.WindowsSmoke` | Windows固有部分の回帰検査 |

## ビルド

```powershell
dotnet build KifuwarabeGo2026.slnx
```

## GUIを開発実行する

```powershell
dotnet run --project KifuwarabeGo2026.Gui.Windows\KifuwarabeGo2026.Gui.Windows.csproj
```

## GTPエンジンを確認する

```powershell
@('protocol_version','name','version','boardsize 9','clear_board','play black D4','genmove white','quit') |
    dotnet run --project KifuwarabeGo2026.Engine\KifuwarabeGo2026.Engine.csproj
```

## スモークテスト

```powershell
dotnet run --project KifuwarabeGo2026.Gui.PortabilitySmoke\KifuwarabeGo2026.Gui.PortabilitySmoke.csproj
dotnet run --project KifuwarabeGo2026.Gui.WindowsSmoke\KifuwarabeGo2026.Gui.WindowsSmoke.csproj
```

## リリースビルド

```powershell
dotnet publish KifuwarabeGo2026.Gui.Windows\KifuwarabeGo2026.Gui.Windows.csproj -c Release -r win-x64 --self-contained false
dotnet publish KifuwarabeGo2026.Engine\KifuwarabeGo2026.Engine.csproj -c Release -r win-x64 --self-contained false
```

GUIのpublish時には、CGOS通信コンポーネントも `Tools\Cgos` 以下へ自動的にpublishされます。

詳しい検査、ZIP作成、GitHub Release公開は、[リリース手順](./KifuwarabeGo2026.Gui/Docs/開発/リリース手順.md)に従ってください。

## 開発文書

- [共有ドキュメントの目次](./KifuwarabeGo2026.Gui/Docs/README.md)
- [作業再開時の引き継ぎ](./KifuwarabeGo2026.Gui/Docs/続きはここから.md)
- [基本方針](./KifuwarabeGo2026.Gui/Docs/設計/基本方針.md)
- [ソースコード概要とCGOS接続フロー](./KifuwarabeGo2026.Gui/Docs/設計/ソースコード概要とCGOS接続フロー.md)
- [きふわらべ式SGF形式仕様](./KifuwarabeGo2026.Gui/Docs/設計/きふわらべ式SGF形式仕様.md)
- [Linux・macOSへの移植の手引き](./KifuwarabeGo2026.Gui/Docs/設計/きふわらべの碁2026・移植の手引き.md)
- [Windows GUI手動スモークテスト](./KifuwarabeGo2026.Gui/Docs/開発/Windows GUI手動スモークテスト手順.md)

Linux版やmacOS版への移植協力を歓迎しています。
