# Protocol P v1.0

Protocol Pは、ゲームコンシェルジュと人間またはコンピューターのプレイヤー実装を接続します。

## ライフサイクル

```text
Describe
StartSession
    ├─ SelectAction
    ├─ NotifyAction
    └─ EndSession
```

`StartSession`は、一つのプレイヤー実装をGame Oasisセッション内の一つの役割へ割り当てます。同じ実装が複数のゲームまたは複数の役割を担当できるよう、割り当てごとに`PlayerBindingId`を発行します。

## ゲーム固有データ

プレイヤーは`PlayerGameObservation`で現在状態を受け取り、`ContractDocument`として行動を返します。Contractsは黒石、白石、盤座標などを所有しません。

`RoleId`はプレイスペースが定める安定した役割名です。ポン抜き参照実装では`black`と`white`を使用します。

## 状態同期

- `SelectAction`は、選択の前提にしたリビジョンを返します。
- コンシェルジュはそのリビジョンをProtocol Sの楽観的競合検出へ渡します。
- 自分の行動を含む適用結果は、参加中の全プレイヤーへ`NotifyAction`で通知します。
- 拒否された行動も、現在状態と拒否理由を通知できます。
- `OperationalState`が`Paused`の間、コンシェルジュは新しい着手要求を行いません。
- 盤面の`Revision`と、停止・再開を管理する`OperationRevision`は独立しています。

## v1.0で未確定の事項

- 思考時間とキャンセル期限
- 中間解析、候補手、評価値のストリーミング
- プレイヤーからの投了を通常行動とするか専用操作とするか
- 接続切れからの再参加と状態再送
- プレイヤー能力とプレイスペース要求能力の詳細な照合
- 人間プレイヤーをProtocol G経由でPへ接続する標準アダプター
