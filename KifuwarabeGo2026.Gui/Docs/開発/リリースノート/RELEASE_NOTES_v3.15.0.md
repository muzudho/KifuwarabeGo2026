# Kifuwarabe Go 2026 v3.15.0

共通ランチャーを正式導入し、GUIとEngineの起動・更新・バージョン管理を一つの入口へまとめたリリースです。

## 共通ランチャー

- 文房具UIとMonoGame DesktopGLで構成した共通ランチャーを追加しました。
- GUIの起動、GUI・Engineの個別更新と一括更新、Engine保存場所の表示に対応しました。
- GUI起動に成功した後でランチャーを閉じるか、共有設定から選択できます。
- GUI、Engine、旧GUI更新版の容量を一覧表示し、不要な複数バージョンを削除できます。
- current、previous、実行中の版は削除できないよう保護します。
- 一覧取得中のローディング表示、削除確認、部分失敗結果を追加しました。

## 更新と安全性

- GitHub Releaseから厳密な名前のGUI・Engine ZIPを取得します。
- SHA-256、ZIP Slip、必須ファイル、バージョンを検証してからcurrentを切り替えます。
- staging、previous、設定の原子的保存により、失敗時に既存のcurrentを維持します。
- v3.10.0以前のGUI内更新から共通ランチャーへ自動移行できないため、READMEに手動再インストール手順を追加しました。

## 共通UIと設定

- 文房具UIを `KifuwarabeGo2026.StationeryUI` としてGUIから独立させ、GUIとランチャーで共有します。
- スクリーンショット保存先とランチャー終了設定を、全GUIバージョン・ランチャー共通の `application-settings.json` に保存します。
- GUIとランチャーの `Ctrl + P` で、`screenshot-yyyyMMdd-HHmmss-fff.png` を保存します。
- スクリーンショットのフラッシュ、シャッター表示、シャッター音をGUIとランチャーで共有します。
- ランチャーの設定画面からスクリーンショット保存先を変更できます。

## 対応環境と配布物

- 正式配布: Windows x64
- 必要環境: .NET 8 Desktop Runtime
- `KifuwarabeGo2026.Launcher-v3.15.0-win-x64.zip`
- `KifuwarabeGo2026.Gui-v3.15.0-win-x64.zip`
- `KifuwarabeGo2026.Engine-v3.15.0-win-x64.zip`

通常利用者にはLauncher版を入口として推奨します。GUI版・Engine版の単独ZIPも互換用に配布します。

## テスト状況

- Releaseビルド成功（警告0、エラー0）
- LauncherSmoke、PortabilitySmoke、WindowsSmoke成功
- 同梱CGOS通信コンポーネントの `--help` 成功
- EngineのGTP基本応答とversion `3.15.0`を確認
- Windows x64向けLauncher版・GUI版・Engine版をpublish
- 配布ZIPにPDBが含まれないことを確認

## SHA-256

- Launcher版: `6AD558C817F661DAF6A264CFB4E92ECAE235878105C2077947E696DBD512EA42`
- GUI版: `B7482C01583379AF1E7F2135642D12BDCC6B702F47B89BAC0AA0BFF357B80B9B`
- Engine版: `92D74926434E8B80AC3E5B979436445BB9906E54C0CEDAC2CF125A0A57C46C94`
