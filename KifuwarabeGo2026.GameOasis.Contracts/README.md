# KifuwarabeGo2026.GameOasis.Contracts

`Kifuwarabe Game Oasis`と、独立して開発できるプレイヤー、ゲームマスター、GUI、プレイスペースを接続する公開契約です。

## 依存方針

- Contractsは、Concierge、GUI、MonoGame、特定ゲーム、特定エンジンの実装を参照しません。
- 各実装からContractsへの一方向参照にします。
- 境界を越えるコマンド、応答、通知、識別子だけを置きます。
- ルール、状態変更、描画、通信プロセス制御、便利関数は置きません。

## 名前空間

```text
KifuwarabeGo2026.GameOasis.Contracts.Common
KifuwarabeGo2026.GameOasis.Contracts.ProtocolP
KifuwarabeGo2026.GameOasis.Contracts.ProtocolM
KifuwarabeGo2026.GameOasis.Contracts.ProtocolG
KifuwarabeGo2026.GameOasis.Contracts.ProtocolS
```

v4.0.0では、プレイスペースを接続するProtocol S、GUIを接続するProtocol G、プレイヤーを接続するProtocol Pの最小契約を実装済みです。Protocol Mは利用シナリオを整理してから追加します。

## 互換性

`ContractVersion`は製品バージョンとは独立した契約バージョンです。外部実装との互換性判定には契約バージョンを使用します。

ゲーム固有データは`ContractDocument`で受け渡します。Contractsは囲碁の石、座標、盤などの参照実装固有型を所有しません。
