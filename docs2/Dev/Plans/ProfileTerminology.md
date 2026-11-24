# プロフィールの将来の用語整理

保存JSONの移行を伴う設計案です。採用・実装済みとは扱いません。[Playerの状況](../CurrentState/PlayerProfiles.md)。

## 将来の用語整理案（本計画の完了後）

現在の `EntryProfile` / `ClientIdentityProfile` / `GtpEngineProfile` は、互換性を保ちながら本計画を完了するための名称として維持する。本計画の完了後、責務を次のように再編・改名する。

| 将来の名前 | 責務 | 現在の主な対応物 |
| --- | --- | --- |
| `EntryProfile` | 対局に参加する一席。`HumanProfile` または `EngineProfile` のどちらかを参照する。自分同士の対局では二つの Entry を作れる。 | `EntryProfile` |
| `HumanProfile` | 人間一人と一対一のプロフィール。紹介・表示など、人そのものの情報を持つ。 | `EntryProfile` の Human 部分 |
| `EngineProfile` | 思考エンジン一つの起動方法。実行ファイル、作業ディレクトリ、引数、GUI/GTP 設定を持つ。 | `GtpEngineProfile` |
| `ServiceProfile` | CGOS、野良対局、大会各日、LocalMatch など、接続先サービス一つの接続情報。 | `CgosConnectionProfile` と LocalMatch の種別 |
| `ClientIdentity` | 一つの `EntryProfile` が一つの `ServiceProfile` で名乗る身元。 | `ClientIdentityProfile` |

`ClientIdentity` は次の項目を中心にする。`Handle` は、機械に入力できる書式に従った Player の Entry 名である。プロトコルに応じて CGOS ではログイン名、LocalMatch では出力ファイル名として使う。人に見せる `DisplayName` とは別の値であり、UI では原則 `HANDLE` と表示する。

```text
ClientIdentity
  EntryProfileId
  ServiceProfileId       // LocalMatch 等、接続先を持たない形はサービス種別で表す
  Handle
  SecretReference        // OS の安全な資格情報ストアを参照する。秘密そのものは JSON に保存しない。
```

この改名は保存 JSON の移行を伴うため、Player/Client Identity を利用する現在の UI・CGOS 接続・LocalMatch が一通り安定した後に、一回の移行として実施する。

## 元の検討経緯

[分割前の計画・調査記録](../Archive/PlayerIntegration.md)
