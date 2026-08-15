# GoScreenRenderer 残存責務移行計画（完了）

完了日: 2026-08-15

## 目的

`Presentation/GoScreenRenderer.cs` に残っていた画面固有・機能固有・部品固有の責務を所有クラスへ移し、`GoScreenRenderer` を MonoGame 描画資源の初期化、共通プリミティブ、描画コンテキスト生成に集中させる。

## 進捗

- [x] 第1段階: 薄いラッパーと画面固有入口の移行
- [x] 第2段階: RightSidePanel / Local Intermission の逆依存解消
- [x] 第3段階: Board Lens と盤面通知の移行
- [x] 第4段階: Dialog / Popup / HUD の移行
- [x] 第5段階: 画面効果の移行
- [x] 第6段階: 画面全体の合成と Renderer 所有関係の分離
- [x] 第7段階: 共通描画基盤の仕上げ
- [x] 全体ビルドと Smoke 検証

## 完了した移行

### 画面・部品固有の描画

- 人間プレイヤー名のキャレット計算を `LocalMatchScreen` へ移した。
- プレイヤー編集パネルの描画とキャレット計算を `EditEntryProfile` へ移した。
- 初期局面コンシェルジュを `InitialPositionConcierge` から直接描画するようにした。
- タイトル画面の入口を `TitleScreenRenderer.DrawScreen` へ移した。
- 保存中表示を `Shared/SavingOverlay/SavingOverlay` へ移した。
- Board Lens バナーを `BoardLensBanner` へ移した。
- CGOS 対局通知は既に `Shared/CgosMatchNotification` へ移行済みである。

### RightSidePanel / Local Intermission

- `GetMoveThinkingText` を対局中の右サイドパネルへ移した。
- 情報帯、結果行、石数帯、縦結果セクションの共通描画 API を `StationeryDrawingContext` に集約した。
- `RightSidePanels` と `LocalMatchIntermissionPage` から `drawingContext.ScreenRenderer` の参照をすべて除去した。
- `DrawPlayerRoleFaceIcon`、`DrawCrispCircleOutline` などの不要な中継メソッドを削除した。

### Dialog / Popup / HUD / Effect

- `TextInputDialog`、`TextAreaDialog`、`MessageDialog`、`Breadcrumb` が `StationeryDrawingContext` を受け取って自分で描画する構造へ移行した。
- `PopupNumberUnderline` と `PopupTimeUnderline` の描画・キャレット計算を各部品へ移した。
- Sticky Note の画面状態を `HeadUpDisplayComponent.Default` が直接所有するようにした。
- `ReviewUnsavedChangesConfirmation`、`ScreenTransition`、`ScreenshotEffect` を各所有クラスから直接描画するようにした。

### 画面合成と共通基盤

- 旧 `GoScreenRenderer.Draw` の画面合成を `GoPresentationRenderer` へ移した。
- Board、チャート、CGOS、GTP、Title の Renderer 群を `GoPresentationRenderer` に集約した。
- `Game1` は複数の Renderer を `GoScreenRenderer` の公開プロパティから取得せず、専用の presentation composition root を利用する。
- 動的文字テクスチャの生成・キャッシュを `StationeryUI/DynamicTextRenderer` へ分離した。
- `StationeryDrawingContext.ScreenRenderer` とコンストラクターの `GoScreenRenderer` 引数を削除した。
- `GoScreenRenderer` 本体の不要な `FillRectangle` / `DrawRectangle` 中継、選択描画、キャレット、各画面ラッパーを削除した。
- 楕円・円弧、背景、矩形、線、文字などは画面固有の判断を持たない共通描画プリミティブとして本体に残した。

## 最終状態

- `GoScreenRenderer.cs`: 400 行（開始時 985 行）
- `partial class GoScreenRenderer`: 0 件
- `StationeryDrawingContext.ScreenRenderer`: 0 件
- `RightSidePanels` / 各 Page からの `GoScreenRenderer` 参照: 0 件
- `GoScreenRenderer.Draw`: なし
- `GoScreenRenderer` の公開する画面固有 Renderer プロパティ: なし
- 画面合成の入口: `GoPresentationRenderer`

`GoScreenRenderer` に残る責務は、描画資源の初期化、`StationeryDrawingContext` と presentation composition root の構築、共通プリミティブの実装である。

## 検証結果

- [x] `dotnet build .\KifuwarabeGo2026.slnx --no-restore`
  - 9プロジェクト、警告 0、エラー 0
- [x] `dotnet run --project .\KifuwarabeGo2026.Gui.PortabilitySmoke\KifuwarabeGo2026.Gui.PortabilitySmoke.csproj --no-build`
  - PASS
- [x] `dotnet run --project .\KifuwarabeGo2026.Gui.WindowsSmoke\KifuwarabeGo2026.Gui.WindowsSmoke.csproj --no-build`
  - PASS

## 完了条件

- [x] 画面・機能・部品固有の文言、Bounds、状態判断を所有クラスへ移した。
- [x] 共通部品から `GoScreenRenderer` への逆依存を解消した。
- [x] `GoScreenRenderer` の画面固有ラッパーと重複実装を削除した。
- [x] 呼び出し元が所有クラスを直接利用する構造にした。
- [x] `GoScreenRenderer.Draw` から画面合成を分離した。
- [x] ソリューション全体と両 Smoke が成功した。
