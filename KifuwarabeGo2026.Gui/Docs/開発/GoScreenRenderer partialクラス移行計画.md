# GoScreenRenderer partial クラス移行計画

最終更新: 2026-08-15

## 目的

`GoScreenRenderer` に集まっている画面固有・部品固有・Board Lens 固有の処理を、それぞれの所有クラスへ移す。
最終的な `GoScreenRenderer` には、SpriteBatch、テクスチャ、フォント、および画面間で共有する最低限の描画基盤だけを残す。

画面や部品は原則として `GoScreenRenderer` を直接受け取らず、`StationeryDrawingContext` などの目的別描画モデルを受け取る。

## 現在の進捗

- 完了: 7項目
- 残存 partial: 基盤1ファイル、移行候補16ファイル
- 直近の確認: `KifuwarabeGo2026.Gui.Core.csproj` が警告0・エラー0でビルド成功

## 完了済み

- [x] `GoScreenRenderer` を引数に取る画面・右側パネルのメソッドを棚卸しし、`StationeryDrawingContext` を渡す形へ変更した。
- [x] `DrawRightSidePanel*` の単純な中継メソッドを削除した。
- [x] `DrawIconStone`、プレイヤー種別アイコン、石の値、文字選択、キャレットなどの共通描画を `StationeryDrawingContext` へ移した。
- [x] `IGoScreenRenderer` を削除し、連の境界 Lens が使う描画操作を `BoardLensModel` へ集約した。
- [x] 単純な中継だった `LocalIntermissionRenderer` を削除した。
- [x] 単純な中継だった `TitleRenderer` を削除し、`TitleScreen`、`PonnukiProviderSelectionScreen`、`ApplicationSettingsScreen` へ呼び出しを直結した。
- [x] `GoScreenRenderer.ApplicationSettings.cs` を削除し、設定画面の描画と設定ボタン描画を `ApplicationSettingsScreen` へ移した。

## 残っている partial クラス

### 描画基盤として残すもの

- [ ] `Presentation/GoScreenRenderer.cs`
  - 当面のホスト。
  - 最終的には SpriteBatch、フォント、テクスチャ、基本図形、描画コンテキスト生成だけに絞る。
  - 画面固有の状態、Bounds、ヒット判定、文言組み立てが残っていないか、各移行後に再棚卸しする。

### 画面クラスへ移すもの

- [ ] `Pages/Title/GoScreenRenderer.Title.cs`
  - 移行先候補: `TitleScreen` とタイトル画面配下の専用コンポーネント。
  - `ApplicationSettingsScreen.DrawSettingsButton` のように、所有画面へ描画処理を寄せる。
- [ ] `Pages/Board/GoScreenRenderer.Board.cs`
  - 移行先候補: 盤面専用の画面または描画コンポーネント。
  - 盤座標変換と基本描画の境界を先に決める。
- [ ] `Pages/BoardAndReview/GoScreenRenderer.BoardAndReview.cs`
  - 移行先候補: `BoardAndReviewScreen`。
- [ ] `Pages/EditTournamentRule/GoScreenRenderer.EditTournamentRule.cs`
  - 移行先候補: `TournamentRulesScreen` と編集パネル。
- [ ] `Pages/GtpEngine/GoScreenRenderer.GtpEngine.cs`
  - 移行先候補: GTPエンジン選択、編集、GUIオプション、削除確認の各画面・パネル。
  - 大きいため、ダイアログ単位に分割する。
- [ ] `Pages/MoveTrendChart/GoScreenRenderer.MoveTrendChart.cs`
  - 移行先候補: `MoveTrendChartScreen` またはチャート描画コンポーネント。
- [ ] `Pages/PopupTrendChart/GoScreenRenderer.PopupTrendChart.cs`
  - 移行先候補: `PopupTrendChartScreen` とポップアップ内の各パネル。
- [ ] `Pages/PopupTrendChart/MoveCommentPanel/GoScreenRenderer.MoveComments.cs`
  - 移行先候補: Move Comment 専用パネル。
