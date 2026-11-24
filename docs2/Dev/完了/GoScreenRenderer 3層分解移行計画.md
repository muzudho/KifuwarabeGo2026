# GoScreenRenderer 3層分解移行計画（完了）

完了日: 2026-08-15

## 目的

旧 `GoScreenRenderer` を次の3層へ分解する。

```text
GoPresentationRenderer
        ↓
KfwStationeryDrawingTools
        ↓
KfwScreenCanvas
        ↓
MonoGame
```

## 進捗

- [x] 第1段階: `KfwScreenCanvas` の抽出
- [x] 第2段階: `KfwStationeryDrawingTools` への移行
- [x] 第3段階: 囲碁固有資源と背景の分離
- [x] 第4段階: composition root の分離
- [x] 第5段階: `GoPresentationRenderer` の公開面整理
- [x] 第6段階: 旧名・ファイル名・コメントの整理
- [x] 全体ビルドとSmoke検証

## 完了した内容

### KfwScreenCanvas

- MonoGameの `GraphicsDevice`、`SpriteBatch`、UIフォント、基本テクスチャを所有する。
- `Begin` / `End`、仮想座標変換、矩形、角丸矩形、線、円、円弧、楕円、文字、テクスチャ描画を提供する。
- 囲碁のセッション、画面状態、Board、CGOS、Title、HUDを参照しない。
- `IDisposable` を実装し、所有するGPU資源を破棄する。
- 実装は203行で、低水準描画に限定されている。

### KfwStationeryDrawingTools

- 旧 `StationeryDrawingContext` を改名し、全56利用ファイルを新型へ移行した。
- 多数の `Action` / `Func` コールバックを廃止し、`KfwScreenCanvas` 1個を利用する構造へ変更した。
- 動的文字、キャレット、選択範囲、ボタン、選択指、Sticky Note、Section Label、情報帯、結果行、石アイコンなどの共通UI描画を所有する。
- `DynamicTextRenderer` を所有し、生成した文字テクスチャを破棄する。
- `DrawStoneCountStrip` は `GoAppSession` ではなく黒石数・白石数を受け取る。
- 囲碁石そのものの描画だけは `BoardRenderer.DrawStone` を注入して利用する。

### GoPresentationRenderer

- 盤面、右サイドパネル、モーダル、チャートの描画順序と画面状態判断を担当する。
- CGOS、GTP、Titleの個別Renderer公開プロパティを削除した。
- Title、CGOS観戦、CGOSログイン、接続先選択、各キャレット計算を用途別メソッドとして公開する。
- 個別Rendererを外部から取得するサービスロケーターとして利用されない構造にした。

### GoPresentationFactory

- Canvas、Stationery Tools、Board Lens、Board Renderer、各画面Rendererの生成と接続を旧 `GoScreenRenderer` から移した。
- Board Renderer生成後にStationeryへ石描画を渡す順序にし、Stationery構築時の `_boardRenderer!` 逆参照を解消した。
- 盤座標フォント、白石・黒石テクスチャは囲碁画面の組み立て側で生成する。
- `GoPresentationServices` が3層の寿命をまとめ、`Game1.Dispose` から破棄する。

### 背景とファイル名

- アプリ共通背景テーマを `StationeryUI/BackgroundRenderer` へ移した。
- `GoScreenRenderer.*.cs` だった13ファイルを、実際のクラス名に合わせて改名した。
- 旧 `GoScreenRenderer.cs` と旧 `StationeryDrawingContext.cs` を削除した。
- `Game1` の保持フィールドを `_presentationServices` とし、旧Renderer名を除去した。

## 最終状態

- `KfwScreenCanvas.cs`: 203行
- `KfwStationeryDrawingTools.cs`: 245行
- `GoPresentationRenderer.cs`: 118行
- `GoPresentationFactory.cs`: 74行
- `GoScreenRenderer` のコード参照: 0件
- `StationeryDrawingContext` のコード参照: 0件
- `GoScreenRenderer.*.cs` ファイル: 0件
- `GoPresentationRenderer` の公開Rendererプロパティ: 0件
- 旧 `GoScreenRenderer.Draw`: なし

## 完了条件

- [x] `GoScreenRenderer` が存在しない
- [x] `StationeryDrawingContext` が存在しない
- [x] `KfwScreenCanvas` はMonoGame描画の隠蔽と低水準プリミティブだけを担当する
- [x] `KfwStationeryDrawingTools` は共通UI描画を担当する
- [x] `GoPresentationRenderer` は囲碁アプリ固有の画面合成を担当する
- [x] `GoPresentationRenderer` がRendererのサービスロケーターとして利用されていない
- [x] 依存方向が `GoPresentationRenderer → KfwStationeryDrawingTools → KfwScreenCanvas → MonoGame` になっている
- [x] `Game1` が旧Renderer経由でCanvasやStationeryへアクセスしていない
- [x] 旧クラス名、旧ファイル名、移設途中コメントがコード上0件である
- [x] GPU資源の破棄経路がある

## 検証結果

- [x] `dotnet build .\KifuwarabeGo2026.slnx --no-restore`
  - 全9プロジェクト、警告0、エラー0
- [x] `dotnet run --project .\KifuwarabeGo2026.GameOasis.Gui.PortabilitySmoke\KifuwarabeGo2026.GameOasis.Gui.PortabilitySmoke.csproj --no-build`
  - PASS
- [x] `dotnet run --project .\KifuwarabeGo2026.Tests.GameOasis.Gui.Windows\KifuwarabeGo2026.Tests.GameOasis.Gui.Windows.csproj --no-build`
  - PASS
