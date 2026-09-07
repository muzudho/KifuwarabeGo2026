# DrawCommandButton の棚卸し（2026-08-15）

## 結論

`DrawCommandButton` はまだ C# の実装コードで使われていた。今回、実装コードから定義・呼び出し・コールバック名をすべて除去し、文房具 UI の `Button` を通る `DrawButton` に統一した。

- C# に残る `DrawCommandButton`: **0 件**
- ビルド: **成功（警告 0、エラー 0）**
- 過去の引き継ぎ文書・個人用チャットにある文字列: 履歴なので変更対象外

## 今分かっている終わっているもの

| 状態 | 場所 | 対応 |
| --- | --- | --- |
| 完了 | `Pages/GtpEngine/GtpEngineRenderer.cs` | 選択、編集、GUI オプション、ランダム着手、削除確認の各ボタンを `DrawButton` 経由に変更 |
| 完了 | `Shared/RightSidePanel/RightSidePanels.cs` | 参照ボタンと盤レンズ操作ボタンを変更 |
| 完了 | `Shared/CatalogOrder/` | 編集枠、Presenter、描画コールバックを変更 |
| 完了 | `Pages/InitialPositionConcierge/InitialPositionConcierge.cs` | 描画コールバックを変更 |
| 完了 | `StationeryUI/MessageDialog/MessageDialog.cs` | 動的メッセージボタンを変更 |
| 完了 | `StationeryUI/Controls/SinglelineTextUnderline/TextInputDialog.cs` | DEFAULT / CANCEL / OK と描画コールバックを変更 |
| 完了 | `StationeryUI/KfwStationeryDrawingTools.cs` | 旧メソッドを削除。動的ボタン用 `DrawButton` は内部で `Button` を生成し、`IsSelected` と `IsEnabled` を設定して `Button.Draw` を呼ぶ |

## 変えられた箇所

固定インスタンスを持てる既存画面は、従来どおり画面モデルの `Button` を直接描画する。今回見つかった旧 API 利用箇所は、表示時にラベル、有効状態、選択状態、または Bounds が変わるものが多いため、共通の動的ボタン入口 `KfwStationeryDrawingTools.DrawButton` に置き換えた。この入口も描画を再実装せず、文房具 UI の `Button` クラスを使用する。

これにより、旧 API と `Button` に二重化していた枠、影、ホバー、選択、無効状態、文字描画の実装は `Button` 側に一本化された。

## 変えられない箇所

現時点で、`DrawCommandButton` のまま残さなければならない実装箇所は **ない**。

ただし、次の箇所は固定の `Button` フィールドへは変えていない。

| 場所 | 固定インスタンス化しなかった理由 | 現在の扱い |
| --- | --- | --- |
| GTP エンジンの GUI オプション行など | 行数、Bounds、ラベル、選択・有効状態が実行時に決まる | 描画時に `Button` を生成する |
| 共通ダイアログ／カタログ順序編集 | 描画部品が描画サーフェスを直接参照せず、コールバック境界を持つ | コールバックの終端で `Button` を生成する |
| 右サイドパネル | セッション状態によりラベル・選択・有効状態が変わる | 動的 `DrawButton` を使用する |

これらは「Button 化できない」のではなく、永続的な Button インスタンスを画面モデルに保持する方式が適さない箇所である。描画そのものは今回すべて Button 化済み。

## 今分かっている残っているもの

- 実装上の `DrawCommandButton` 残件: **なし**
- 任意の追加確認: GUI を実際に起動し、旧描画から Button 描画へ統一したことによる文字サイズ・折返し・余白の目視確認
- 履歴文書内の `DrawCommandButton` という記述: 当時の記録として残す

## 確認方法

```powershell
rg -n --glob "*.cs" "DrawCommandButton" KifuwarabeGo2026.GameOasis.Gui
dotnet build KifuwarabeGo2026.slnx --no-restore
```

2026-08-15 実行結果: 検索結果 0 件、ビルド成功、警告 0、エラー 0。
