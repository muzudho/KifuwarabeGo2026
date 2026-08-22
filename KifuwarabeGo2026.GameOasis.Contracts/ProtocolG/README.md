# Protocol G v1.0

Protocol Gは、GUIとゲームコンシェルジュを接続します。

## 原則

- GUIは具体的なプレイスペース実装を参照しません。
- GUIはProtocol SのセッションIDを知りません。
- 画面座標、ボタン、色、MonoGame型を契約へ含めません。
- 「プレイスペースを選ぶ」「セッションを開始する」「行動を送る」などの意味的な操作を扱います。
- 状態、設定、行動、イベント、結果のゲーム固有部分は`ContractDocument`で運びます。

## 最小ライフサイクル

```text
GetPlaySpaces
GetConfigurationSchema
OpenSession
    ├─ GetSnapshot
    ├─ SubmitAction
    └─ CloseSession
```

## Protocol Sとの分離

Protocol Gの`GameOasisSessionId`はコンシェルジュが発行します。Protocol Sの`PlaySpaceSessionId`はコンシェルジュ内部に隠します。

```text
GUI ── GameOasisSessionId ──→ Concierge
Concierge ── PlaySpaceSessionId ──→ PlaySpace
```

Protocol GはGUI向けのカタログ項目とスナップショットを定義し、Protocol Sの具象応答をそのまま公開しません。

## v1.0で未確定の事項

- プッシュ通知またはイベント購読
- GUI再接続後のイベント再取得
- 人間プレイヤー操作をProtocol GとPのどちらへ所属させるか
- 人間ゲームマスター操作をProtocol GとMのどちらへ所属させるか
- 複数GUI、観戦GUI、権限別表示
- 大きな状態の差分配信

最初はポーリング可能な最小APIとし、現行GUIを接続しながら必要な通知を追加します。
