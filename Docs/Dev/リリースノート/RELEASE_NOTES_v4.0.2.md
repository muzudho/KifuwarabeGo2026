# Kifuwarabe Go 2026 v4.0.2

v4.0.1のランチャー更新導線を、目的が分かる短い操作へ改善したパッチリリースです。

> [!IMPORTANT]
> 通常利用者は `KifuwarabeGo2026.Launcher-v4.0.2-win-x64.zip` をダウンロードしてください。

## 操作導線

- タイトル画面に［ランチャーを開く］と［ランチャーを更新］を別々に配置しました。
- ［ランチャーを開く］は従来どおり共通ランチャーを起動し、コンシェルジュを終了します。
- ［ランチャーを更新］は「ランチャーを最新版にします」と説明してから実行確認します。
- 更新成功後に「ショートカットをデスクトップに作りますか？」と確認します。
- 更新を選ばなかった場合や更新できなかった場合は、更新していないこととGitHubの確認先を表示します。

## デスクトップショートカット

- 作成先はWindowsのデスクトップです。
- 名前は `Kifuwarabe Go 2026 Launcher.lnk` です。
- リンク先、作業フォルダー、アイコンを所定フォルダーの最新ランチャーへ設定します。
- 作成したショートカットを読み直してリンク先を検証します。

## 互換性と配布物

- v4.x.x移行期間として、v3.x.xランチャーから利用するGUI／Engineの資産名と旧GUI公開名を維持します。
- 正式配布: Windows x64
- 必要環境: .NET 8 Desktop Runtime
- `KifuwarabeGo2026.Launcher-v4.0.2-win-x64.zip`
- `KifuwarabeGo2026.Gui-v4.0.2-win-x64.zip`
- `KifuwarabeGo2026.GameOasis.Gui-v4.0.2-win-x64.zip`（旧公開名互換）
- `KifuwarabeGo2026.Engine-v4.0.2-win-x64.zip`

## SHA-256

- Launcher版: `04535C8E326B3B0CD28E35E2B69621CEAC886370D41C0F7942A09049155D6004`
- GUI版: `0985AD399A695CD3036B4386CB3ACBE808A7FEC0BC0CE50E9C93A282C6352231`
- 旧公開名互換GUI版: `0985AD399A695CD3036B4386CB3ACBE808A7FEC0BC0CE50E9C93A282C6352231`
- Engine版: `3D0418F760225C72724E1784234E6D0C14F32EDE78DE71B4D2EAFF38FF9B1C47`
