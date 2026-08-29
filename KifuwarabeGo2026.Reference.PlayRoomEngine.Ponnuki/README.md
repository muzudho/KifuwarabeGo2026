# KifuwarabeGo2026.Reference.PlayRoomEngine.Ponnuki

Protocol S v1.0を実装する、ポン抜きプレイスペースの公式参照実装です。

このプロジェクトは次だけを参照します。

```text
KifuwarabeGo2026.GameOasis.Contracts
```

GUI、MonoGame、ゲームコンシェルジュ、現行`Engine`、`Match`、`Shared`は参照しません。別リポジトリーで作られるプレイスペースと同じ条件を保ちます。

## 文書スキーマID

```text
io.github.muzudho.kifuwarabego2026.games.ponnuki.configuration.v1
io.github.muzudho.kifuwarabego2026.games.ponnuki.action.v1
io.github.muzudho.kifuwarabego2026.games.ponnuki.state.v1
io.github.muzudho.kifuwarabego2026.games.ponnuki.event.v1
io.github.muzudho.kifuwarabego2026.games.ponnuki.outcome.v1
```

設定では、盤サイズ、ランダム初期着手数、乱数シード、捕獲目標数、開始手番、明示的な初期配置を指定できます。明示的な初期配置とランダム初期着手を組み合わせることもできます。

現在の参照実装は`play`と`pass`を受け付け、いずれかの捕獲数が設定された目標へ到達すると終了します。
