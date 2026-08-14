# GoScreenRenderer 構造化・完了記録

最終更新: 2026-08-14

この文書は完了済み作業の履歴である。現在の残作業は
`Docs/開発/GoScreenRenderer構造化・引継ぎ.md` を参照する。

## 完了した画面移管

- コメント入力ダイアログ
  - `TextAreaDialog` が領域とボタンを所有し、旧Bounds・旧ヒットAPIを削除した。
- アプリ設定
  - `ApplicationSettingsScreen` が操作UIと設定リンクを所有する。
- CGOS観戦
  - `CgosWatchingScreen` がLEAVE VIEW、KIFU REVIEW、SGF OUTPUTを所有する。
- 大会ルール編集
  - `TournamentRulesScreen` が領域、入力UI、選択・dirty状態を所有する。
- 盤編集・変化図編集・棋譜レビュー
  - `BoardAndReviewScreen` 配下の操作グループへ移管した。
- エントリー選択・エントリープロファイル
  - `SelectEntryScreen`、`EntryProfilesScreen`、`ProfileSelection`、`ProfileEdit` へ移管した。
- タイトル・ポン抜きプロバイダー選択
  - `TitleScreen` と `PonnukiProviderSelectionScreen` へ移管した。
- Popup Trend Chart
  - `PopupTrendChartScreen` が画面領域、`MoveCommentPanel`、SCORE軸、WIN RATE軸を所有する。
- コメント表示
  - `MoveCommentsScreen` が `TableRowLabel`、本文領域、5ボタンを所有する。
- ローカル対局の操作UI
  - 14ボタンとプレイヤー選択・人間名入力のヒット判定を `LocalMatchScreen` 配下へ移管した。

## 完了した文房具UI・共通部品

- `StationeryDrawingContext`
- `Button`
- `Headline`
- `SectionLabelComponent`
- `ChartAxisSectionLabelComponent`
- `TableRowLabel`
- `LinkUnderline`
- `MultilineTextUnderline`
- `SinglelineTextUnderline`／`TextInputDialog`
- `PopupNumberUnderline`
- `PopupTimeUnderline`
- `StickyNote` 本体
- `Breadcrumb`
- `SpinBox`
- `CgosMatchNotification`
- `EditEntryProfile`
- `PlayerRow` と配下のプレイヤー表示部品
- `PlayerTimeUsageBar`
- `TitleGoEquipment`
- `ScreenTransition`
- `ScreenshotEffect`
- `ReviewUnsavedChangesConfirmation`
- `InitialPositionConcierge`
- `MoveAnalysis`

## 確立した構造化方針

- UIは描画関数だけでなく、Bounds、表示値、選択・ホバーなどの状態を所有する。
- 親画面／親コンポーネントが子UIをプロパティとして所有する。
- 描画とヒット判定は同じUIインスタンスを参照する。
- 文房具UIはrendererを参照せず、共通の `StationeryDrawingContext` を使う。
- `SectionLabelComponent` は対象区画と自身のBounds、方向、任意の表示ピンを所有する。
- Popup Trend Chartの着手コメント、SCORE、WIN RATEはピンで表示を切り替える。
- `PlayerTimeUsageBar` はBoundsとUSED／NOW／LIMITを所有する。

## 旧文書から統合したもの

- `Presentation/Bounds移管・引継ぎメモ.md`
- `Presentation/GoScreenRenderer.ヒット判定棚卸.md`
- `Presentation/ページ分割計画.md`

完了事項と残作業が混在していたため、2026-08-14に本記録と新しい引き継ぎ文書へ分割した。

