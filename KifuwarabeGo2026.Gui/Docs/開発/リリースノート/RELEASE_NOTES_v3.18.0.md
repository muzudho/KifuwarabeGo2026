# Kifuwarabe Go 2026 v3.18.0

CGOSのプラクティス対局と、人間プレイヤーによるオンライン対局に対応したリリースです。エントリーとClient Identityの管理画面も改善しました。

> [!IMPORTANT]
> 通常利用者は `KifuwarabeGo2026.Launcher-v3.18.0-win-x64.zip` をダウンロードしてください。`.zip.sha256` はランチャーが自動検証に使う機械向けファイルです。GUI版・Engine版ZIPは単独利用・互換運用向けです。

## CGOS対局

- `PRACTICE PLAYER`を任意で接続し、自分のエントリー同士によるCGOS練習対局を行えるようにしました。
- HumanエントリーでCGOSへ接続し、盤面のクリック、PASS、RESIGNで対局できるようにしました。
- Humanの自着手を観戦盤へ即時反映し、着手音と消費時間も正しく更新します。
- 予定外に始まったプラクティス対局を検出し、対局情報の表示と確認付き投了を行えるようにしました。
- CGOS接続画面のPASSWORDを伏字表示し、各プレイヤーで個別に目アイコンから表示を切り替えられるようにしました。

## エントリーとClient Identity

- Client IdentityへCOMMENTを追加し、HANDLEとPASSWORDの用途をメモできるようにしました。
- エントリー編集画面でClient Identityを最大5件まで直接管理できるようにしました。
- Client Identityの追加、削除、パスワード表示切替、IMEを含むテキスト編集を改善しました。
- エントリーのPLAYER、HANDLE、PASSWORD、ENGINEを共通の縦区画へ整理しました。

## リリース工程

- `Invoke-Release.ps1`から中央バージョンを更新できるようにしました。
- リリースノートの版番号見出しを検査し、異なる版のノートによる誤公開を防止します。

## 対応環境と配布物

- 正式配布: Windows x64
- 必要環境: .NET 8 Desktop Runtime
- `KifuwarabeGo2026.Launcher-v3.18.0-win-x64.zip`
- `KifuwarabeGo2026.Gui-v3.18.0-win-x64.zip`
- `KifuwarabeGo2026.Engine-v3.18.0-win-x64.zip`

通常利用者にはLauncher版を入口として推奨します。GUI版・Engine版の単独ZIPも互換用に配布します。

## テスト状況

- Releaseビルド成功
- LauncherSmoke成功
- PortabilitySmoke成功
- WindowsSmoke成功
- Windows x64向けLauncher版・GUI版・Engine版のpublish成功

## SHA-256

- Launcher版: `1FAC1BA5D4969CE444FC09ABECAB4533FD40B91002EDF35C443358F700D350BC`
- GUI版: `9DBF31C2E7441268269AABC8F91F2B37A40784ED2B0F8A4C33453B444C202CDF`
- Engine版: `6DF8AD6A833F6E33AE5BFE247E093839DDB3F0F0DE70F9A34D988FB6593BDD5B`
