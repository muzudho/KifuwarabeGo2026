# GoScreenRenderer のヒット判定棚卸

対象は `Presentation/GoScreenRenderer.cs` に**直接定義されている**、マウスの仮想座標 `Point` に対するヒット判定です。`GoScreenRenderer.*.cs` にある partial class 側の判定は含めません。

進捗の正本と、完了済み／残作業の一覧は [Bounds移管・引継ぎメモ.md](Bounds移管・引継ぎメモ.md) にあります。この文書は残っている判定の詳細を調べるときに使います。

2026-08-14 の再集計時点で、このファイルに直接残る公開判定は12個、内部公開判定は1個です。旧34判定のうち、コメント入力、ローカル対局ボタン、プレイヤー種別、人間名入力の判定は移管済みで、共用していた private helper も削除済みです。

## 移管済みで、このファイルから削除した判定

- コメント入力：DISCARD、SAVE & CLOSE。
- ローカル対局：BACK、START、CHANGE、GAME SETTINGS、SEED AUTO、SGF入出力、終局後検討、PASS、RESIGN、CANCEL。
- プレイヤー設定：通常／ポン抜きの黒白プレイヤー種別、人間名入力。

これらの現在の所有先は `TextAreaDialog`、`LocalMatchScreen`、`PlayerKindSelectionRow` です。

## タイトル画面

| メソッド | 戻り値 | 対象 | 所有状態 |
| --- | --- | --- | --- |
| `GetTitleHomeLocalButtonHit` | `bool` | HOME の LOCAL MATCH | `TitleScreen.Default.LocalMatchButton.Bounds` を参照。 |
| `GetTitleHomeCgosButtonHit` | `bool` | HOME の CGOS CLIENT | `TitleScreen.Default.CgosClientButton.Bounds` を参照。 |
| `GetTitleAppHit` | `int?` | HOME のアプリカード | 現在は index 0（CAPTURE GAME）だけを走査する。 |

タイトルの BACK、プロバイダー選択の NEXT／RECHECK PROVIDER、ENGINE CHANGE リンクは、2026-08-14 にこの表から除去済みです。`TitleRenderer` はそれぞれ `TitleScreen` の `Button.IsHit`、`PonnukiProviderSelectionScreen` の `Button.IsHit`／`LinkUnderline.IsHit` を直接呼びます。

## すでに UI コンポーネントへ委譲している互換ラッパー

| メソッド | 委譲先 | 対象 |
| --- | --- | --- |
| `IsTournamentRulesSettingsFileHit` | `_tournamentRulesSettingsFileLinkUnderline.IsHit` | 大会ルール設定ファイルのリンク。唯一のインスタンスメソッド。 |
| `GetTextInputDialogCancelButtonHit` | `TextInputDialog.IsCancelButtonHit` | テキスト入力ダイアログ CANCEL。 |
| `GetTextInputDialogOkButtonHit` | `TextInputDialog.IsOkButtonHit` | テキスト入力ダイアログ OK。 |
| `GetTextInputDialogDefaultButtonHit` | `TextInputDialog.IsDefaultButtonHit` | テキスト入力ダイアログ DEFAULT。 |
| `IsTextInputDialogTextBoxHit` | `TextInputDialog.IsTextBoxHit` | テキスト入力欄。 |
| `GetCgosMatchWatchNowHit` | `CgosMatchNotification.IsWatchNowHit` | CGOS 対局通知の即時観戦。`enabled` も渡す。 |
| `GetCgosMatchWatchLaterHit` | `CgosMatchNotification.IsWatchLaterHit` | CGOS 対局通知の後で観戦。`enabled` も渡す。 |
| `GetCgosMatchDeferredHit` | `CgosMatchNotification.IsDeferredHit` | CGOS 対局通知の保留。 |
| `GetCgosMatchDeferredBannerHit` | `CgosMatchNotification.IsDeferredBannerHit` | CGOS 保留バナー。 |
| `GetLocalPlayingBoardLensButtonHit` | `LocalPlayingBoardLensButtons.GetHit` | 対局中 BOARD LENS。`internal` で `BoardLensButton?` を返す。 |

これらはすでに UI が実際の矩形・有効状態の一部を所有しています。呼び出し側の互換性が不要になった段階で、`GoScreenRenderer` のラッパーを削除できる候補です。

