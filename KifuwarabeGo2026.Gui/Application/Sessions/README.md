# Sessions

`GoAppSession` はアプリ全体の状態を統括します。責務ごとに partial class をこの配下へ分割します。

- `GoAppSession/GoAppSession.Review.cs`: SGF レビュー用レコードとレビュー復帰用コピー
- `GoAppSession/GoAppSession.Review.Start.cs`: 新規レビュー開始と保持済み棋譜の再開
- `GoAppSession/GoAppSession.Review.Position.cs`: 指定手数までの再生と表示用レコードの作成
- `GoAppSession/GoAppSession.Review.Comments.cs`: ルート・着手コメントの変更
- `GoAppSession/GoAppSession.Review.Exit.cs`: レビュー完了、破棄、休憩盤への復帰
- `VariationSession/GoAppSession.Variation.cs`: 変化図編集の開始位置と編集状態
- `VariationSession/GoAppSession.Variation.Lifecycle.cs`: 開始、採用用レコード作成、破棄
- `VariationSession/GoAppSession.Variation.Editing.cs`: 着手、盤面編集、アンドゥ、コメント編集
- `BoardEditing/GoAppSession.BoardEditing.cs`: 通常の盤面編集の開始・確定・取消
- `BoardEditing/GoAppSession.BoardEditing.Operations.cs`: 石の配置、全消去、アンドゥ／リドゥ
- `BoardEditing/GoAppSession.EditingHistory.cs`: 盤面編集と変化図編集で共有する編集履歴
- `GameRecords/Current/GoAppSession.CurrentGameRecord.cs`: 現在盤面からの棋譜レコード生成とメタデータ転記
- `GameRecords/Go/GoAppSession.GameRecordPosition.cs`: 棋譜を初期局面として盤面へ適用
- `Game/GoAppSession.MatchBackedGame.cs`: MatchSession に委譲する着手・パス・終局反映
- `Game/GoAppSession.LocalGame.cs`: 通常対局の着手・パス・投了・終局判定
- `Game/GoAppSession.GameLifecycle.cs`: ローカル対局の開始・中断
- `Game/GoAppSession.LocalReplay.cs`: 対局中・終局後のローカル棋譜シーク表示
- `MoveInformation/GoAppSession.MoveInformation.cs`: 着手情報、コメント、チャートの表示状態
- `BoardLens/GoAppSession.RenParser.cs`: 盤面レンズの切替状態と連解析キャッシュ
- `BoardState/GoAppSession.BoardState.cs`: 全モードで共有する盤面・手番・局面履歴の初期化
- `Cgos/GoAppSession.CgosCredentials.cs`: CGOSのログイン情報とポップアップ編集状態
- `GameRecords/Go`: `GoGameRecord` など棋譜モデルの配置候補
- `VariationSession`: 変化図編集状態の配置候補

`Current` や `BeforeReview` はインスタンス名であり、型の分類ではないため、個別フォルダーにはしません。

## レビュー棋譜の所有者と同期規則

| データ | 所有者 | 用途 |
| --- | --- | --- |
| `CurrentGameRecord` | `GoAppSession` | 現在の盤面を描画・操作するための棋譜 |
| `_reviewGameRecord` | `GoAppSession.Review` | SGF 全体、全手順、ルートコメントの正本 |
| `_beforeReviewGameRecord` | `GoAppSession.Review` | レビュー開始前の状態を退避するコピー |

- レビュー中のコメント更新は、まず `_reviewGameRecord` を更新する。
- 表示と編集の 0 手目コメントは `ReviewRootComment` を経由し、常に `_reviewGameRecord` を正本にする。
- `CurrentGameRecord` は盤面復元で作り直されることがあるため、レビューSGFの全情報を持つ前提にしない。
- 変化図編集で棋譜を再生するときは `ApplyRecordPosition` を使い、`_reviewGameRecord` を一時的にも差し替えない。
