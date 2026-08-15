# GoScreenRenderer 3層分解移行計画

最終更新: 2026-08-15

## 目的

`Presentation/GoScreenRenderer.cs` を次の3つの責務へ分解する。

1. `KfwScreenCanvas`: MonoGame の描画資源と低水準描画を隠蔽する画面キャンバス
2. `KfwStationeryDrawingTools`: Canvas を使って共通UI部品を描く道具箱
3. `GoPresentationRenderer`: 囲碁アプリ固有の Renderer 所有と画面合成

依存方向は次の一方向とする。

```text
GoPresentationRenderer
        ↓
KfwStationeryDrawingTools
        ↓
KfwScreenCanvas
        ↓
MonoGame
```

`KfwScreenCanvas` に囲碁固有・画面固有の機能を追加しない。`KfwStationeryDrawingTools` に画面遷移や対局状態の判断を追加しない。`GoPresentationRenderer` に低水準描画の実装を追加しない。

## 現在地

- `GoScreenRenderer.cs`: 387行
- `StationeryDrawingContext.cs`: 251行
- `GoPresentationRenderer.cs`: 89行
- `partial class GoScreenRenderer`: 0件
- `StationeryDrawingContext.ScreenRenderer`: 0件
- `GoScreenRenderer.Draw`: なし
- `GoScreenRenderer` を直接保持するクラス: `Game1` のみ
- `StationeryDrawingContext` の参照: 57ファイル
- ソリューション全体: 警告0・エラー0
- Portability Smoke / Windows Smoke: PASS

## 目標とする責務

### KfwScreenCanvas

MonoGame の描画資源と描画セッションを所有する最下層とする。

- `GraphicsDevice`
- `SpriteBatch`
- UI用 `SpriteFont`
- 1ピクセルテクスチャ、円形テクスチャ
- `Begin` / `End`
- 仮想画面への座標変換
- 矩形、角丸矩形、線、円、円弧、楕円の描画
- 通常文字、フィット文字、中央文字、回転文字の描画と計測
- 汎用テクスチャ生成
- 画面サイズ
- 所有する GPU 資源の破棄

囲碁石、盤座標、付箋、ボタン、右サイドパネル、背景デザイン、画面合成は所有しない。

### KfwStationeryDrawingTools

`KfwScreenCanvas` を利用して、アプリ内で共通利用するUI表現を描く中間層とする。現在の `StationeryDrawingContext` は新しいクラスを並立させず、このクラスへ発展・改名する。

- 動的文字の描画とキャッシュ
- テキスト選択、キャレット
- コマンドボタン
- 選択指マーク
- Sticky Note
- Section Label
- データ行フレーム、結果ラベル、結果行、情報帯
- 石アイコン、プレイヤー役割アイコン、石数表示
- UI部品が利用する Canvas API の公開

`GoAppSession` 全体への依存は減らし、可能なら必要な数値・表示文字列・状態だけを引数にする。特に `DrawStoneCountStrip(GoAppSession, ...)` は黒石数・白石数などの表示モデルを受け取る形を候補とする。

### GoPresentationRenderer

囲碁アプリ固有の画面合成と Renderer 所有を担当する。

- `BoardRenderer`
- `MoveTrendChartRenderer`
- `PopupTrendChartRenderer`
- `CgosWatchingRenderer`
- `GtpEngineRenderer`
- `CgosLoginRenderer`
- `TitleScreenRenderer`
- 盤面・右サイドパネル・モーダル・チャートの描画順序
- `GoAppSession` に基づく画面状態の判断

MonoGame の `GraphicsDevice`、`SpriteBatch`、GPUテクスチャの生成方法は知らない構造を目標とする。

## 残っているもの

### 第1段階: KfwScreenCanvas の抽出

