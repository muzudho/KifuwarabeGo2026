# GoScreenRenderer 残存責務移行計画

最終更新: 2026-08-15

## 目的

`Presentation/GoScreenRenderer.cs` に残っている責務を棚卸しし、画面固有・機能固有・部品固有の処理を所有クラスへ移す。
最終的な `GoScreenRenderer` は、MonoGame の描画資源を初期化して共通描画コンテキストを提供する薄い描画基盤にする。

本計画では、単に別クラスを呼ぶラッパーを増やさない。呼び出し元は可能な限り、所有クラスと `StationeryDrawingContext` を直接利用する。

## 現在地

- `GoScreenRenderer.cs`: 985 行
- `partial class GoScreenRenderer`: 0 件
- `GoScreenRenderer` の CGOS 対局通知参照: 0 件
- `StationeryDrawingContext.ScreenRenderer` の逆参照: 17 件
  - `RightSidePanels.cs`: 15 件
  - `LocalMatchIntermissionPage.cs`: 1 件
  - `StationeryDrawingContext` 自身の保持: 1 件
- 直近の検証: ソリューション全体が警告 0・エラー 0、Portability Smoke と Windows Smoke が PASS

## 判定基準

### GoScreenRenderer に残してよいもの

- `GraphicsDevice`、`SpriteBatch`、フォント、1 ピクセルテクスチャーなどの描画資源の生成と寿命管理
- 仮想画面変換を伴う描画セッションの開始・終了
- `StationeryDrawingContext` を構成する最低水準のプリミティブ描画
- 所有 Renderer を生成する composition root。ただし、画面ごとの描画順序や状態判定は別クラスへ移す

### 移すもの

- 特定画面、特定モーダル、右側パネル、Board Lens、タイトルなどのレイアウトと文言
- 特定 UI の Bounds、ヒット判定、キャレット計算、表示状態
- 既存の所有クラスを呼ぶだけのラッパー
- `StationeryDrawingContext.ScreenRenderer` を経由しなければ使えない共通部品

## 残っているもの

### 第1段階: 既存所有クラスへ直接移せる薄いラッパー

- [ ] 人間プレイヤー名のキャレット計算
  - 現在: `GetHumanPlayerNameCaretIndex`
  - 呼び出し元: `Game1`
  - 移行先候補: `EditEntryProfile` または `LocalMatchScreen`
  - `StationeryDrawingContext.GetTextCaretIndex` と対象 Bounds を使えば `GoScreenRenderer` は不要
- [ ] プレイヤー編集パネル
  - 現在: `EditEntryProfile` プロパティ、`DrawPlayerEditPanel`、`GetPlayerEditPanelCaretIndex`
  - 移行先: `Presentation/Shared/EditEntryProfile`
  - `Game1` と画面合成クラスが `EditEntryProfile` を直接所有・参照する
- [ ] 初期局面コンシェルジュ描画
  - 現在: `InitialPositionConcierge` プロパティ、`DrawInitialPositionConciergeContent`
  - 移行先: `Pages/InitialPositionConcierge/InitialPositionConcierge`
  - `RightSidePanels` から `drawingContext.ScreenRenderer` を経由せず直接描画する
- [ ] タイトル画面入口
  - 現在: `_titleScreenRenderer`、`DrawUseSelection`
  - 移行先: `TitleScreenRenderer` または `TitleScreen`
  - 物理座標変換と描画セッションも `StationeryDrawingContext` で実行する
- [ ] 保存中オーバーレイ
  - 現在: `DrawSavingOverlay`
  - 移行先候補: `Presentation/Shared/SavingOverlay/SavingOverlay.cs`
  - レイアウト、文言、スピナーを専用部品へ集約する

### 第2段階: 右側パネル固有処理

- [ ] `GetMoveThinkingText`
  - 移行先: `Shared/RightSidePanel` 内の対局中パネル
- [ ] `DrawInfoStrip`
  - 移行先: `Shared/RightSidePanel` の専用部品、または `StationeryDrawingContext`
- [ ] `DrawResultRow` と `DrawResultLabel`
  - 利用箇所: RightSidePanel、Local Intermission、Move Analysis
  - 移行先候補: `Shared/ResultRow/ResultRow.cs`
- [ ] `DrawStoneCountStrip`
  - 移行先: `Shared/RightSidePanel` の石数表示部品
- [ ] `DrawPlayerRoleFaceIcon`
  - 同等処理は `StationeryDrawingContext.DrawPlayerRoleFaceIcon` に移行済み
  - 本体側の利用を確認し、重複なら削除する
- [ ] `RightSidePanels.cs` 内の `drawingContext.ScreenRenderer` 参照 15 件を 0 件にする
- [ ] `LocalMatchIntermissionPage.cs` 内の `drawingContext.ScreenRenderer` 参照を 0 件にする

