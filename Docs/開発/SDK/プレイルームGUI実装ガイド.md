# プレイルームGUI実装ガイド

状態：プレースホルダー

## この文書の読者

Kifuwarabe Go 2026へ新しい［プレイルームGUI］、いわゆる［ゲームのGUI］を追加したい開発者を対象とします。ゲームの状態を表示し、人間の入力をゲームオアシスへ渡す実装を扱います。

## 接続境界

GUIとゲームオアシスの公開境界はProtocol Gです。ゲームルールとの接続には、Concierge越しのProtocol SまたはPlay Room公開契約を使用します。

```text
プレイルームGUI
  ↓ Protocol G
GameOasis Concierge
  ↓ Protocol S
プレイルームエンジン
```

.NET実装の現在の契約は次にあります。

* `KifuwarabeGo2026.GameOasis.Contracts.ProtocolG.IGuiProtocol`
* `KifuwarabeGo2026.GameOasis.Contracts.PlayRoom.PlayRoomLaunchRequest`
* `KifuwarabeGo2026.GameOasis.Contracts.PlayRoom.PlayRoomLaunchResult`
* 現在の同一プロセス起動境界：`KifuwarabeGo2026.PlayRoom.Launching.IPlayRoomLauncher`。

## 最小実装手順（予定）

1. 対応するゲームID、部屋種別、Protocol版、表示能力を記述する。
2. `PlayRoomLaunchRequest`から自己記述的な設定、初期状態、参加者を復元する。
3. Protocol Gでセッションへ接続し、状態と通知を購読する。
4. 人間の入力をゲーム固有の行動文書へ変換して送る。
5. 終了結果と診断を表示し、Lobbyへ安全に戻る。
6. Engine切断、Protocol不一致、不正状態を画面全体の異常終了にしない。

## 実装してはいけない責務

* 合法手、手番、終局、勝敗の正本を持つこと。
* Lobbyのカタログやインストール情報を直接保存すること。
* GTP、CGOS、SGF等の外部仕様を画面状態へ直接漏らすこと。FormalAdapterまたは中立契約を利用します。
* 特定のPlay Room Engine具象アセンブリを直接参照すること。

## この文書へ今後追加する内容

* Protocol Gの状態遷移とメッセージ一覧。
* Play Room GUIマニフェストと探索方法。
* MonoGameを使う参照GUIと、別言語GUIの最小例。
* Match、Board Editor、Review、Watchの共通点と差分。
* 入力、描画、通知、切断、再接続の適合性試験。
* アクセシビリティ、ローカライズ、画面サイズの最低要件。

## 関連文書

* [`ロビー・プレイルーム4役物理分割計画.md`](../../開発_作業計画/ロビー・プレイルーム４役物理分割計画.md)
* [`PlaySpace外部実装SDK.md`](./PlaySpace外部実装SDK.md)

