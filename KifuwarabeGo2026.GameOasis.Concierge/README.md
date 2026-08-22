# KifuwarabeGo2026.GameOasis.Concierge

Game Oasisのゲームコンシェルジュ中核です。Protocol S実装を登録し、具体的なゲームを知らずにセッションを仲介します。

## 依存方針

このプロジェクトが参照するGame OasisプロジェクトはContractsだけです。

```text
GameOasis.Concierge ──→ GameOasis.Contracts
GameOasis.Concierge ──×──→ Reference.PlaySpace.Ponnuki
GameOasis.Concierge ──×──→ Reference.GUI
```

プレイスペース実装は、アプリケーションの組み立て場所から`IPlaySpaceProtocol`として登録します。同一プロセス実装でも、別プロセスへの通信アダプターでも同じように扱えます。

## 現在の範囲

- プレイスペースの登録と一覧
- Protocol Sバージョンの互換性確認
- Game Oasisセッションとプレイスペースセッションの対応付け
- 設定検証とセッション生成
- 状態取得と行動適用の仲介
- セッション終了
- 同一セッション内の操作直列化
- Protocol Gによるカタログ、設定スキーマ、セッション、状態、行動、終了の公開
- Protocol Pによるプレイヤー登録、役割への割り当て、着手要求、適用結果通知、参加終了

## まだ含まないもの

- Protocol Mによるゲームマスター接続
- 参加者の認証、権限、手番要求
- 時計、棋譜、操作監査
- 別プロセス探索、起動、再接続

現在のProtocol G `SubmitActionAsync`は、Protocol P、Mを接続する前の中核確認用APIです。将来は参加者と権限を確認したコマンドだけがこの経路へ到達するようにします。

現段階では、Game Oasisセッションを閉じる前に、そのセッションへ割り当てたProtocol Pプレイヤーを`UnbindPlayerAsync`で参加終了させる必要があります。セッション終了時の一括参加解除は、Protocol Mと運営ライフサイクルを設計するときに統合します。