- [ ] `Pages/OnlineMatch/Cgos/Login/GoScreenRenderer.Cgos.cs`
  - 移行先候補: `CgosLoginPage`、`CgosSelectConnectionPage` と各サブパネル。
  - 接続選択、ログイン、管理者操作、接続編集を分割する。
- [ ] `Pages/OnlineMatch/Cgos/Watch/GoScreenRenderer.CgosWatching.cs`
  - 移行先候補: `CgosWatchPage` と観戦用コンポーネント。

### 共通部品クラスへ移すもの

- [ ] `Shared/CatalogOrder/GoScreenRenderer.CatalogOrder.cs`
  - 移行先候補: Catalog Order の画面・カード・ナビゲーション部品。
- [ ] `Shared/EntryProfiles/GoScreenRenderer.EntryProfiles.cs`
  - 移行先候補: Entry Profile の選択・編集コンポーネント。
- [ ] `Shared/PlayersComponent/GoScreenRenderer.PlayersComponent.cs`
  - 移行先候補: Players Component と Player Row。
- [ ] `Shared/SelectEntry/GoScreenRenderer.SelectEntry.cs`
  - 移行先候補: Select Entry コンポーネント。

### Board Lens 内へ移すもの

- [ ] `BoardLens/GlassesSystem/ChippedSingleEyeGlassSeedLens.cs`
  - 現在も `partial GoScreenRenderer` なので、独立した Lens クラスへ変更する。
  - 描画操作は `BoardLensModel` または目的を限定した描画コンテキストから受け取る。
- [ ] `BoardLens/RenSystem/RenNetworkBasicLens.cs`
  - 独立した Lens クラスへ変更し、連ネットワーク固有の処理を集約する。

## 推奨する移行順序

1. 小さい共通部品: `SelectEntry`、`PlayersComponent`、`CatalogOrder`。
2. Board Lens 2件: 既に導入した `BoardLensModel` を利用する。
3. 小さい画面: `BoardAndReview`、`EditTournamentRule`、`MoveComments`。
4. チャート: `MoveTrendChart`、`PopupTrendChart`。
5. タイトルと盤面: `Title`、`Board`。
6. 大規模画面: `GtpEngine`、`Cgos`、`CgosWatching`。
7. 最後に `GoScreenRenderer.cs` 本体を再棚卸しする。

## 各ファイルの作業手順

1. partial 内の描画、Bounds、ヒット判定、保持状態、ヘルパーを分類する。
2. 対応する画面または部品クラスに描画メソッドを作る。
3. 必要な低水準描画だけを `StationeryDrawingContext` または目的別モデルへ追加する。
4. `GoScreenRenderer` 本体を引数として渡さない。
5. Bounds とヒット判定は同じUIインスタンスに所有させる。
6. 呼び出し側を新しい所有クラスへ切り替える。
7. partial ファイルと不要な中継メソッドを削除する。
8. 古い型名・メソッド名の残存参照を `rg` で確認する。
9. Coreをビルドし、必要に応じてWindows版とSmokeプロジェクトも確認する。

## 1項目の完了条件

- [ ] 対象ファイルが `partial class GoScreenRenderer` ではなくなっている。
- [ ] 画面または部品が `GoScreenRenderer` を直接参照していない。
- [ ] 描画依存が `StationeryDrawingContext` または目的別モデルだけになっている。
- [ ] Bounds、ヒット判定、表示状態の所有者が一致している。
- [ ] 旧メソッド、旧ラッパー、旧ファイルへの参照が残っていない。
- [ ] ビルドが警告・エラーなしで成功する。

## 全体の完了条件

- [ ] `partial class GoScreenRenderer` の宣言が `Presentation/GoScreenRenderer.cs` 以外に存在しない。
- [ ] `GoScreenRenderer.cs` に画面固有・部品固有・Board Lens固有の処理が残っていない。
- [ ] 全画面の描画経路が所有クラスから追跡できる。
- [ ] Core、Windows、および関連Smokeプロジェクトの検証が成功する。
