# GoScreenRenderer のヒット判定棚卸

対象は `Presentation/GoScreenRenderer.cs` に**直接定義されている**、マウスの仮想座標 `Point` に対するヒット判定です。`GoScreenRenderer.*.cs` にある partial class 側の判定は含めません。

2026-08-14 時点で、公開・内部公開の判定は 34 個、これらが共用する private helper は 1 個です。呼び出し元は主に `Game1` です。

## 画面に残っている矩形ベースの判定

| メソッド | 戻り値 | 対象 | 現在の所有・メモ |
| --- | --- | --- | --- |
| `GetLocalUseButtonHit` | `bool` | ローカル対局の利用カード | `LocalUseButtonBounds`。画面コンポーネント未分離。 |
| `GetImportSgfButtonHit` | `bool` | SGF 読込 | `ImportSgfButtonBounds`。 |
| `GetStartPlayingButtonHit` | `bool` | 対局開始 | 終局中は常に `false`。 |
| `GetChangeAppProviderButtonHit` | `bool` | アプリ提供エンジン変更 | ローカルアプリ中断パネル。 |
| `GetAppProviderGameSettingsButtonHit` | `bool` | 提供エンジンのゲーム設定 | ローカルアプリ中断パネル。 |
| `GetPonnukiRandomSeedAutoChangeHit` | `PonnukiRandomSeedRole?` | Provider / Black / White の SEED AUTO | 3 矩形を順に調べ、該当ロールを返す。 |
| `GetReturnToSetupButtonHit` | `bool` | 設定へ戻る | `ReturnToSetupButtonBounds`。 |
| `GetExportSgfButtonHit` | `bool` | SGF 出力 | `ExportSgfButtonBounds`。 |
| `GetSgfAutoSaveCheckHit` | `bool` | SGF 自動保存チェック | `ExportSgfButtonBounds` 全体を使う。描画側も同じ bounds をチェックボックスに渡している。 |
| `GetLocalGameOverReviewButtonHit` | `bool` | 終局後の検討 | `LocalGameOverReviewButtonBounds`。 |
| `GetSetupBackToTitleButtonHit` | `bool` | タイトルへ戻る | `SetupBackToTitleButtonBounds`。 |
| `GetBlackPlayerKindButtonHit` | `GoPlayerKind?` | 黒番プレイヤー種別 | private helper を使う。 |
| `GetWhitePlayerKindButtonHit` | `GoPlayerKind?` | 白番プレイヤー種別 | private helper を使う。 |
| `GetPonnukiBlackPlayerKindButtonHit` | `GoPlayerKind?` | ポン抜き黒番プレイヤー種別 | private helper を使う。 |
| `GetPonnukiWhitePlayerKindButtonHit` | `GoPlayerKind?` | ポン抜き白番プレイヤー種別 | private helper を使う。 |
| `GetHumanPlayerNameTextBoxHit` | `GoStone?` | 人間プレイヤー名 | 人間に設定された側だけを判定し、石色を返す。 |
| `GetPassButtonHit` | `bool` | パス | `PassButtonBounds`。 |
| `GetResignButtonHit` | `bool` | 投了 | `ResignButtonBounds`。 |
| `GetCancelPlayingButtonHit` | `bool` | エンジン準備中の取消 | `CancelPlayingButtonBounds`。 |
| `GetTextAreaDialogCancelButtonHit` | `bool` | コメント入力ダイアログの取消 | `TextAreaDiscardButtonBounds`。 |
| `GetTextAreaDialogApplyButtonHit` | `bool` | コメント入力ダイアログの適用 | `TextAreaApplyButtonBounds`。 |

private helper `GetPlayerKindButtonHit(Point, int)` は、各プレイヤー種別行の 3 種類の選択肢を調べて `GoPlayerKind?` を返します。公開 API ではありません。

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

`GoScreenRenderer` の partial class を含めて機械的に調べた結果、`Rectangle` を返す／保持する `*Bounds` 定義は **250 個**です。ここにはボタン以外に、パネル、テキスト欄、リスト行、ツールチップ、チャート領域も含まれます。したがって、すべてを `Button` に置換するのではなく、操作可能な領域は文房具 UI、表示領域は画面クラスの `Rectangle` プロパティとして移します。

| 現在の定義ファイル | 数 | 移管先 |
| --- | ---: | --- |
| `GoScreenRenderer.cs` | 31 | `Pages/LocalMatch/LocalMatchScreen`、`Shared/TextAreaDialog`、`Shared/TournamentRules` |
| `Pages/ApplicationSettings/GoScreenRenderer.ApplicationSettings.cs` | 10 | `Pages/ApplicationSettings/ApplicationSettingsScreen` |
| `Pages/BoardAndReview/GoScreenRenderer.BoardAndReview.cs` | 35 | `Pages/BoardAndReview/BoardAndReviewScreen` |
| `Pages/Cgos/GoScreenRenderer.Cgos.cs` | 67 | `Pages/Cgos/CgosScreen` |
| `Pages/CgosWatching/GoScreenRenderer.CgosWatching.cs` | 3 | `Pages/CgosWatching/CgosWatchingScreen` |
| `Pages/EditTournamentRule/GoScreenRenderer.EditTournamentRule.cs` | 25 | `Pages/EditTournamentRule/TournamentRulesScreen` |
| `Pages/GtpEngine/GoScreenRenderer.GtpEngine.cs` | 52 | `Pages/GtpEngine/GtpEngineScreen` |
| `Pages/MoveComments/GoScreenRenderer.MoveComments.cs` | 7 | `Pages/MoveComments/MoveCommentsScreen` |
| `Pages/MoveTrendChart/GoScreenRenderer.MoveTrendChart.cs` | 5 | `Pages/MoveTrendChart/MoveTrendChartScreen` |
| `Pages/ReviewChartPopup/GoScreenRenderer.ReviewChartPopup.cs` | 4 | `Pages/ReviewChartPopup/ReviewChartPopupScreen` |
| `Pages/Title/GoScreenRenderer.Title.cs` | 2 | `Pages/Title/TitleScreen` |
| `Shared/EntryProfiles/GoScreenRenderer.EntryProfiles.cs` | 7 | `Shared/EntryProfiles/EntryProfilesScreen` |
| `Shared/SelectEntry/GoScreenRenderer.SelectEntry.cs` | 2 | `Shared/SelectEntry/SelectEntryScreen` |

### 移管済みの操作 Bounds

`Pages/LocalMatch/LocalMatchScreen` は次の 14 個の `Button.Bounds` と 1 個のカード領域を所有します。`GoScreenRenderer` の同名 private property は、移行中の描画互換のためこの画面オブジェクトを参照するだけです。

- `StartPlayingButtonBounds`、`ChangeAppProviderButtonBounds`、`AppProviderGameSettingsButtonBounds`
- `PonnukiProviderSeedAutoChangeBounds`、`PonnukiPlayer1SeedAutoChangeBounds`、`PonnukiPlayer2SeedAutoChangeBounds`
- `ImportSgfButtonBounds`、`SetupBackToTitleButtonBounds`、`ReturnToSetupButtonBounds`
- `ExportSgfButtonBounds`、`LocalGameOverReviewButtonBounds`
- `PassButtonBounds`、`ResignButtonBounds`、`CancelPlayingButtonBounds`
- `LocalUseButtonBounds` は `LocalUseCardBounds` として同画面が所有する（カードは現時点では `Button` 描画を使わないため）。

タイトル／ポン抜きの操作 Bounds はすでに `TitleScreen` と `PonnukiProviderSelectionScreen` が所有します。
