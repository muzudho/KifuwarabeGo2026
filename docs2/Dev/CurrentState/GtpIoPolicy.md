# GTP入出力と画面操作の方針

2026-07-07の基本方針から、GTPの入出力規則を整理しました。コマンド一覧は当初の候補であり、最新の全対応一覧ではありません。

## GTP 方針

コンピューター囲碁では、対局プロトコルとして Go Text Protocol (GTP) を使う。

- 標準入力から GTP コマンドを受け取る。
- 標準出力へ GTP 応答を返す。
- 標準エラー出力は使わない。
- 標準出力へデバッグログを出さない。

初期実装で扱う候補の GTP コマンド:

- `protocol_version`
- `name`
- `version`
- `known_command`
- `list_commands`
- `boardsize`
- `clear_board`
- `komi`
- `play`
- `genmove`
- `quit`

## アプリケーション操作方針

標準入力と標準出力は GTP 専用にするため、アプリケーション操作には使わない。

画面上の操作には以下を使う。

- マウス
- ボタン
- テキストボックス
- 必要に応じた画面内ログ表示

## 元の検討経緯

[分割前の計画・調査記録](../Archive/InitialDirection.md)
