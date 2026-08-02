# Kifuwarabe Go 2026 v3.2.0

Go Appごとに対応エンジンを確認できる共通選択画面と、エンジン自身が対応アプリを公開する`kfw-list-apps`を追加したリリースです。観戦画面と連解析画面の表示も整理しました。

## Go App対応エンジンの検出

- Engineへ`kfw-list-apps`を追加し、同梱エンジンが対応する`play`と`ponnuki`を1行ずつ返すようにしました
- `known_command`と`list_commands`から`kfw-list-apps`の対応を確認できます
- アプリIDは1単語ならlowercase、複数単語ならlowerCamelCaseとする共通規約を追加しました
- ローカル対局では`play`、ポン抜きゲームでは`ponnuki`への対応状況をエンジンごとに確認します
- `kfw-list-apps`未実装の従来型GTPエンジンは、後方互換のためGo Play用として扱います
- コマンドを実装していて対象アプリを返さないエンジンや、能力照会に失敗したエンジンは、一覧へ表示したまま選択不可にします

## 共通エンジン選択画面

- ローカル対局とポン抜きゲームで、対象Go Appを指定して使う共通GTPエンジン選択ダイアログを使用します
- ポン抜きゲームからも、エンジンの追加、編集、複製、削除、並べ替えができます
- 対応状況と選択できない理由を、一覧とプロパティ欄へ表示します
- ポン抜き開始画面では、選択済みエンジンを読み取り専用で表示し、独立した［SELECT PROVIDER ENGINE］ボタンから選択画面を開きます
- 画面用語を［問題提供エンジン］から［アプリ提供エンジン］へ統一しました

## 観戦・解析画面の改善

- CGOS観戦中は、既に観戦している対局に対する［対局を観る］ボタンを表示せず、見えない領域のクリック判定も無効にしました
- 連解析の表示中は、解析図と重なっていた最終着手の印を非表示にしました

## テスト状況

- ソリューション全体のReleaseビルド
- 移植性スモークとWindowsスモーク
- GUI版とEngine版のWindows x64 publish
- GUI、Core、Shared、Match、GtpExtensions、CGOS、Engineのファイルバージョン確認
- 同梱CGOS通信コンポーネントとEngine GTP応答のスモークテスト

## 配布物

- GUI版: `KifuwarabeGo2026.Gui-v3.2.0-win-x64.zip`
- Engine版: `KifuwarabeGo2026.Engine-v3.2.0-win-x64.zip`

GUIとEngineの両方を使う場合は、2つともダウンロードしてください。

## SHA-256

- GUI版: `8BE37DBE62E5B0964EFF45E19C465D8F35415A921E97869B981103B1280A58A0`
- Engine版: `621EECC9F2FBDC069F7F029D4E527057F80791AA89E76350FBCCADB7C5D301DA`

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime
- フレームワーク依存配置
