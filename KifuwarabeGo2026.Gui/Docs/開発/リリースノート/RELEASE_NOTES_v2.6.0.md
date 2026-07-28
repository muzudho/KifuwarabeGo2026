# Kifuwarabe Go 2026 v2.6.0

Windows版の使い勝手を磨きながら、将来LinuxやmacOSへ移植する人がOS固有実装を差し替えやすい構成へ整理したリリースです。

## 主な変更

- GUIをOS非依存のCoreとWindows固有実装へ分離
- 起動する実行ファイル名は `KifuwarabeGo2026.Gui.exe` を維持
- ファイル選択、クリップボード、外部アプリ起動、ダイアログ、文字画像化などをインターフェース化
- Linux/macOS移植の入口と差し替え対象をまとめた移植の手引きを追加
- Ubuntu、macOS、Windowsで移植性を検査するCIと、Windows固有実装のスモークテストを追加
- ローカル対局の最新局面ラベルを `LIVE` から `CURRENT` へ変更
- ScoreチャートのY軸を評価値に応じて自動拡張
- 密集して線状になっていた黄色いコメント印を廃止
- 前後のコメントへ直接移る頭出しボタンを追加
- コメント番号、総コメント数、着手番号、コメント内ページ番号を分けて表示
- コメント頭出し時に盤面、チャート、コメントを同期

作者の確認環境はWindowsだけです。Linux版やmacOS版への移植に協力してくださる方を歓迎します。

## テスト状況

- 自動ビルドとスモークテストを実施
- Windows GUI手動スモークテストのラウンドA・Bは全項目OK
- ラウンドC以降は未実施で、今後の回帰確認項目として継続

## 配布物

- GUI版: `KifuwarabeGo2026.Gui-v2.6.0-win-x64.zip`
- Engine版: `KifuwarabeGo2026.Engine-v2.6.0-win-x64.zip`

GUIとEngineの両方を使う場合は、2つともダウンロードしてください。

## SHA-256

- GUI版: `D9E42E147CD4C0D92E8D6CDA0607688661193063ACC25CEDEE34EBCE0A218E44`
- Engine版: `2F1D58659F641A7D41C75BA53B49850959550D63A9CAC6C0607C92286619A878`

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime
- フレームワーク依存配置
