# 公式Lobbyから公開通信で囲碁対局を進める

2026-09-09：Windowsの公式Lobbyの通常囲碁Match起動を`GoStdioMatchLauncher`へ切り替えました。

## 接続と責務

公式Lobby → `Reference.MatchRunner.Go` → `PlayRoomGui.JsonLines` → 公式囲碁ウィンドウの`--stdio`を使用します。進行役はProtocol GでConciergeを操作し、既存の囲碁Protocol S Engineが合法手・取り・手番・投了・連続パスの面積採点を確定します。画面は確定局面を表示し、クリック・P・Rを入力イベントとして返します。

GTP接続処理は新しい進行役プロジェクトへ移し、従来のファイル起動Hostとも共用します。画面へ渡す起動要求からエンジン接続とオプションを除き、初期局面はconfigurationのsetupStonesを使用します。Lobbyの保存先や内部セッションを画面から読みません。従来のSGF添付を履歴再生に使う経路ではありません。

終局時は画面へcompleteを送り、勝者と終局理由をLobbyへ返して結果ダイアログを表示します。画面を閉じた場合、通信異常、キャンセルでも進行セッションと子プロセスを回収します。Lobby終了時も実行中の対局をキャンセルします。

## 検証

- ソリューションReleaseビルド：警告0・エラー0。
- 公式Lobbyと囲碁Hostを`artifacts/stdio-match-lobby`へ発行し、進行役・通信DLLの同梱を確認。同配置のHostでも対局スモークはPASS。Releaseスクリプトへ新DLLの必須検査と発行Hostへの対局スモークを追加。
- 独立プロセスのテスト用プレイルーム経由で、着手・取り・不正着手拒否・投了・連続パス採点・古い入力・手番違い・閉じる通知・応答ID不一致・異常終了・終了しないホスト・GTP対局・キャンセル・異常後の再起動を検証。
- 公式Hostのpublish出力に対し、公式進行役と2つのテストGTPプレイヤーで連続パスから採点・完了応答・正常終了までPASS。画面なしモードと実ウィンドウの両方で確認。
- LobbyEngine、Contracts、Concierge、PlayRoomGui JSON Lines、GUI移植性、Windows非対話試験はPASS。前段階でOSポリシーに拒否された試験も、今回は完走しています。
- PlayRoomEngine JSON LinesとProtocol S適合性3件（Go・Ponnuki・外部Counter）もPASS。

## 残る確認と範囲

実ウィンドウ試験は自動でパスするGTP同士の対局です。公式Lobbyのボタンを実際に押す開始操作、実マウスによる着手、P/R/Esc、閉じるボタン、Lobby結果ダイアログの見た目は手動確認が残ります。

最小表示契約には時計・アゲハマ数・最終着手・結果装飾がありません。時間切れの自動裁定は今回追加していません。既存Protocol Sの残り時間観測を使います。編集・レビュー・ポン抜き・CGOSの移行、SDK全体の配布物だけでの再現確認も別途残ります。

[進行役と再現コマンド](../../../KifuwarabeGo2026.Reference.MatchRunner.Go/README.md) / [全体計画](../Plans/ExternalPlayRoomIntegration.md)