- [ ] `GoScreenRenderer` を `KfwScreenCanvas` へ改名・抽出する
- [ ] 次の低水準メソッドを Canvas へ置く
  - `FillRect`
  - `DrawRect`
  - `DrawRoundedFill`
  - `DrawLine`
  - `DrawCircle`
  - `DrawEllipseWire`
  - `DrawCircumscribedCircleArc`
  - `DrawInscribedEllipseArc`
  - `DrawText`
  - `DrawFittedText`
  - `DrawSharpCenteredFittedText`
  - `DrawRotatedCenteredText`
  - `CreateTexture`
  - `CreateCircleTexture`
- [ ] `GraphicsDevice`、`SpriteBatch`、UIフォント、`_pixel`、`_softCircle` を Canvas の所有にする
- [ ] `Begin` / `End` と仮想座標変換を Canvas のAPIにする
- [ ] `GetTextBoxCaretIndex` を文字計測APIだけで実装し、Stationery側へ移す
- [ ] GPU資源の寿命を明確にし、必要なら `IDisposable` を実装する
- [ ] Canvas が `GoAppSession`、Board、CGOS、Title、HUDを参照しないことを確認する

### 第2段階: KfwStationeryDrawingTools への移行

- [ ] `StationeryDrawingContext` を `KfwStationeryDrawingTools` へ改名する
- [ ] 多数の `Action` / `Func` コールバックを `KfwScreenCanvas` 1個への依存へ置き換える
- [ ] `DynamicTextRenderer` を Stationery Tools が所有する
- [ ] `DrawCommandButton` を `GoScreenRenderer` から Stationery Tools へ移す
- [ ] `DrawStickyNote` を Stationery Tools へ移す
- [ ] `DrawSelectionFingerMark` を Stationery Tools へ移す
- [ ] `DrawIconStone` と `DrawPlayerRoleFaceIcon` の石描画依存を整理する
- [ ] `DrawStoneCountStrip` から `GoAppSession` 依存を除く
- [ ] 57ファイルの引数型・フィールド型を段階的に `KfwStationeryDrawingTools` へ変更する
- [ ] `StationeryDrawingContext` という旧型名と互換ラッパーを最終的に削除する

### 第3段階: 囲碁固有資源の分離

- [ ] `_boardCoordinateFont` を `BoardRenderer` の生成側へ移す
- [ ] `_stoneLight` / `_stoneDark` の生成と所有を `BoardRenderer` または石描画専用クラスへ移す
- [ ] `BoardLensModel` の生成を Canvas から切り離す
- [ ] `BoardRenderer` 初期化前のコールバックが `_boardRenderer!` を参照する循環的な組み立てを解消する
- [ ] Title用の楕円・円弧は Canvas APIを直接利用する
- [ ] 背景デザイン `DrawBackground` を `Shared/BackgroundRenderer` などの所有クラスへ移す
- [ ] Canvas には囲碁石・盤面・背景テーマの知識を残さない

### 第4段階: composition root の分離

- [ ] 現在 `GoScreenRenderer` のコンストラクターにある全Renderer生成を専用Factoryへ移す
- [ ] Factory名は `GoPresentationFactory` または同等の組み立て責務が分かる名前にする
- [ ] Factoryが `KfwScreenCanvas`、`KfwStationeryDrawingTools`、`GoPresentationRenderer` を生成して接続する
- [ ] `GoPresentationRenderer` は描画順序と画面状態判断だけを担当する
- [ ] `Game1` の `_renderer` を、3つの依存または専用の生成結果オブジェクトへ置き換える
- [ ] `Game1` の `_renderer.StationeryDrawingContext` を Stationery Tools の直接参照へ置き換える
- [ ] `Game1` の `_renderer.Presentation` を `GoPresentationRenderer` の直接参照へ置き換える
- [ ] 旧 `GoScreenRenderer` クラスを削除する

### 第5段階: GoPresentationRenderer の公開面整理

- [ ] `CgosWatchingRenderer`、`GtpEngineRenderer`、`CgosLoginRenderer`、`TitleScreenRenderer` の公開プロパティ利用箇所を棚卸しする
- [ ] 画面描画の入口を可能な限り `GoPresentationRenderer` のメソッドへ集約する
- [ ] 各PageへRendererを直接渡す必要がある場合は、所有関係と理由を明記する
- [ ] `GoPresentationRenderer` が新しいサービスロケーターにならないようにする

