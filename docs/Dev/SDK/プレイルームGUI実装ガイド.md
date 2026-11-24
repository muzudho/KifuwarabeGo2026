# プレイルームGUI実装ガイド

状態：プレースホルダー

2026-09-09：標準入出力による画面なしの接続例は[External.PlayRoomClient](../../../Samples/External.PlayRoomClient/README.md)を参照してください。外部Pythonクライアント→公式ホストと、公式.NETクライアント→外部Python Board Editorの両方向を試験できます。以下のProtocol Gによる画面付きGUI全体の構想とは到達範囲が異なります。

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

## 文書の拡充計画

予定項目は[SDK整備計画](../Plans/SdkDevelopment.md)で管理します。

## 関連文書

* [Lobby・Play Room分離の到達点](../CurrentState/LobbyPlayRoomSeparation.md)
* [`PlaySpace外部実装SDK.md`](./PlaySpace外部実装SDK.md)
