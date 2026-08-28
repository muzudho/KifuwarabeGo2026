# Board Editor Play Room JSON Lines Protocol v1

標準入力と標準出力はUTF-8のJSON Linesです。標準出力はプロトコル応答専用、標準エラーは診断専用です。

ライフサイクル：

```text
open(PlayRoomLaunchRequest)
  -> PlayRoomReady
  -> replacePosition(BoardEditorPositionUpdate)  0回以上
  -> adopt | discard | goodbye
  -> BoardEditorCompletion
  -> process exit
```

`open` は version 1、`roomTypeId = board-editor`、公式囲碁game ID、初期局面文書を要求します。ホストはロビーのStorage、GUI内部状態、`GoAppSession`を参照せず、渡された局面文書のコピーだけを所有します。

- `adopt`: 編集後の局面文書を `Adopted` とともに返します。
- `discard`: 局面文書を返さず `Discarded` を返します。
- `goodbye`: 未採用の局面を破棄し `Closed` を返します。

すべての要求と応答はプロトコル版と要求IDを持ちます。セッション開始後の操作は、準備完了応答で発行されたセッションIDが一致しなければ拒否されます。タイムアウト、不正JSON、応答ID不一致、途中終了はクライアント側の失敗となり、呼出側はロビーへ戻れます。

## Review Play Room

```text
open(PlayRoomLaunchRequest: roomTypeId = review)
  -> PlayRoomReady
  -> navigate(ReviewNavigation)  0回以上
  -> usePosition(ReviewPositionSelection) | goodbye
  -> ReviewCompletion
  -> process exit
```

Reviewホストは起動時の棋譜文書を読み取り専用で保持し、`navigate`では表示手数だけを変更します。`usePosition`は表示中の手数と一致する局面文書コピーだけを`PositionSelected`として返します。元棋譜の内容は変更しません。
