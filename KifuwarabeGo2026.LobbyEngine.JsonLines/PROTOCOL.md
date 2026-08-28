# Lobby Engine JSON Lines Protocol v1

標準入力と標準出力は UTF-8 の JSON Lines とし、1行を1メッセージとして扱います。標準出力には応答以外を書かず、診断情報は標準エラーへ出力します。

要求：

```json
{"protocolVersion":1,"requestId":"unique-id","method":"listEntries","parameters":null}
```

成功応答：

```json
{"protocolVersion":1,"requestId":"unique-id","success":true,"result":{"entries":[]},"error":null}
```

失敗応答：

```json
{"protocolVersion":1,"requestId":"unique-id","success":false,"result":null,"error":{"code":"invalid-request","message":"..."}}
```

v1 の操作は読み取り専用の `listEntries` だけです。プロファイル保存などの変更操作は、再実行安全性を別途設計するまで同一プロセスの `ILobbyEngine` を使用します。

クライアントは応答版、要求ID、成功状態、結果の有無を検査します。タイムアウト、起動失敗、子プロセス終了、不正JSON、不一致応答、エラー応答では、GUIを終了せず同一プロセス実装の読取結果へ復旧します。
