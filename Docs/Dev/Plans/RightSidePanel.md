# RightSidePanel 残作業

記録時点の状況は[現状説明](../CurrentState/RightSidePanel.md)を参照してください。着手前に残存箇所を再確認します。

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
