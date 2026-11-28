# 外部プログラム向け：次局の思考エンジン切替

配布ルート：D:\Projects\KifuwarabeGoPolypropylene
実行入口：KifuwarabeGo2026.Tools.GtpEngineSwitch.exe
機械可読の案内：engine-control.json（schemaVersion=1）。記載する相対パスはこのルート基準。

## 何系が主系か

engine-switch.json の primary（整数1/2/3）が、要求ファイルがなければ次回起動に使う主系。
engines配列の0番目が1系、1番目が2系、2番目が3系。各pathはルート基準の相対パス、または絶対パス。
現在の設定例：

```json
{"primary":1,"engines":[{"path":"1系/KifuwarabeGoPolypropylene.ThinkEngine.exe"},{"path":"2系/KifuwarabeGoPolypropylene.ThinkEngine.exe"},{"path":"3系/KifuwarabeGoPolypropylene.ThinkEngine.exe"}]}
```

primaryは実行中の系統とは限らない。実際の起動記録はengine-status/<wrapperPid>.jsonを読む。
state=running、runningPrimary、wrapperPid、enginePid、executable、updatedUtcを確認できる。
通常終了時はstate=stoppedとexitCodeを記録する。異常終了・強制終了時には古いrunning記録が残り得るので、
外部プログラムはPIDが実在し、該当の実行ファイルのプロセスかも確認する。runningは健全性監視の保証ではない。
複数のラッパーが動く場合は各PIDの記録があり、「今何系か」が複数になる場合もある。

## 次局への切替要求

request-primary.jsonを次の内容で配置する。request-idは任意だが、受付結果を照合するため一意の値を推奨。

```json
{"request-primary":2,"request-id":"update-001"}
```

推奨する書き方：

```powershell
.\Request-Primary.ps1 -Primary 2
```

このスクリプトは対象系統の４ファイルを確認し、一時ファイル完成後にrequest-primary.jsonへ置き換える。
外部プログラムもUTF8で同じディレクトリーに一時ファイルを書き、最後に原子的に置き換える。
直接上書き中の不完全JSONは拒否され得る。まだ未処理の要求を置き換えた場合は、最後の要求が次回対象になる。

ラッパーは次回起動時だけ要求を読み取る。対局中の定期ポーリング・エンジン交換はしない。
CGOS側は各局のsetupで新しいエンジンを起動し、gameoverで終了するため、TCP接続を維持した次局切替ができる。
要求が起動時の読取に間に合わなければ、その次の起動に適用する。要求ファイルを置くだけでは新しい対局は開始しない。

1. 要求ファイルをswitch-requestsへ移動して確保する（後から届いた新しい要求を誤って消さない）。
2. 1/2/3の範囲、対象EXE/DLL/deps.json/runtimeconfig.jsonの存在、子プロセス作成を検査する。
3. 成功したらengine-switch.jsonのprimaryだけを原子的に更新し、新しい主系を使う。未知の設定項目も保持する。
4. 不正要求・ファイル不足・起動失敗なら要求をrejectedとして記録し、従来のprimaryで起動する。

acceptedは設定採用とプロセス作成の成功を示す。GTP応答・棋力・以後のクラッシュまで保証するものではない。
元の主系自体が起動できない場合はエラー終了する。設定ファイルが破損・書込不可の場合も復旧が必要になる。
要求の受渡しと設定更新は短時間の排他ロックで保護する。engine-switch.lockの存在自体は動作中の意味ではない。
ロックファイルを削除して対処せず、OSの排他状態を確認する。

## 受付結果

request-primary-result.jsonのrequestIdと要求のrequest-idを照合する。
status=accepted/rejected、requestedPrimary、selectedPrimary、processedUtc、error、archivedRequestを確認できる。
直近結果は次の要求で更新される。全件の要求と結果はswitch-requests配下に保存する。
要求ファイルが消えただけで成功と判定せず、requestId付き受付結果と起動状態を確認する。
状態ファイルを書けない場合はstderrへ記録するため、状態が読めないときは成功と推定しない。

## 新版を配布する順番

1系が動作中なら2系/3系の未使用を状態とプロセスで確認する。
未使用の3系へ、検証済みReleaseビルドを４ファイル一式で配布する。

```powershell
.\Copy-Release.ps1 -Slot 3
.\Request-Primary.ps1 -Primary 3
```

コピー元を変更する場合はCopy-Release.ps1へ-SourceDirectoryを指定する。
配布を終えてから要求する。コピー途中の系統を要求しない。使用中の系統へ上書きしない。
Copy-Releaseはprimaryを変更せず、要求スクリプトも実行中のエンジンを停止しない。
研究の定期タスクはこの配布や要求を自動実行しない。

2026-11-25実装。隔離統合試験：対局中の要求保留・次局採用・GTP応答・設定項目保持・不正JSON・番号範囲外・
ファイル不足・起動不能からの旧系統継続・終了記録・3系選択の21検査成功。実際のCGOSサーバーへの接続試験は未実施。

配置時点：primary=1、1系/2系=0.3.1、3系=0.4.0（7416自己テスト成功）。本番切替要求は未発行。


## 2026-11-28：エンジンオプション［ｎ系］（配置前）

系統切替ラッパーを実行入口にしたエンジンプロファイルには、先頭に［ｎ系］のコンボ選択を表示する。
選択肢は［要求ファイルに従う］［１系］［２系］［３系］。既定は［要求ファイルに従う］。
GUI保存キーはEngineSlot、値は選択肢の日本語文字列。エントリー個別の上書きにも対応する。

- 要求ファイルに従う：従来どおりrequest-primary.jsonを対局プロセス起動時に処理する。要求がなければprimaryを使う。
- １系～３系：そのラッパープロセスだけ指定系統を使う。共通primaryを変更せず、未処理要求を消費しない。
- 固定指定先が起動不能ならエラーとし、別系統へ暗黙に戻さない。
- 固定指定を解除すると、残していた要求ファイルが次回対局で処理され得る。
- 起動済みの対局には変更を適用しない。対局開始後のEngineSlot設定は拒否する。

kfw-optionsでcomboを公開し、kfw-get-option EngineSlotとkfw-set-option EngineSlot <選択肢>に対応する。

子エンジンが `externallyManaged: true` を返した設定は、実効値と外部管理フラグをGUIへそのまま伝える。対局開始時に子を起動し直す場合は実際に選ばれた子の定義を確認し、外部管理の項目には、仮起動で受け付けた値を再送しない。ラッパーが各系統の研究設定ファイルを読み書きする必要はない。
能力問い合わせ用の仮プロセスは要求を消費しない。最初の盤設定・対局操作で仮プロセスを終了し、選択系統を確定して起動する。
仮起動もengine-statusに記録される。状態記録とOSのプロセスを併せて確認する。
思考エンジンの四ファイルと大会用1系は変更しない。

検証：オフラインReleaseビルド成功。GUI対象限定の四択・エントリー保存・ローカル/CGOS設定引渡し・既定値分離のテスト成功。
隔離ラッパーで固定1/2/3、要求維持、primary維持、自動要求3採用、不正値拒否、対局中変更拒否、実エンジンGTP応答を確認。
実GUI画面での操作は未確認。配置は自動承認レビューにより拒否されたため、実運用先のラッパー/GUIは未更新。
