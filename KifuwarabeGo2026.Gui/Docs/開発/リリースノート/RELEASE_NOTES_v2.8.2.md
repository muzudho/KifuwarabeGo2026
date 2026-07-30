# Kifuwarabe Go 2026 v2.8.2

CGOS観戦へ戻る導線、テキスト選択、小型トレンドチャートを使いやすくしたリリースです。

## 主な変更

- 進行中のCGOS観戦から `LEAVE VIEW` で接続画面へ戻ったあと、「対局を観る」通知を再表示
- 再表示された通知から、進行中のCGOS観戦画面へ何度でも戻れるように改善
- テキストボックスのダブルクリックで文字列全体をハイライト選択
- 小型トレンドチャートのWinRate Y軸を `100% / EVEN / 100%` の3ラベルへ整理
- 小型トレンドチャートのScore Y軸を黒側最大値、`EVEN`、白側最大値の3ラベルへ整理
- ポップアップチャートの5段階Y軸ラベルは従来どおり維持

## テスト状況

- 7プロジェクト全体のReleaseビルドを実施
- 移植性スモークとWindowsスモークを実施
- テキストボックスのダブルクリック全選択を自動回帰検査

## 配布物

- GUI版: `KifuwarabeGo2026.Gui-v2.8.2-win-x64.zip`
- Engine版: `KifuwarabeGo2026.Engine-v2.8.2-win-x64.zip`

GUIとEngineの両方を使う場合は、2つともダウンロードしてください。

## SHA-256

- GUI版: `9E12883117FFCF69D26766E47EC6D9DBAF115F42BFD3CC27A308F5CCBF3FD14E`
- Engine版: `573E43B453BF02BFF6C05C27C15A2A75F76BBC4324C2BBCD207B85823F591A3F`

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime
- フレームワーク依存配置
