# Sessions

`GoAppSession` はアプリ全体の状態を統括します。責務ごとに partial class をこの配下へ分割します。

- `GoAppSession/GoAppSession.Review.cs`: SGF レビュー用レコードとレビュー復帰用コピー
- `GameRecords/Go`: `GoGameRecord` など棋譜モデルの配置候補
- `VariationSession`: 変化図編集状態の配置候補

`Current` や `BeforeReview` はインスタンス名であり、型の分類ではないため、個別フォルダーにはしません。
