# Player・Client Identityの実装状況

確認記録: 2026-08-12。後日の実装全体を再検証した表ではありません。古い表と同日の追記に相違があるため、ここでは最新追記の完了項目と操作補足を入口にします。[後続課題](../Plans/PlayerProfiles.md)。

### 完了

- Human / Computer の Player 統合選択、GTP Engine の選択、Local Match / Ponnuki、SGF 表示名出力。
- ClientIdentityProfile / ClientIdentityCatalog、Player からの Client Identity 参照、CGOS Connection Profile の不変 ID 参照。
- CGOS 実行時の Client Identity 認証情報参照。
- CLIENT IDENTITIES ダイアログの表示、最大5件までの OnlineMatch (CGOS) / LocalMatch Client Identity 追加、削除、行選択、接続先選択。
- Client Identity の `DISPLAY`、`LOGIN NAME`、`LOGIN PASS` をクリック、貼り付け、IME、Enter/Escape、Tab で編集・保存できる。非編集中のパスワードは伏せ字にする。
- LocalMatch Client Identity では `LOGIN NAME` を `OUTPUT NAME` として表示し、パスワード欄を表示しない。
- `USE` により選択 Client Identity を Player の既定使用先にできる。Player 編集画面では既定 Client Identity を表示し、`SELECT CLIENT IDENTITY` / `EDIT CLIENT IDENTITIES` から選択・編集できる。

### 操作上の補足（2026-08-12）

- LocalMatch / OnlineMatch (CGOS) ともに、Entry Profile を選ぶと、そのサービス向けの既定 Client Identity を採用する。
- 画面には Client Identity の `HANDLE` を表示する。`HANDLE` をクリックすると、選択中 Player が持つ当該サービス用 Client Identity だけの一覧を開き、今回の対局・接続に限って切り替えられる。
- この一時切替は Player 内の Client Identity 順序を変更しない。恒久的な既定変更には Client Identity 編集画面の `USE` を使う。
- `HANDLE` は「機械に入力できる書式に従った、Player の Entry 名」。CGOS ではログイン名、LocalMatch では棋譜ファイル名に使う。

## 同日の追加完了項目

- [完了] Local Match の開始時に、黒白それぞれの既定 LocalMatch Client Identity の `NAMELY KEY`（旧 OUTPUT NAME）を固定し、手動保存・自動保存する棋譜ファイル名へ反映する。空欄または旧データでは Player の Identifier、表示名の順に補う。
- [完了] OnlineMatch (CGOS) で同じ接続先に複数 Client Identity がある場合、Player 内の並び順で最初の Client Identity を既定として使う。接続・対局開始画面には、その Client Identity の Display と HANDLE を `DEFAULT CLIENT IDENTITY` として表示する。
- [完了] OnlineMatch (CGOS) 接続先の選択 UI を、Client Identity 編集の現在値・前後切替から、接続先一覧を選ぶ一貫した UI へ整理する。
- [完了] Entry Catalog と Client Identity Catalog をまたぐ追加・削除・既定変更の保存を、ひとまとまりの操作として整理する。Client Identity を先に保存し、その後 Player の Client Identity 参照を保存する。

## 元の検討経緯

[分割前の計画・調査記録](../Archive/PlayerIntegration.md)