### 第6段階: 命名・ファイル名・コメント整理

- [ ] `GoScreenRenderer.*.cs` という旧名を含むファイルを実クラス名に合わせて改名する
- [ ] `GoScreenRenderer` を前提としたXMLコメントと移設途中コメントを削除・更新する
- [ ] `StationeryDrawingContext` を前提とした変数名・recordプロパティ名を更新する
- [ ] 名前空間を `Presentation/Canvas`、`Presentation/StationeryUI`、`Presentation` の責務に合わせて整理する
- [ ] `rg` で旧クラス名が0件であることを確認する

## 今分かっている完了済みのもの

- [x] `GoScreenRenderer` の partial クラスをすべて独立 Renderer / Screen / Presenter へ移した
- [x] `GoScreenRenderer.Draw` を廃止し、画面合成を `GoPresentationRenderer.Draw` へ移した
- [x] Board、Title、CGOS、GTP、チャートの各Rendererは独立クラスになっている
- [x] `StationeryDrawingContext.ScreenRenderer` の逆依存を削除した
- [x] RightSidePanel と各Pageから `GoScreenRenderer` の直接参照を削除した
- [x] Dialog、Popup、HUD、画面効果は所有クラスから Stationery API を使って描画する
- [x] 動的文字の生成・キャッシュを `DynamicTextRenderer` へ分離した
- [x] CGOS対局通知、Board Lensバナー、保存オーバーレイを所有フォルダーへ移した
- [x] `Game1` 以外は `GoScreenRenderer` を保持していない
- [x] `GoScreenRenderer` は開始時985行から387行まで縮小した
- [x] 現時点でソリューション全体が警告0・エラー0でビルドできる
- [x] Portability Smoke と Windows Smoke がPASSしている

## 推奨する移行順序

1. `KfwScreenCanvas` を作り、低水準描画とMonoGame資源を移す
2. `StationeryDrawingContext` のコールバック群を Canvas 依存へ置き換える
3. `KfwStationeryDrawingTools` へ改名し、共通UI描画を移す
4. Board・石・背景など囲碁固有資源を Canvas から外す
5. 専用Factoryへ3層の生成・接続を移す
6. `Game1` を3層の直接利用へ変更する
7. `GoPresentationRenderer` の公開Rendererプロパティを整理する
8. 旧 `GoScreenRenderer`、旧 `StationeryDrawingContext`、旧ファイル名を削除する
9. 全参照監査、全体ビルド、Smokeを実行する

この順序では、各段階でビルド可能な状態を維持する。大規模な型名変更は、Canvas抽出とコールバック整理が終わってから行う。

## 各段階の完了条件

- [ ] 対象責務の実装が3層の正しい所有クラスにある
- [ ] 上位層から下位層への一方向依存になっている
- [ ] 下位層が `GoAppSession` や画面固有Rendererを参照していない
- [ ] 一時ラッパーと重複実装が残っていない
- [ ] ソリューション全体が警告0・エラー0でビルドできる
- [ ] Portability Smoke と Windows Smoke がPASSする

## 全体の完了条件

- [ ] `GoScreenRenderer` が存在しない
- [ ] `StationeryDrawingContext` が存在しない
- [ ] `KfwScreenCanvas` はMonoGame描画の隠蔽と低水準プリミティブだけを担当する
- [ ] `KfwStationeryDrawingTools` は共通UI描画だけを担当する
- [ ] `GoPresentationRenderer` は囲碁アプリ固有の画面合成だけを担当する
- [ ] `GoPresentationRenderer` がRendererのサービスロケーターとして利用されていない
- [ ] 依存方向が `GoPresentationRenderer → KfwStationeryDrawingTools → KfwScreenCanvas → MonoGame` になっている
- [ ] `Game1` が旧Renderer経由でCanvasやStationeryへアクセスしていない
- [ ] 旧クラス名・旧ファイル名・移設途中コメントが0件である
- [ ] Core、Windows、Portability Smoke、Windows Smokeの検証が成功する
