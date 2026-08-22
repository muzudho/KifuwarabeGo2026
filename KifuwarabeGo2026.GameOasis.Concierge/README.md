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

## まだ含まないもの

- Protocol GによるGUI接続
- Protocol Pによるプレイヤー接続
- Protocol Mによるゲームマスター接続
- 参加者の認証、権限、手番要求
- 時計、棋譜、操作監査
- 別プロセス探索、起動、再接続

現在の`ApplyActionAsync`は、Protocol G、P、Mを接続する前の中核確認用APIです。将来は参加者と権限を確認したコマンドだけがこの経路へ到達するようにします。
