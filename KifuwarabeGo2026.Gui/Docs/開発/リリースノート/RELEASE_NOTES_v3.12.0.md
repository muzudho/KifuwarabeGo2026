# Kifuwarabe Go 2026 v3.12.0

画面構造をソースコード上の所有関係へ反映し、対局中・棋譜レビュー・コメント編集の視認性を改善したリリースです。

## Popup Trend Chart

- 着手コメントパネルを独立コンポーネントとして整理しました。
- 着手コメント、SCORE、WIN RATEをピンで表示・非表示にできます。
- SCOREとWIN RATEの両方を外すとチャート面も隠れ、盤とコメントへ集中できます。
- 着手コメントラベルは表示切替時に滑らかに移動します。

## 持ち時間とSGF

- USED、NOW、LIMITを割合バーと固定3列の時刻で表示します。
- 棒はUSEDを黒、現在思考分を青、残りを水色で表します。
- SGF FF[4]標準の `TM`、`BL`、`WL` を使い、時間制限と各着手後の残り時間を保存・読込します。
- 棋譜レビューでは記録された時間を復元します。時間情報のないSGFはハイフン表示になります。

## UIの視認性

- 複数行コメント入力の罫線を、実際のフォント行高とベースラインへ揃えました。
- 縮小ウィンドウでも罫線が最低1px見えるよう補正します。
- 文房具Buttonの文字を中央揃えにし、余白の大きい短いラベルを自動拡大します。

## 内部構造

- 画面固有のBounds、ヒット判定、内部状態を画面クラスと文房具UIへ移管しました。
- UIが自身の位置、サイズ、表示値、状態を所有し、親コンポーネントが子UIを所有する構造へ整理しました。
- `StationeryDrawingContext` を共通描画境界として、rendererとの結合を減らしました。

## 配布物

- GUI版: `KifuwarabeGo2026.Gui-v3.12.0-win-x64.zip`
- Engine版: `KifuwarabeGo2026.Engine-v3.12.0-win-x64.zip`

GUIとEngineの両方を使う場合は、2つともダウンロードしてください。

## テスト状況

- Releaseビルド成功（警告0、エラー0）
- PortabilitySmoke、WindowsSmoke成功
- 同梱CGOS通信コンポーネントの `--help` 成功
- EngineのGTP基本応答とversion `3.12.0`を確認
- Windows x64向けGUI版・Engine版をpublish

## SHA-256

- GUI版: `26390C42FAC7F5231A8BA2D1B796FBA0428C820D7AF3EADB4A2B63B62E5AECDE`
- Engine版: `B3F7651920520F74FCE00B298F289F14D10DE96C31FBA0AFEDD0426AE98D2DD1`

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime
- フレームワーク依存配布
