# Kifuwarabe Go 2026 v3.17.0

タイトル画面の情報設計とエントリープロファイルの設定手順を整理し、対局準備を分かりやすくしたリリースです。

> [!IMPORTANT]
> 通常利用者は `KifuwarabeGo2026.Launcher-v3.17.0-win-x64.zip` をダウンロードしてください。`.zip.sha256` はランチャーが自動検証に使う機械向けファイルです。GUI版・Engine版ZIPは単独利用・互換運用向けです。

## タイトル画面

- ［ENTRY SETTINGS］［FORMAL APPS］［CASUAL APPS］の3区画へ整理しました。
- エンジンとエントリーを登録してから、ローカル対局、CGOS、カジュアルアプリへ進む導線を明確にしました。
- 各区画とアプリの説明をStickyNotesへまとめ、FORMAL APPSが他製GTPエンジンと有名な拡張仕様を利用する区画だと分かる説明へ変更しました。
- StickyNotesの高さを本文行数に合わせて自動調整します。

## エンジンとエントリーの管理

- タイトル画面から独立した管理モードとしてエンジン／エントリー登録を開けるようにしました。
- 選択中、対局で使用中、並び替え対象の表示を区別しました。
- エントリー編集画面を `ENTRY NAME`、`ENTRY TYPE`、`ENGINE`、`CLIENT IDENTITIES` の順へ再構成しました。
- `ENTRY TYPE` の［HUMAN］［ENGINE］で、保存後も種別を切り替えられます。
- HUMANへ切り替えてもエンジン選択を保持し、ENGINEへ戻したときに復元します。
- 新規エントリーの追加を［ADD］ボタン1個へ統合しました。
- Client IdentityをHANDLEとPASSWORDの組で最大5件まで編集でき、パスワードの個別表示、追加、削除ができます。
- ENTRY NAMEなどのテキスト編集で、カーソル移動、選択、Backspace、Deleteの動作を改善しました。

## 設定とランチャー

- アプリケーション設定画面に、効果音ごとのサウンドテストを追加しました。
- 共通ランチャーでインストール先フォルダーを選択できるようにしました。
- インストール済みバージョン一覧と保存先の表示を改善しました。

## 対応環境と配布物

- 正式配布: Windows x64
- 必要環境: .NET 8 Desktop Runtime
- `KifuwarabeGo2026.Launcher-v3.17.0-win-x64.zip`
- `KifuwarabeGo2026.Gui-v3.17.0-win-x64.zip`
- `KifuwarabeGo2026.Engine-v3.17.0-win-x64.zip`

通常利用者にはLauncher版を入口として推奨します。GUI版・Engine版の単独ZIPも互換用に配布します。

## テスト状況

- Releaseビルド
- LauncherSmoke
- PortabilitySmoke
- WindowsSmoke
- Windows x64向けLauncher版・GUI版・Engine版のpublish

## SHA-256

リリース成果物の作成後に記録します。
