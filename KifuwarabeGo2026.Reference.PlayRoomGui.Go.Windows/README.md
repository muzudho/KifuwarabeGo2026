# KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows

囲碁Play Room専用Windows Hostの実行入口です。

`--launch-request <json-file>`で保存済みLocal Match起動要求を読み、Lobby非依存の`GoPlayRoomLaunchPlan`へ変換して初期盤面を表示します。現在の画面ループは盤、罫線、初期配置石だけを表示する第2縦切りです。対局操作、プレイヤー接続、Lobbyからの子プロセス起動と終了監視は後続段階で接続します。

Escキーまたはウィンドウの閉じる操作で正常終了します。不正な引数は終了コード2、ファイルまたはJSONの読込失敗は3、契約上の拒否は4を返します。
