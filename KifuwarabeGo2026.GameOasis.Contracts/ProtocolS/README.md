# Protocol S v1.0

Protocol Sは、ゲームコンシェルジュと独立したプレイスペースエンジンを接続します。

## ライフサイクル

```text
Describe
GetConfigurationSchema
ValidateConfiguration
CreateSession
    ├─ GetSnapshot
    ├─ ApplyAction
    └─ CloseSession
```

一つの`IPlaySpaceProtocol`実装は、複数のセッションを扱えます。セッションIDはプレイスペース実装が発行し、コンシェルジュは不透明な識別子として扱います。

## ゲーム固有文書

ゲーム設定、状態、行動、イベント、勝敗は`ContractDocument`で受け渡します。

```text
MediaType  application/jsonなど
SchemaId   文書の意味と版を示す安定ID
Content    文書本体
```

これにより、Contractsは囲碁、ポン抜き、チェス、クイズなどの固有型を参照しません。各プレイスペースは設定スキーマを公開し、自分が受理する文書を検証します。

## リビジョン

`PlaySpaceSnapshot.Revision`はセッション内で単調増加する状態版です。コンシェルジュは`ApplyPlaySpaceActionRequest.ExpectedRevision`へ、行動の前提にしたリビジョンを指定します。

現在リビジョンと一致しない要求は、古い状態を前提にした競合として拒否します。具体的なエラーコードは参照実装を二つ接続してから共通化します。

## 二種類の失敗

境界自体の失敗と、ゲーム上の行動拒否を分けます。

- `ProtocolResponse<T>.Failure`: セッション不存在、文書形式不正、互換バージョン不一致、通信障害など
- `PlaySpaceActionApplied.IsAccepted == false`: 禁止された着手、手番違いなど、要求を解釈できた上でのゲーム上の拒否

ゲーム上の拒否でも、応答には現在のスナップショットを含めます。

## 実装上の制約

- Protocol S実装はGUIまたはMonoGameへ依存しません。
- 状態変更は`ApplyActionAsync`を経由します。
- `GetSnapshotAsync`は状態を変更しません。
- キャンセルされた操作を成功として記録しません。
- 設定検証に成功していない入力を暗黙に補正する場合は、補正後の設定を明示できる拡張を別途設計します。
- 乱数を使用するプレイスペースは、再現に必要なシードまたは生成情報を状態かイベントへ残します。

## v1.0で未確定の事項

- 共通エラーコード一覧
- 能力ID一覧
- セッション破棄の冪等性
- バイナリ文書の運搬方法
- イベントの通し番号と再取得API
- スナップショットの差分配信

これらは通常囲碁とポン抜きの参照実装を作り、両方に必要だと確認してから契約へ追加します。
