# KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows

囲碁Play Room専用Windows Hostの実行入口です。

第1縦切りでは画面ループをまだ所有せず、`--launch-request <json-file>`で保存済みLocal Match起動要求を読み、Lobby非依存の`GoPlayRoomLaunchPlan`へ変換できることを検査します。正常時は標準出力へ`ready`結果を出して終了コード0、不正な引数は2、ファイルまたはJSONの読込失敗は3、契約上の拒否は4を返します。
