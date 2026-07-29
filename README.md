# きふわらべの碁２０２６

［きふわらべの碁２０２６］ は、（人間同士の対局や、人間の対局の観戦の用途ではなく）［コンピューター囲碁の思考エンジン開発のため］に用途を絞って作成した、コンピューター囲碁アプリケーションです。  

例えば、電気通信大学の［コンピュータ囲碁オープン２０２６］や、インターネット上のフリーのCGOSサーバー（コンピューター囲碁対局サーバー）に参加可能な程度の機能をサポートできる実績があります。  

開発者は［むずでょ］（人間）です。主に［Codex］、たまに［Copilot］（ＡＩエージェント）と一緒に開発しています。  

![スクリーンショットv2.5.0](./RepositoryAssets/Screenshots/スクリーンショット-20260728-0102-v250-対局画面.png)  
（画面は開発中のものです。 v2.5.0）  

![スクリーンショットv2.5.0](./RepositoryAssets/Screenshots/スクリーンショット-20260728-0104-v250-チャートポップアップ.png)  
（画面は開発中のものです。 v2.5.0）  

![スクリーンショットv2.5.0](./RepositoryAssets/Screenshots/スクリーンショット-20260728-0105-v250-盤面編集.png)  
（画面は開発中のものです。 v2.5.0）  


## リンク

- [リリースページ](https://github.com/muzudho/KifuwarabeGo2026/releases)
- [開発日誌 2026年7月](./KifuwarabeGo2026.Gui/Docs/開発/開発日誌/2026-07.md)


## 主な機能

- 9路、13路、19路の囲碁盤表示
- 人間対人間、人間対コンピューター、コンピューター対コンピューターの対局
- Go Text Protocol (GTP) による外部思考エンジン連携
- 同梱のランダム合法手 GTP エンジン `Kifuwarabe Random GTP`
- 大会ルール設定の追加、編集、複製、削除
- GTP エンジン設定の追加、編集、複製、削除
- SGF 棋譜の読み込み、局面編集、棋譜レビュー
- `R` キーによる連解析表示
- CGOS サーバーへの接続（CGF Open 2026 への参加の実績有り）


## 動作環境

- Windows
- .NET SDK 10.0.302、または互換性のある新しい10.0 feature band

アプリケーションの対象フレームワークと利用者向けランタイムは.NET 8です。開発用SDKには、ソリューションの `.slnx` 形式を扱える.NET 10を使用します。

> [!Note]
> 現在、作者が動作確認できる環境はWindowsだけです。Linux版やmacOS版への移植協力を歓迎しています。詳しくは[きふわらべの碁2026・移植の手引き](./KifuwarabeGo2026.Gui/Docs/設計/きふわらべの碁2026・移植の手引き.md)をご覧ください。


## 起動方法

```powershell
dotnet run --project KifuwarabeGo2026.Gui.Windows\KifuwarabeGo2026.Gui.Windows.csproj
```

GTP エンジン単体を確認する場合:

```powershell
@('protocol_version','name','version','boardsize 9','clear_board','play black D4','genmove white','quit') | dotnet run --project KifuwarabeGo2026.Engine\KifuwarabeGo2026.Engine.csproj
```

CGOS 練習サーバーへ接続する場合:

```powershell
dotnet run --project KifuwarabeGo2026.Gui.Communication.Cgos -- --account black
dotnet run --project KifuwarabeGo2026.Gui.Communication.Cgos -- --account white
```

黒番・白番の両方を同じ端末から接続する場合:

```powershell
dotnet run --project KifuwarabeGo2026.Gui.Communication.Cgos -- --both
```


## リリースビルド

```powershell
dotnet publish KifuwarabeGo2026.Gui.Windows\KifuwarabeGo2026.Gui.Windows.csproj -c Release -r win-x64 --self-contained false
dotnet publish KifuwarabeGo2026.Engine\KifuwarabeGo2026.Engine.csproj -c Release -r win-x64 --self-contained false
```

出力先:

- `KifuwarabeGo2026.Gui.Windows\bin\Release\net8.0-windows\win-x64\publish`
- `KifuwarabeGo2026.Engine\bin\Release\net8.0\win-x64\publish`

GUI の publish 時には、CGOS 通信コンポーネントも `Tools\Cgos` 以下へ自動的に publish されます。
`KifuwarabeGo2026.Gui.Core.dll` はGUI版へ、`KifuwarabeGo2026.Shared.dll` はGUI版とEngine版の両方へ自動的に含まれます。


## ドキュメント

- [共有ドキュメント](./KifuwarabeGo2026.Gui/Docs/README.md)
- [きふわらべ式SGF形式仕様](./KifuwarabeGo2026.Gui/Docs/設計/きふわらべ式SGF形式仕様.md)
- [きふわらべの碁2026・移植の手引き](./KifuwarabeGo2026.Gui/Docs/設計/きふわらべの碁2026・移植の手引き.md)