### 第3段階: Board Lens と盤面通知

- [ ] `DrawBoardLensBanner`
  - 呼び出し元: `Game1`
  - 移行先候補: `Presentation/BoardLens/BoardLensBanner.cs`
  - Lens 名、別名、ガイド、進捗表示を所有する
- [ ] `DrawCrispCircleOutline`
  - Board Lens バナーだけの補助ならバナーと一緒に移す
- [ ] `BoardLensModel` の生成
  - 現在: `_boardLensModel` を `GoScreenRenderer` が組み立てる
  - 当面は composition root として残してよい
  - 描画コールバックが `StationeryDrawingContext` だけで表せるようになった段階で `BoardRenderer` 側へ移す

### 第4段階: ダイアログと HUD

- [ ] Text Input Dialog 一式
  - 現在: `GetTextInputDialog*`、`IsTextInputDialogTextBoxHit`、`GetTextInputDialogCaretIndex`、`DrawTextInputDialog`
  - 補助: `DrawCompositionLamp`、`DrawDynamicCompositionText`、`DrawTextBoxSelection`
  - 移行先: `StationeryUI` の `TextInputDialog`
- [ ] Message Dialog
  - 現在: `DrawMessageDialog`
  - 移行先: `StationeryUI/MessageDialog`
- [ ] Review Unsaved Changes Confirmation
  - 現在: `DrawReviewUnsavedChangesConfirmation`
  - 移行先: `Pages/ReviewUnsavedChangesConfirmation`
- [ ] Popup Number Underline
  - 現在: `DrawPopupNumberUnderline`、`GetPopupNumberUnderlineCaretIndex`
  - 移行先: `StationeryUI/Controls/PopupNumberUnderline`
- [ ] Popup Time Underline
  - 現在: `DrawPopupTimeUnderline`、`GetPopupTimeUnderlineCaretIndex`
  - 移行先: `StationeryUI/Controls/PopupTimeUnderline`
- [ ] Text Area Dialog 一式
  - 現在: `DrawTextAreaDialog`、`DrawTextAreaContent`、`GetTextAreaCaretPosition`
  - 状態: `_multilineTextUnderline` とラスタライズ済みテクスチャーも本体が所有
  - 移行先: `Shared/TextAreaDialog`
- [ ] Breadcrumb
  - 現在: `DrawBreadcrumb`
  - 移行先: `Shared/Breadcrumb/Breadcrumb`
- [ ] Sticky Note の画面状態設定
  - 現在: `HeadUpDisplay` プロパティ、`SetStickyNoteScreen`、`DrawStickyNote`
  - 移行先候補: `HeadUpDisplayComponent` と `StationeryDrawingContext`
  - `Game1` は `HeadUpDisplay` または画面状態モデルを直接更新する

### 第5段階: 画面効果

- [ ] `DrawLightningScreenTransition`
  - 移行先: `Pages/ScreenTransition`
- [ ] `DrawScreenshotCaptureEffect`
  - 移行先: `Pages/ScreenshotEffect`
- [ ] 各効果クラスに `StationeryDrawingContext` を受け取る描画入口を追加し、`Game1` から直接呼ぶ

### 第6段階: 画面全体の合成

- [ ] `Draw`
  - 現在、盤面、右側パネル、チャート、ルール、エントリー選択、プレイヤー編集、GTP 編集を一つのメソッドで合成している
  - 移行先候補: `BoardAndReviewScreen` または新しい `BoardAndReviewRenderer`
  - モーダル判定と描画順序を画面所有クラスへ移す
- [ ] Renderer 所有フィールド／プロパティ
  - `_boardRenderer`
  - `_moveCommentPanelRenderer`
  - `_moveTrendChartRenderer`
  - `_popupTrendChartRenderer`
  - `_titleScreenRenderer`
  - `CgosWatchingRenderer`
  - `GtpEngineRenderer`
  - `CgosLoginRenderer`
  - これらは描画基盤ではなく画面構成要素である
  - 専用の presentation composition root を新設するか、対応 Screen に所有させる
- [ ] `Game1` が必要な所有 Renderer を `GoScreenRenderer` の公開プロパティ経由で取得している構造を解消する

### 第7段階: 共通描画基盤の仕上げ

- [ ] 動的文字列テクスチャー
  - 現在: `_dynamicOptionTextTextures`、`DrawDynamicOptionText`
  - 移行先候補: `StationeryUI/DynamicTextRenderer`
  - テクスチャーの破棄責務も明確にする
- [ ] `StationeryDrawingContext.ScreenRenderer` を削除する
  - コンストラクターから `GoScreenRenderer` 引数を削除する
  - 共通部品から `GoScreenRenderer` への逆依存を 0 件にする