## 次の整理方針

1. 新しい画面では、ボタン・リンク・入力欄の `IsHit` を画面または文房具 UI が所有する。
2. `Game1` から直接呼ばれる既存 static API は、移行中は互換ラッパーとして残す。
3. 呼び出し元を画面クラスまたは専用の操作クラスへ移した後、そのラッパーと renderer 側の矩形を削除する。
4. 判定で状態を必要とする場合は、`GetStartPlayingButtonHit`、`GetHumanPlayerNameTextBoxHit`、CGOS 通知のように必要最小限の状態を引数に取る。

## `*Bounds` の棚卸（partial class を含む）

`GoScreenRenderer` の partial class を含めて機械的に再集計した結果、`Rectangle` を返す／保持する `*Bounds` 定義は **185 個**です。ここにはボタン以外に、パネル、テキスト欄、リスト行、ツールチップ、チャート領域も含まれます。したがって、すべてを `Button` に置換するのではなく、操作可能な領域は文房具 UI、表示領域は画面クラスの `Rectangle` プロパティとして移します。

| 現在の定義ファイル | 数 | 移管先 |
| --- | ---: | --- |
| `GoScreenRenderer.cs` | 27 | `Pages/LocalMatch/LocalMatchScreen`、`Shared/TournamentRules` |
| `Pages/BoardAndReview/GoScreenRenderer.BoardAndReview.cs` | 9 | `Pages/BoardAndReview/BoardAndReviewScreen` |
| `Pages/Cgos/GoScreenRenderer.Cgos.cs` | 67 | `Pages/Cgos/CgosScreen` |
| `Pages/GtpEngine/GoScreenRenderer.GtpEngine.cs` | 52 | `Pages/GtpEngine/GtpEngineScreen` |
| `Pages/MoveComments/GoScreenRenderer.MoveComments.cs` | 7 | `Pages/MoveComments/MoveCommentsScreen` |
| `Pages/MoveTrendChart/GoScreenRenderer.MoveTrendChart.cs` | 8 | `Pages/MoveTrendChart/MoveTrendChartScreen` |
| `Pages/ReviewChartPopup/GoScreenRenderer.ReviewChartPopup.cs` | 4 | `Pages/ReviewChartPopup/ReviewChartPopupScreen` |
| `Pages/Title/GoScreenRenderer.Title.cs` | 2 | `Pages/Title/TitleScreen` |
| `Shared/EntryProfiles/GoScreenRenderer.EntryProfiles.cs` | 7 | `Shared/EntryProfiles/EntryProfilesScreen` |
| `Shared/SelectEntry/GoScreenRenderer.SelectEntry.cs` | 2 | `Shared/SelectEntry/SelectEntryScreen` |

`Shared/TextAreaDialog`、`Pages/ApplicationSettings`、`Pages/CgosWatching`、`Pages/EditTournamentRule` の Bounds は renderer から削除済みです。旧集計では `MoveTrendChart` を5個としていましたが、再集計で8個へ補正しました。

### 移管済みの操作 Bounds

`Pages/LocalMatch/LocalMatchScreen` は次の 14 個の `Button.Bounds` と 1 個のカード領域を所有します。`GoScreenRenderer` の同名 private property は、移行中の描画互換のためこの画面オブジェクトを参照するだけです。

- `StartPlayingButtonBounds`、`ChangeAppProviderButtonBounds`、`AppProviderGameSettingsButtonBounds`
- `PonnukiProviderSeedAutoChangeBounds`、`PonnukiPlayer1SeedAutoChangeBounds`、`PonnukiPlayer2SeedAutoChangeBounds`
- `ImportSgfButtonBounds`、`SetupBackToTitleButtonBounds`、`ReturnToSetupButtonBounds`
- `ExportSgfButtonBounds`、`LocalGameOverReviewButtonBounds`
- `PassButtonBounds`、`ResignButtonBounds`、`CancelPlayingButtonBounds`
- `LocalUseButtonBounds` は `LocalUseCardBounds` として同画面が所有する（カードは現時点では `Button` 描画を使わないため）。

タイトル／ポン抜きの操作 Bounds はすでに `TitleScreen` と `PonnukiProviderSelectionScreen` が所有します。
