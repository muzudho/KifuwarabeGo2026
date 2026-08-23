# RightSidePanel 残作業

## 現在地

セットアップ、終局、盤編集、変化図、レビュー、ローカル対局中の右側パネルは専用クラスへ移設済み。現行コードに残る高水準の橋渡しは、休憩画面の `LocalMatchIntermissionPage.DrawRightSidePanelContent` である。

## 残作業

1. `LocalMatchIntermissionPage.DrawRightSidePanelContent` の描画本体、行座標、Button を `LocalMatchIntermissionRightSidePanel` へ移す。
2. CGOS 観戦の右側領域を必要に応じて専用パネルへ分離し、`RightSidePanelLayout` の共通座標を使う。
3. パネルが利用する共通表示を、低水準描画ではなく意味を持つ共通コンポーネントとして整理する。
4. `GoScreenRenderer` やページに残った右側パネル専用の互換 API を削除する。

## 完了条件

- `RightSidePanel` がページの高水準描画メソッドを呼ばない。
- 各ページが利用する右側パネルと操作部品を所有する。
- `rg -n "DrawRightSidePanel|RightSidePanelContent" KifuwarabeGo2026.GameOasis.Gui -g "*.cs"` で意図しない橋渡しがない。
- ソリューション全体が警告 0、エラー 0 でビルドできる。
