# Player統合の後続課題

2026-08-12の後続記録です。過去の未着手表示をそのまま着手根拠にせず、[実装状況](../CurrentState/PlayerProfiles.md)と現行コードで残件を再確認します。二段階選択・左右ペイン案と実装済みHANDLE選択の差分は、[当時の計画](../Archive/PlayerIntegration.md)に残しています。

## 選択画面・入力方式の未着手案

以下は2026-08-12に未着手とされた案です。同日のHANDLE選択実装と重なるため、採用する差分を確認してから着手します。

- [未着手] Player 選択画面を二段階化する。Entry Profile の選択後、その Entry Profile が現在のサービスで使える Client Identity を選び、黒白それぞれの Player を `Entry Profile + Client Identity` として確定する。
- [未着手] Player 選択画面は大きな左右二ペインとする。左半分に Entry Profile 一覧、右半分に左で選択中の Entry Profile が参照する Client Identity 一覧を表示し、同一ダイアログ内で両方を選択・確定する。
- [未着手] OnlineMatch (CGOS) では、ダイアログで確定した Client Identity の Handle と Password を接続開始画面の入力欄へコピーする。元画面での直接編集は今回の接続だけに使う一時ドラフトであり、Client Identity には保存しない。
- [未着手] LocalMatch では、ダイアログで確定した Client Identity の Handle を設定画面へコピーする。設定画面での直接編集は今回の対局・棋譜ファイル名だけに使う一時ドラフトであり、Client Identity には保存しない。Password は扱わない。
- [未着手] Player 関連 UI の操作要素は、常設の `SELECT` / `CHANGE` ボタンを原則置かない。選択リンクと直接編集欄は角丸の太いアンダーラインで表示し、マウスホバーまたは Tab フォーカス時だけ、クリック結果を `CHANGE`（選択ダイアログ）または `EDIT`（直接編集）として示す。START 等の主要アクションボタンは常設して目立たせる。

## 後続

- Client Identity の並べ替え・ページ送りは、最大5件の少数運用のため当面不要。
- LOGIN PASS の OS 保護ストア移行。
- CGOS／大会ルールへの Player 統合、旧 PlayerKind／エンジン選択状態の削除、ConnectionProfile の一般化は、本計画の従来どおりの後続課題。

## 元の検討経緯

[分割前の計画・調査記録](../Archive/PlayerIntegration.md)
