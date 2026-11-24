# Kifuwarabe Go 2026 v4.0.7

GTP・CGOS・SGFの外部仕様境界と参照実装の配置を整理し、ランチャーのインストール済みバージョン一覧を使いやすくしたリリースです。

> [!IMPORTANT]
> 通常利用者は `KifuwarabeGo2026.Launcher-v4.0.7-win-x64.zip` をダウンロードしてください。

## Launcher

- インストール済みバージョンを8件単位で確認できるページャーを追加しました。
- `PREVIOUS`／`NEXT`ボタン、ページ番号、表示範囲を表示します。
- 左右キー、PageUp／PageDown、ゲームパッド左右でもページを移動できます。
- 画面下の`OPEN FOLDER`／`UNINSTALL`ボタンとメッセージ帯の重なりを解消しました。

## FormalAdapterと参照実装

- GTP、CGOS、SGFの外部仕様解釈をFormalAdapterへ集約しました。
- 外部GTPエンジン用Protocol Pアダプター、公式きふわらべ参照GTPサーバー、標準入出力Hostを役割別プロジェクトへ分離しました。
- 囲碁の共有ドメイン、PlayRoom GUI、PlayRoom Engine、Player Engineのプロジェクト名と依存関係を整理しました。
- 移行後の所有アセンブリと依存方向が旧配置へ戻らないための回帰検査を追加しました。

## 既知の環境制約

- Smart App Controlが有効な環境では、ローカルで再生成した未署名EXE／DLLが遮断される場合があります。
- 今回の配布物にはコード署名を行っていません。

## 互換性と配布物

- v4.x.x移行期間として、v3.x.xランチャー向けの資産名と旧GUI公開名を維持します。
- `KifuwarabeGo2026.Launcher-v4.0.7-win-x64.zip`
- `KifuwarabeGo2026.Gui-v4.0.7-win-x64.zip`
- `KifuwarabeGo2026.GameOasis.Gui-v4.0.7-win-x64.zip`（旧公開名互換）
- `KifuwarabeGo2026.Engine-v4.0.7-win-x64.zip`

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime（配布物はframework-dependentです）

## SHA-256

- Launcher版: `88C4EA8DD3A2A1865511BBA0AB0DC741ECCE07AEC2D327F55849AC5A39792868`
- GUI版: `E4A9FE73CD07DAE32354A0D0A47FE3C54DF72516FD3C4915DE55BD79E3B8236B`
- 旧公開名互換GUI版: `35F6225D0DF9C508C65C8E9B470ED210F7C701D21684027476E63D76457DEC56`
- Engine版: `92F8F2A3167E2FA3A5B37BD48323504D99F51D2C7F0D611327A2F41144230B7B`
