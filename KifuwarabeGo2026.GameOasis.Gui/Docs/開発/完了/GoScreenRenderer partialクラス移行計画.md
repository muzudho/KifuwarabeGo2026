# GoScreenRenderer partial クラス移行計画（完了）

完了日: 2026-08-15

## 目的

`GoScreenRenderer` に集まっていた画面固有、部品固有、Board Lens 固有の描画処理を、それぞれの所有クラスへ移す。
画面や部品は `GoScreenRenderer` を直接受け取らず、`StationeryDrawingContext` または目的別の描画クラスを利用する構成にする。

## 最終結果

- [x] 移行対象だった 16 ファイルを、それぞれの画面・部品・Board Lens クラスへ移した。
- [x] `GoScreenRenderer` の `partial` 宣言をすべて解消した。
- [x] `Presentation/GoScreenRenderer.cs` 自体も通常の `sealed class` に戻した。
- [x] Board の連解析と Nobi Lens を `BoardRenderer` に集約し、`GoScreenRenderer` からの描画コールバックを解消した。
- [x] GTP・CGOS のヒット判定を所有 Renderer に移し、Game1 と Smoke の参照を追随させた。
- [x] ソリューション全体のビルドと Smoke を通した。

## 完了した共通基盤整理

- [x] `GoScreenRenderer` を引数に取っていた画面・右側パネル描画を棚卸しし、`StationeryDrawingContext` を渡す形へ変更した。
- [x] `DrawRightSidePanel*` など、共通描画を中継するだけのラッパーを整理した。
- [x] アイコン石、プレイヤー種別アイコン、文字選択、キャレットなどの共通描画を `StationeryDrawingContext` に移した。
- [x] `IGoScreenRenderer` を削除し、Board Lens の描画操作を `BoardLensModel` に集約した。
- [x] 中継だけだった `LocalIntermissionRenderer` と `TitleRenderer` を削除した。
- [x] Application Settings の描画を `ApplicationSettingsScreen` に移した。

## partial ファイル移行実績

### 画面

- [x] `Pages/Title/GoScreenRenderer.Title.cs` → `TitleScreenRenderer`
- [x] `Pages/Board/GoScreenRenderer.Board.cs` → `BoardRenderer`
- [x] `Pages/BoardAndReview/GoScreenRenderer.BoardAndReview.cs` → `BoardAndReviewScreen`（旧 partial ファイル削除）
- [x] `Pages/EditTournamentRule/GoScreenRenderer.EditTournamentRule.cs` → `TournamentRulesPresenter`
- [x] `Pages/GtpEngine/GoScreenRenderer.GtpEngine.cs` → `GtpEngineRenderer`
- [x] `Pages/MoveTrendChart/GoScreenRenderer.MoveTrendChart.cs` → `MoveTrendChartRenderer`
- [x] `Pages/PopupTrendChart/GoScreenRenderer.PopupTrendChart.cs` → `PopupTrendChartRenderer`
- [x] `Pages/PopupTrendChart/MoveCommentPanel/GoScreenRenderer.MoveComments.cs` → `MoveCommentPanelRenderer`
- [x] `Pages/OnlineMatch/Cgos/Login/GoScreenRenderer.Cgos.cs` → `CgosLoginRenderer`
- [x] `Pages/OnlineMatch/Cgos/Watch/GoScreenRenderer.CgosWatching.cs` → `CgosWatchingRenderer`

### 共通部品

- [x] `Shared/CatalogOrder/GoScreenRenderer.CatalogOrder.cs` → `CatalogOrderPresenter`
- [x] `Shared/EntryProfiles/GoScreenRenderer.EntryProfiles.cs` → `EntryProfilesPresenter`
- [x] `Shared/PlayersComponent/GoScreenRenderer.PlayersComponent.cs` → `PlayersComponent`
- [x] `Shared/SelectEntry/GoScreenRenderer.SelectEntry.cs` → `SelectEntryPresenter`

### Board Lens

- [x] `BoardLens/GlassesSystem/ChippedSingleEyeGlassSeedLens.cs` → 独立した `ChippedSingleEyeGlassSeedLens`
- [x] `BoardLens/RenSystem/RenNetworkBasicLens.cs` → 独立した `RenNetworkBasicLens`

## 最終監査

- [x] `.cs` ファイルに `partial class GoScreenRenderer` は存在しない。
- [x] 各移行対象は `GoScreenRenderer` ではなく、目的別クラスとして宣言されている。
- [x] Board 固有の連解析、連番号、境界、Nobi Lens 描画は `BoardRenderer` にある。
- [x] 旧 `GoScreenRenderer.GetGtpEngine*` 参照は、`GtpEngineRenderer` 参照へ更新した。
- [x] CGOS ログインの描画・ヒット判定・キャレット計算は `CgosLoginRenderer` にある。

ファイル名には移行元を追跡しやすくするため `GoScreenRenderer.*.cs` が一部残るが、クラス宣言はすべて独立クラスであり、partial クラスではない。

## 検証結果

2026-08-15 に以下を実行し、すべて成功した。

```text
dotnet build .\KifuwarabeGo2026.slnx --no-restore
結果: 8 プロジェクト成功、警告 0、エラー 0

dotnet run --project .\KifuwarabeGo2026.GameOasis.Gui.PortabilitySmoke\KifuwarabeGo2026.GameOasis.Gui.PortabilitySmoke.csproj --no-build
結果: PASS

dotnet run --project .\KifuwarabeGo2026.Tests.GameOasis.Gui.Windows\KifuwarabeGo2026.Tests.GameOasis.Gui.Windows.csproj --no-build
結果: PASS
```

## 完了条件

- [x] 全移行対象が独立クラスになっている。
- [x] `GoScreenRenderer` に Board Lens 固有処理が残っていない。
- [x] 全画面の描画経路を所有クラスから追跡できる。
- [x] Core、Windows、関連 Smoke の検証が成功している。
- [x] 本文書を `Docs/開発/完了` に移動した。
