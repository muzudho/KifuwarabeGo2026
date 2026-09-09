# External Play Room Client

公式LobbyのDLLを参照せず、別プロセスのプレイルームを呼び出す最小CLIです。Python 3.10以降の標準ライブラリだけを使用します。任意の実行ファイルと引数を指定できるため、他の作者の互換ホストにも接続できます。

現段階の対象は画面のないBoard Editor / Review / Match参照ホストです。画面付き囲碁Windowsホストの `--launch-request` 経路とは異なります。このサンプルはカタログ・設定画面・ゲーム進行を備えた完成版Lobbyではなく、Lobby作者が接続を実装する際の最小例です。

## 起動

リポジトリー直下で対象ホストをビルドします。

```powershell
dotnet build KifuwarabeGo2026.Reference.PlayRoomGui.BoardEditor.JsonLinesHost -c Release
python Samples/External.PlayRoomClient/client.py --room board-editor --trace -- dotnet KifuwarabeGo2026.Reference.PlayRoomGui.BoardEditor.JsonLinesHost/bin/Release/net8.0/KifuwarabeGo2026.Reference.PlayRoomGui.BoardEditor.JsonLinesHost.dll
```

Review / Matchはホスト名と `--room review` / `--room match` を変更します。Board Editorの `--discard` は編集後に破棄する例です。`--trace` は実際の送受信JSONをstderrへ表示します。正常終了は0、通信・ホスト・契約失敗は1です。

配布物で試す場合は各ホストを `dotnet publish -c Release -o <出力先>` で発行し、出力ディレクトリー全体を渡します。DLL単体のコピーではなく、deps.json、runtimeconfig.json、依存DLLも必要です。上のframework-dependent DLL起動には.NET 8以降の対応ランタイムが必要です。

## 接続の約束

- UTF-8 JSON一件を一行でstdinへ送り、stdoutから一行の応答を受けます。ログはstderrです。
- `protocolVersion` は1。外側の `requestId` は要求ごとに変え、応答の同じIDを検査します。`open` 内の起動要求IDとは別です。
- `open` が返す `sessionId` を以後の操作に使います。
- 一度に一要求を送ります。タイムアウト・壊れたJSON・ID不一致では接続を再利用せずプロセスを回収します。
- 正しい失敗応答は `RemoteError` です。無効な操作を拒否しただけなら同じセッションを続けられます。
- 終了操作は応答だけでなく終了コード0まで検査します。stderrは並行して読み、直近の診断だけを保持します。

v1では列挙値は文字列でなく数値です。Board EditorのAdopted/Discarded/Closedは0/1/2、ReviewのPositionSelected/Closedは0/1、MatchのFinished/Closedは0/1、操作PlayPoint/Pass/Resignは0/1/2です。`playSpaceTypeId` は文字列ではなく `{"value":"…"}`、`ContractDocument.content` はJSONやSGFを格納した**文字列**です。

[操作の仕様](../../KifuwarabeGo2026.PlayRoomGui.JsonLines/PROTOCOL.md) / [外部利用の実装計画](../../Docs/Dev/Plans/ExternalPlayRoomIntegration.md)

## 試験

```powershell
python -m unittest discover -s Samples/External.PlayRoomClient -v
```

これはタイムアウト、壊れた応答、ID不一致、途中終了、大量stderr、終了しないホストを検査します。公式ホストとの試験も実行する場合は、3つのホストを同じ発行先へpublishして指定します。

```powershell
dotnet publish KifuwarabeGo2026.Reference.PlayRoomGui.BoardEditor.JsonLinesHost -c Release -o artifacts/playroom-hosts
dotnet publish KifuwarabeGo2026.Reference.PlayRoomGui.Review.JsonLinesHost -c Release -o artifacts/playroom-hosts
dotnet publish KifuwarabeGo2026.Reference.PlayRoomGui.Match.JsonLinesHost -c Release -o artifacts/playroom-hosts
$env:PLAYROOM_HOST_DIRECTORY = (Resolve-Path artifacts/playroom-hosts).Path
python -m unittest discover -s Samples/External.PlayRoomClient -v
```

公式ホスト試験は環境変数未設定なら明示的にskipします。

## 逆方向：外部作者のプレイルームを公式クライアントから利用

`board_editor_host.py`はPythonだけで書いた、画面のない独立Board Editor参照実装です。標準入力の要求を読み、標準出力へ応答します。ゲームルールやSGFの意味解釈は行わず、局面文書のコピーを受け渡します。

```powershell
dotnet run --project KifuwarabeGo2026.Tests.PlayRoomGui.JsonLines -c Release -- --external-board-editor python Samples/External.PlayRoomClient/board_editor_host.py
```

公式.NETクライアントで採用・破棄と日本語文書の往復を検査します。外部呼出元と公式ホストの方向に加え、公式クライアントと外部ホストの方向でも同じ通信境界を使用できる最小例です。完成版ロビー・画面付きプレイルームの交換は後続段階です。
