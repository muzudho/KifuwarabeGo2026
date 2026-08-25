# Kifuwarabe Go 2026 v4.0.4

ローカル対局の着手音を修正し、画面上の現在地を分かりやすくしたパッチリリースです。

> [!IMPORTANT]
> 通常利用者は `KifuwarabeGo2026.Launcher-v4.0.4-win-x64.zip` をダウンロードしてください。

## GUI

- コンピューター対コンピューターのローカル対局でも、石が盤面へ置かれたときに着手音が鳴るよう修正しました。
- Local Matchの休憩画面から囲碁盤を外し、対局情報と操作を表示するLobby画面として整理しました。
- 画面下部のパンくずリストを`Launcher`、`Lobby`、`Play Room`のいずれかから始まる表示へ整理し、現在地を分かりやすくしました。

## 互換性と配布物

- v4.x.x移行期間として、v3.x.xランチャー向けの資産名と旧GUI公開名を維持します。
- `KifuwarabeGo2026.Launcher-v4.0.4-win-x64.zip`
- `KifuwarabeGo2026.Gui-v4.0.4-win-x64.zip`
- `KifuwarabeGo2026.GameOasis.Gui-v4.0.4-win-x64.zip`（旧公開名互換）
- `KifuwarabeGo2026.Engine-v4.0.4-win-x64.zip`

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime（配布物はframework-dependentです）

## SHA-256

- Launcher版: `601DF37772A55916CE95152926EE51D24BFECBE35705021A87F16A85534DC15E`
- GUI版: `004970349205E6F4F6A7458A51A0DE7CAE140B97058017875390F23E944C8EC2`
- 旧公開名互換GUI版: `004970349205E6F4F6A7458A51A0DE7CAE140B97058017875390F23E944C8EC2`
- Engine版: `841A398B6E2F4375327BFDF50F2E57FD45F81ED508A5E7EA73C095604FBF3EB5`
