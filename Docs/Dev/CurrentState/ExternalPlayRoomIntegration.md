# 外部アプリからのプレイルーム利用の現状

2026-09-09にコードと文書を確認し、外部クライアントを追加して実行試験を行いました。[次の実装計画](../Plans/ExternalPlayRoomIntegration.md)。以下の表は着手時点の棚卸しです。

## 今回の実装・検証

[External.PlayRoomClient](../../../Samples/External.PlayRoomClient/README.md)を追加しました。Python標準ライブラリだけで任意のコマンドを起動し、版・要求ID・セッション開始応答を確認します。stderrを並行して読み、通信失敗時は子プロセスを終了・回収します。

公式Board Editor / Review / Matchホストのpublish出力と独立Pythonホストに対し、7試験メソッドがすべてPASSしました。編集採用・破棄、レビュー選択、対局状態・3種類の意味操作・完了、不正版・セッション不一致、途中終了を検査しています。偽ホストでは無応答、不正JSON、ID不一致、大量stderr、完了応答後に終了しない場合を検査しています。初回実行はopenの5秒タイムアウトで1件失敗し、同じコードの再実行で全件成功しました。起動時間のばらつきは残る制約です。

逆方向の参照例として`board_editor_host.py`を追加し、公式.NETの`BoardEditorProcessSession`から独立Pythonホストへの採用・破棄・日本語文書の往復もPASSしました。呼出元とプレイルームの両側を独立実装できる、画面のない最小境界の実証です。完成版LobbyやGUI全体の交換を証明するものではありません。

ソリューション全体のReleaseビルドは警告0・エラー0で成功しました。画面付きホストへの標準入出力接続、外部作者による画面付き実装の交換、GUI手動操作は今回の検証に含みません。

既存のLobbyEngine、Contracts、Concierge、PlayRoomGui JSON Lines、PlayRoomEngine JSON Lines、GUI移植性（新しい描画依存・座標検査を含む）、Windows非対話試験もPASSしました。Protocol S適合性は公式Go・Ponnuki・外部Counterの3件PASSです。Windows GUIのpublish出力に新しい描画DLLとContracts DLLが含まれることを確認しました。旧名`Tests.PlayRoom.JsonLines`はbin/objのみ残るディレクトリーで実行プロジェクトがないため、現行の`Tests.PlayRoomGui.JsonLines`の結果を使用しています。

| 経路 | コード上で確認した内容 | この確認では証明しないこと |
|---|---|---|
| PlayRoomEngine JSON Lines / Protocol S | ルールEngine用SDK、公式ホスト、外部Counterサンプルが存在 | 外部アプリから画面付きプレイルームを操作できること |
| PlayRoomGui JSON Lines | Board Editor、Review、Matchの要求・応答と終了契約が存在 | これらの要求が実際の囲碁ウィンドウへ接続されていること |
| 囲碁Windowsホスト | `--launch-request`でファイルを読み、stdoutへreadyを返して`GoInitialBoardGame`を起動 | stdinで起動後の操作を受け、画面入力を外部へ通知する一連の通信 |
| 外部利用例 | `Samples`には外部EngineのCounterが存在 | 公式プレイルームを呼び出す独立クライアントの実例 |

`Reference.PlayRoomGui.Match.JsonLinesHost/Program.cs`は要求応答のループを持ちます。`submitAction`は受理結果を返しますが、画面入力を外部へ自発的に配信する処理はこのループにはありません。

`Reference.PlayRoomGui.Go.Windows/Program.cs`は起動要求の読込みとready出力の後、画面ループへ入ります。起動後に標準入力を読み続けるループはこの入口にはありません。専用WindowsホストのREADMEには古い段階の説明が残るため、到達点はコードと[段階別記録](LobbyPlayRoomSeparation.md)を併せて確認する必要があります。

したがって、過去のDLL分離・子プロセス起動の完了を、外部アプリからの画面付きプレイルーム利用の完了とは扱いません。今回確認した範囲では、通信契約の外部利用例と、画面ループへの接続が次の確認対象です。
