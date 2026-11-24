# FormalAdapter移行の到達点

2026-08-30の記録では段階0〜6・8・9は完了。段階7は実装済みでWindows最終回帰の判定が保留です。GTPプリミティブ・クライアント・オプション、CGOSの型付き通信とGUI通知、SGF文書・Go変換を移行済み。USI・CSA・KIFは対象外です。

[残る検証](../Plans/FormalAdapterValidation.md) / [SGF文書モデル](SgfDocument.md)

### 作業段階8：GTP Protocol Pアダプターと参照サーバーを分離する

状態：完了（2026年8月30日）

旧`Reference.Communication.Gtp`に同居していた双方向の責務を分離しました。外部GTPエンジンをProtocol Pの`IPlayerProtocol`として利用する`KifuwarabeGtpPlayerProtocol`とloadsgf用一時SGFファイル処理は`KifuwarabeGo2026.FormalAdapter.Gtp.PlayerEngine`へ移しました。

公式きふわらべプレイヤーをGTPサーバーとして公開する`GtpEngine`、原子的局面設定、ポン抜きProvider、独自オプション処理は`KifuwarabeGo2026.Reference.PlayerEngine.Go.Gtp`へ移しました。標準入出力へ接続する薄い構成点は`KifuwarabeGo2026.Reference.PlayerEngine.Go.Gtp.Host`へ改名し、配布互換名`KifuwarabeGo2026.Engine`は維持します。

試験は`KifuwarabeGo2026.Tests.FormalAdapter.Gtp.PlayerEngine`へ改名しました。旧`Reference.Communication.Gtp`と`.Host`は廃止し、以降の文書で旧名称が現れる箇所は移行前の履歴を表します。

### 作業段階9：移行後のGTP境界を回帰検査で固定する

状態：完了（2026年8月30日）

第8段階で分離した責務が旧配置へ戻らないよう、GUI移植性試験へ構造検査を追加しました。`FormalAdapter.Gtp.PlayerEngine`が外部GTPエンジン向けProtocol Pアダプターを所有すること、`Reference.PlayerEngine.Go.Gtp`が参照GTPサーバーを所有することをアセンブリ単位で検査します。

さらに、アダプターが参照サーバーへ依存せず、参照サーバーもアダプターへ依存しないこと、旧`Reference.Communication.Gtp`のプロジェクトファイルとソースが復活していないこと、ソリューションが新しいアダプター・サーバー・Host名だけを含むことを検査します。旧フォルダー内に残る`bin`・`obj`生成物は実装ではないため検査対象外です。

専用プロジェクトのReleaseビルドは警告0件、エラー0件です。GUI移植性およびGTP・CGOS・SGF基準ベクトルを含む試験は`PASS`しました。

#### 2026年8月30日のWindows最終回帰再試行

全ソリューションをReleaseで再ビルドし、警告0件、エラー0件を確認しました。GUI移植性およびGTP・CGOS・SGF基準ベクトル、PlayRoom GUIのBoard Editor・Review・Match正常／異常終了、PlayRoom Engineの複数／単一セッション・ライフサイクル・異常終了、Protocol S適合性3ケースはすべて`PASS`しました。

Windows非対話試験だけは、再生成された`KifuwarabeGo2026.Tests.GameOasis.Gui.Windows.exe`がアプリケーション制御ポリシーによりプロセス起動時に遮断され、試験コードへ到達しませんでした。コード回帰を示す失敗ではなく、既知のSmart App Control環境制約が再現した結果です。セキュリティ設定は変更せず、第7段階の最終完了判定を引き続き対応方針決定待ちとします。

## 元の検討経緯

[分割前の計画・調査記録](../Archive/FormalAdapterMigration.md)
