# PlaySpace JSON Lines Protocol v1

標準入力と標準出力はUTF-8のJSON Linesです。標準出力には応答だけを出し、診断は標準エラーへ出します。要求と応答はプロトコル版と要求IDを持ちます。

## Protocol S対応表

```text
describe                 -> ProtocolResponse<PlaySpaceDescriptor>
getConfigurationSchema   -> ProtocolResponse<ContractDocument>
validateConfiguration    -> ProtocolResponse<PlaySpaceConfigurationValidation>
createSession            -> ProtocolResponse<PlaySpaceSessionCreated>
getSnapshot              -> ProtocolResponse<PlaySpaceSnapshot>
applyAction              -> ProtocolResponse<PlaySpaceActionApplied>
closeSession             -> ProtocolResponse<PlaySpaceSessionClosed>
goodbye                  -> process exit
```

要求の`parameters`は既存Protocol Sの要求型をそのままJSON化します。応答の`result`は`value`または`error`を持ち、Protocol Sの成功・失敗を保存します。通信エンベロープの失敗は、不正JSON、未対応メソッド、プロトコル版不一致など、Protocol Sを呼び出せなかった場合だけに使います。

## ホストマニフェスト

`*.playspace.json`はPlaySpace種別ID、起動コマンド、引数、複数セッション対応を記述します。呼出側は具象PlaySpaceアセンブリを参照せず、マニフェストと`PlaySpace.JsonLines`だけでプロセスを起動できます。

参照ホストは`--play-space go`で通常囲碁、`--play-space ponnuki`でポン抜きを選びます。`--single-session`を指定したホストは、活動中セッションがある間、二つ目の生成を`single-session-busy`で拒否します。指定しないホストは同じプロセスで複数セッションを扱います。

タイムアウト、不正応答、要求ID不一致、子プロセス終了はクライアント側の通信障害になります。Conciergeはこの障害をセッション失敗または再接続待ちへ変換し、Lobbyへ通知する責務を持ちます。今回の参照クライアントは障害を検出可能な例外として公開し、Lobby通知とログ永続化は後続のConcierge Host接続段階へ残します。
