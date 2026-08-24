# Kifuwarabe Go 2026 v4.0.1

コンシェルジュからランチャーを更新し、普段使うWindowsショートカットを新しい配置へ案内できる試験リリースです。

## ランチャー更新

- コンシェルジュの設定画面に「ランチャー更新」を追加しました。
- GitHub Releasesから最新Launcher ZIPとSHA-256を取得し、検証してから `%LOCALAPPDATA%\KifuwarabeGo2026\Launcher\Current` へ配置します。
- 更新前のランチャーは `Previous` に保持し、切替失敗時は旧版へ戻します。
- ランチャー起動中の上書きは拒否し、終了後の再試行を案内します。

## Windowsショートカット移行

- 通常のWindows `.lnk` を最大5件登録できます。
- 一覧の上から一件ずつ「更新しますか？」と確認し、「はい」「いいえ」を選べます。
- 更新結果は一覧へ逐次反映されます。
- 登録後にリンク先が変更された項目は自動更新せず、利用者へ通知します。
- 引数、説明、ホットキー、ウィンドウ状態を保持し、旧ランチャーに追従していた作業フォルダーとアイコンを新配置へ更新します。

## プラットフォーム境界

- コンシェルジュ本体はOS非依存の `ILauncherMaintenanceService` のみ参照します。
- `.lnk`、Windows Script Host、`LocalApplicationData`、WinForms画面はWindows実装へ隔離しました。
- 将来のmacOS／Linux対応では、同じサービス境界へ各OS用アダプターを追加できます。

## 互換性と配布物

- v4.x.x移行期間として、v3.x.xランチャーから利用するGUI／Engineの資産名と旧GUI公開名を維持します。
- 正式配布: Windows x64
- 必要環境: .NET 8 Desktop Runtime
- `KifuwarabeGo2026.Launcher-v4.0.1-win-x64.zip`
- `KifuwarabeGo2026.Gui-v4.0.1-win-x64.zip`
- `KifuwarabeGo2026.GameOasis.Gui-v4.0.1-win-x64.zip`（旧公開名互換）
- `KifuwarabeGo2026.Engine-v4.0.1-win-x64.zip`

## SHA-256

- Launcher版: `59A5CCBCB149F29582C46F78BDBED15B6FD64C3F949F91DABEFB5DE9880DEB03`
- GUI版: `C837CC29EAEF65E8D276842C3DD493EF2C1947D5ACEC982E46AB5BD7524DCFBA`
- 旧公開名互換GUI版: `C837CC29EAEF65E8D276842C3DD493EF2C1947D5ACEC982E46AB5BD7524DCFBA`
- Engine版: `E63A650C0BA932ACBEE337F346086224C84DB831BF4435CC6D78DE75CE2794C6`
