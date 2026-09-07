# Kifuwarabe Go 2026 v3.14.0

GUI の更新状況、CGOS 終局後のレビュー、コンピューター対局中の盤面表示を改善したリリースです。

## GUI 更新

- ［最新バージョンへ更新］の処理工程をモーダルダイアログへ表示します。
- リリース確認、ダウンロード、展開、検証、更新版起動の進捗を確認できます。
- 更新に失敗した場合は、調査に使うログファイルの場所を表示します。

## CGOS と棋譜レビュー

- CGOS の終局後に共通の棋譜レビュー画面を開き、最終局面の RESULT を表示します。
- レビュー画面から CGOS の自動 SGF 保存を切り替えられます。
- レビュー終了後は CGOS 接続画面へ戻ります。

## 描画と診断

- コンピューター同士の高速対局でも、各着手後の盤面を最低 1 フレーム描画してから次の着手へ進みます。
- クリックから画面状態変更、最初のフレーム提示までの経過時間をログへ記録します。

## 配布物

- GUI版: `KifuwarabeGo2026.GameOasis.Gui-v3.14.0-win-x64.zip`
- Engine版: `KifuwarabeGo2026.Engine-v3.14.0-win-x64.zip`

GUIとEngineの両方を使う場合は、2つともダウンロードしてください。

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime
- フレームワーク依存配布

## テスト状況

- Releaseビルド成功（警告0、エラー0）
- PortabilitySmoke、WindowsSmoke成功
- 同梱CGOS通信コンポーネントの `--help` 成功
- EngineのGTP基本応答とversion `3.14.0`を確認
- Windows x64向けGUI版・Engine版をpublish
- GUI、Core、Engineのファイルバージョン `3.14.0.0`を確認
- 配布ZIPにPDBが含まれないことを確認

## SHA-256

- GUI版: `FCC4BB02C085B473D768E255B4021EA042A82084DAB9501E12175CA9DDDF3103`
- Engine版: `0F3032C709AD3A26C4E45EA4DF9465989A5F322C51485955DBBC286459C1ABBB`
