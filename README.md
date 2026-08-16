# きふわらべの碁２０２６

［きふわらべの碁２０２６］ は、［コンピューター囲碁の思考エンジン同士の対局］ができるアプリケーションです。  

例えば、電気通信大学の［コンピュータ囲碁フォーラムオープン２０２６］や、インターネット上のフリーのCGOSサーバー（コンピューター囲碁対局サーバー）に参加可能な程度の機能をサポートできる実績があります。  

開発者は［むずでょ］（人間）です。主に［Codex］、たまに［Copilot］（ＡＩエージェント）と一緒に開発しています。  

![スクリーンショットv2.5.0](./RepositoryAssets/Screenshots/スクリーンショット-20260728-0102-v250-対局画面.png)  
（画面は開発中のものです。 v2.5.0）  

![スクリーンショットv2.5.0](./RepositoryAssets/Screenshots/スクリーンショット-20260728-0104-v250-チャートポップアップ.png)  
（画面は開発中のものです。 v2.5.0）  

![スクリーンショットv2.5.0](./RepositoryAssets/Screenshots/スクリーンショット-20260728-0105-v250-盤面編集.png)  
（画面は開発中のものです。 v2.5.0）  


## ダウンロード

- [最新版をダウンロードする](https://github.com/muzudho/KifuwarabeGo2026/releases/latest)

### v3.10.0 以前から更新する方へ

v3.10.0 の［最新バージョンへ更新］は GUI 用 ZIP だけを取得する旧方式のため、共通ランチャーへ自動移行できません。
リリースページから最新版を手動でダウンロードし、別のフォルダーへすべて展開して、`KifuwarabeGo2026.Launcher.exe` を起動してください。
古いショートカットはランチャーへのショートカットに作り直してください。旧更新機能が保存した版は、ランチャーのインストール済みバージョン画面でアンインストールできます。

### 使わなくなったバージョンの削除

`KifuwarabeGo2026.Launcher.exe` のインストール済みバージョン画面では、GUI、Engine、および旧更新機能が `%LOCALAPPDATA%\KifuwarabeGo2026\Updates` に保存した版をアンインストールできます。
現在使用中の版とロールバック用の直前版は、誤削除を防ぐためアンインストールできません。

### 共通ランチャー

`KifuwarabeGo2026.Launcher.exe` を毎回の起点として使用します。ランチャーでは次の操作ができます。

- current GUIの起動。起動できない場合はprevious GUIへフォールバックします。
- GUIとEngineの個別更新、または両方の更新確認。
- current Engineの保存場所を開く。
- インストール済み版と旧更新機能が残した版の確認・アンインストール。
- `%LOCALAPPDATA%\KifuwarabeGo2026\Logs\launcher.log` による障害調査。

更新中はcurrentを変更しません。ダウンロード、展開、必須ファイル、ファイルバージョンの検証にすべて成功した後だけ、`launcher-settings.json` のcurrentとpreviousを切り替えます。
旧更新機能の `%LOCALAPPDATA%\KifuwarabeGo2026\Updates` は自動移行しません。不要ならランチャーで削除し、必要な版はランチャーから正式に再取得してください。


## 囲碁エンジンを作りたい人向け

［きふわらべの碁２０２６］で動くGTPエンジンを作るための目次です。外部のGTP仕様書を探さなくても実装へ取り掛かれる公開リファレンスを用意しています。

1. [Play（通常の囲碁対局）](./KifuwarabeGo2026.Gui/PublicDocs/GoApps/Play/README.md)
2. [ポン抜き](./KifuwarabeGo2026.Gui/PublicDocs/GoApps/Ponnuki/README.md)


## きふわらべの碁２０２６の開発に参加したい方

- [リポジトリ開発者向けREADME](./README.developer.md)


## 主な機能

- 9路、13路、19路の囲碁盤表示
- 人間対人間、人間対コンピューター、コンピューター対コンピューターの対局
- Go Text Protocol (GTP) による外部思考エンジン連携
- 同梱のランダム合法手 GTP エンジン `Kifuwarabe Random GTP`
- 大会ルール設定の追加、編集、複製、削除
- GTP エンジン設定の追加、編集、複製、削除
- SGF 棋譜の読み込み、局面編集、棋譜レビュー
- `L` キーで切り替える `BOARD LENS` 表示
- CGOS サーバーへの接続（CGF Open 2026 への参加の実績有り）
- 大会ルール、思考エンジン、CGOS接続先を使いたい順に並べ替える順序編集


## 動作環境

- Windows x64
- .NET 8 Desktop Runtime

プログラム開発用のSDKやVisual Studioは必要ありません。

> [!Note]
> 現在、作者が動作確認している環境はWindowsだけです。


## 起動方法

1. [リリースページ](https://github.com/muzudho/KifuwarabeGo2026/releases/latest)を開きます。
2. 画面を使う場合は `KifuwarabeGo2026.Gui-v～-win-x64.zip` をダウンロードします。
3. 思考エンジンも使う場合は `KifuwarabeGo2026.Engine-v～-win-x64.zip` もダウンロードします。
4. ZIPファイルを右クリックし、［すべて展開］で展開します。
5. `KifuwarabeGo2026.Gui.exe` をダブルクリックします。

GUI版とEngine版の両方を使う場合は、両方のZIPが必要です。

思考エンジンを別のフォルダーへ展開した場合は、GUIのエンジン設定画面で `KifuwarabeGo2026.Engine.exe` を選択してください。

起動時にWindowsから確認画面が出た場合は、ダウンロード元がこのGitHubリポジトリのリリースページであることを確認してから操作してください。
