# PlaySpace外部実装SDK

外部PlaySpaceは、公式GUI、MonoGame、Concierge具象実装、通常囲碁実装を参照せずに作成できます。.NET実装で必要な参照は次の二つです。

* `KifuwarabeGo2026.GameOasis.Contracts`: Protocol Sの要求、応答、自己記述文書。
* `KifuwarabeGo2026.PlayRoomEngine.JsonLines`: 標準入出力ホスト、クライアント、マニフェスト。

## 最小実装手順

1. `IPlaySpaceProtocol`を実装する。
2. 実行ファイルの入口から`PlayRoomEngineJsonLinesHost.RunAsync(protocol)`を呼ぶ。
3. `*.playspace.json`へ安定した種別ID、コマンド、引数、複数セッション能力を書く。
4. ゲーム設定、行動、状態、イベント、結果を、それぞれ異なる`ContractDocument.SchemaId`を持つJSONとして公開する。
5. 適合性ベクトルを作り、CLIランナーへマニフェストと共に渡す。

実装例は[`Samples/External.PlaySpace.Counter`](../../../Samples/External.PlaySpace.Counter/)にあります。この例は公開二プロジェクトだけを参照し、値を増やして目標へ到達する最小ゲームを別プロセスで実行します。

## 適合性ランナー

```powershell
dotnet run --project KifuwarabeGo2026.PlayRoomEngine.Conformance -c Release -- `
  --manifest path\to\component.playspace.json `
  --vector path\to\conformance-vector.json
```

ランナーは偽Conciergeとして、記述、Protocol版、設定スキーマ、正常・不正設定、セッション生成、初期状態、行動、リビジョン競合、終了、終了後の状態消滅を検査します。引数を省略すると、公式通常囲碁、公式ポン抜き、外部風Counterサンプルを同じ方法で検査します。

## 状態遷移

```text
process start
  -> describe / schema / validate (何回でも可)
  -> createSession
       -> getSnapshot / applyAction (何回でも可、revisionは楽観的同時実行制御)
       -> closeSession
  -> goodbye
  -> process exit
```

複数セッション対応ホストは複数の活動中セッションを識別子で分離します。単一セッションホストは活動中の二重生成を`single-session-busy`で拒否し、終了後は再生成できます。

## 言語非依存資産

* [`playspace-host-manifest.schema.json`](../../../Conformance/ProtocolS/v1/playspace-host-manifest.schema.json)
* [`conformance-vector.schema.json`](../../../Conformance/ProtocolS/v1/conformance-vector.schema.json)
* [`go.json`](../../../Conformance/ProtocolS/v1/go.json)
* [`ponnuki.json`](../../../Conformance/ProtocolS/v1/ponnuki.json)
* [`external-counter.json`](../../../Conformance/ProtocolS/v1/external-counter.json)

JSON Linesの詳細は[`PROTOCOL.md`](../../../KifuwarabeGo2026.PlayRoomEngine.JsonLines/PROTOCOL.md)を参照してください。
