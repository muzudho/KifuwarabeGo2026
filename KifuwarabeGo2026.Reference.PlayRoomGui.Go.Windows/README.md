# KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows

囲碁Play Room専用Windows Hostの実行入口です。

外部の進行役から利用する`--stdio`モードを追加しました。[公開する標準入出力の能力・文書形式・操作手順](STDIO.md)を参照してください。局面更新、画面からの着手・パス・投了、終了通知を扱います。以下のファイル起動モードとは責務が異なります。

公式Windows Lobbyの通常囲碁Matchは、[公式進行役](../KifuwarabeGo2026.Reference.MatchRunner.Go/README.md)から`--stdio`へ接続します。対局判定とGTP接続は進行役が所有し、終局結果はLobbyへ返します。

従来の`--launch-request <json-file>`も利用できます。保存済みLocal Match起動要求をLobby非依存の`GoPlayRoomLaunchPlan`へ変換し、Host内の最小対局状態とGTP接続を使って着手・パスを処理します。この旧経路と、Protocol G/Sを使う新しい公式Lobby経路は別です。

Escキーまたはウィンドウの閉じる操作で正常終了します。不正な引数は終了コード2、ファイルまたはJSONの読込失敗は3、契約上の拒否は4を返します。