- [ ] `FillRectangle`、`DrawRectangle` など本体の internal ブリッジを削除する
  - 利用側は `StationeryDrawingContext` を直接使う
- [ ] `DrawEllipseWire`、`DrawCircumscribedCircleArc`、`DrawInscribedEllipseArc`
  - Title と Board Lens の専用描画へ移すか、汎用図形 API として `StationeryDrawingContext` に置く
- [ ] `DrawBackground`
  - 画面共通背景として残すなら `StationeryDrawingContext.DrawBackground` の実装詳細にする
  - レイアウト／装飾が製品固有なら `Shared/BackgroundRenderer` へ分ける
- [ ] `CreateTexture`、`CreateCircleTexture`、`FillRect`、`DrawRect`、`DrawLine`、`DrawCircle`、`DrawText`、`DrawFittedText` などだけが本体に残る状態を目指す

## 完了しているもの

- [x] `GoScreenRenderer` を引数に取る画面・右側パネル API を `StationeryDrawingContext` 引数へ変更した
- [x] `DrawRightSidePanel*` と `DrawIconStone` 系の単純ラッパーを整理した
- [x] Application Settings を `ApplicationSettingsScreen` へ移した
- [x] Title、Board、BoardAndReview、Tournament Rules、GTP Engine、CGOS Login、CGOS Watch を独立 Renderer／Screen へ移した
- [x] Move Trend Chart、Popup Trend Chart、Move Comment Panel を独立 Renderer へ移した
- [x] Catalog Order、Entry Profiles、Players Component、Select Entry を独立 Presenter／Component へ移した
- [x] Board Lens の `ChippedSingleEyeGlassSeedLens` と `RenNetworkBasicLens` を独立させた
- [x] Board の連解析と Nobi Lens を `BoardRenderer` へ移した
- [x] `IGoScreenRenderer` を削除し、Board Lens の描画操作を `BoardLensModel` に集約した
- [x] `LocalIntermissionRenderer` と `TitleRenderer` の単純ラッパークラスを削除した
- [x] CGOS 対局通知の描画、メッセージ構築、座標変換、ヒット判定を `Shared/CgosMatchNotification` へ移した
- [x] 移行後に未使用となった `GoScreenRenderer` のフィールド、プロパティ、補助メソッドを削除した
- [x] `partial class GoScreenRenderer` をすべて解消した

詳細な完了記録は `Docs/開発/完了/GoScreenRenderer partialクラス移行計画.md` を参照する。

## 推奨する移行順序

1. 初期局面コンシェルジュ、プレイヤー編集、タイトル入口、保存オーバーレイ
2. RightSidePanel と Local Intermission の `ScreenRenderer` 依存
3. Board Lens バナー
4. Breadcrumb、各 Popup、各 Dialog、Sticky Note 状態
5. Screen Transition と Screenshot Effect
6. 盤面・レビュー画面の合成 `Draw`
7. Renderer の所有関係を presentation composition root へ移す
8. 動的文字列描画と図形描画を共通基盤へ整理
9. `StationeryDrawingContext.ScreenRenderer` を削除

## 各項目の作業手順

1. 呼び出し元、Bounds、ヒット判定、状態所有者を `rg` で確認する
2. 所有クラスに `StationeryDrawingContext` を受け取る API を作る
3. 描画、座標変換、Begin/End、ヒット判定を所有クラスへ移す
4. `Game1` または画面合成クラスから所有クラスを直接呼ぶ
5. `GoScreenRenderer` のラッパー、フィールド、using を削除する
6. 同名の旧参照と `drawingContext.ScreenRenderer` 参照を再検索する
7. ソリューション全体をビルドする
8. Portability Smoke と Windows Smoke を実行する

## 1項目の完了条件

- [ ] 対象のレイアウト、文言、Bounds、状態が所有クラスにある
- [ ] 対象クラスが `GoScreenRenderer` を直接参照しない
- [ ] `GoScreenRenderer` に対象のラッパーや重複実装が残っていない
- [ ] 呼び出し元が所有クラスを直接利用している
- [ ] ソリューション全体が警告 0・エラー 0 でビルドできる
- [ ] 関連 Smoke が PASS する

## 全体の完了条件

- [ ] `GoScreenRenderer` に画面・機能・部品固有の文言、Bounds、状態判定がない
- [ ] `StationeryDrawingContext.ScreenRenderer` が存在しない
- [ ] `RightSidePanels` と各 Page が `GoScreenRenderer` を参照しない
- [ ] Renderer の公開プロパティを `Game1` がサービスロケーターのように利用していない
- [ ] `GoScreenRenderer.Draw` が画面固有の合成処理を持たない
- [ ] `GoScreenRenderer` が描画資源、共通プリミティブ、描画コンテキスト生成だけを担当する
- [ ] Core、Windows、Portability Smoke、Windows Smoke の検証が成功する
