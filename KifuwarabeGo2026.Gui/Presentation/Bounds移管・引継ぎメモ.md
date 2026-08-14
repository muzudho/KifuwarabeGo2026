# GoScreenRenderer の Bounds 移管・引継ぎメモ

## 目的

`GoScreenRenderer` の partial class に残っている `*Bounds` とヒット判定を、対応する `Pages` または `Shared` 配下の独立クラスへ移す。

移管先の画面クラスは、操作可能な要素を文房具 UI の `Button`、`LinkUnderline`、既存の入力 UI として所有する。パネルやテキスト領域など操作を伴わないものは、その画面クラスの `Rectangle` プロパティとして所有する。

renderer は描画面の実装に限定し、移行期間だけ既存の `Get...Hit` API と private `...Bounds` を互換ラッパーとして残す。

詳細な棚卸は [GoScreenRenderer.ヒット判定棚卸.md](GoScreenRenderer.ヒット判定棚卸.md) を参照する。

## 現在地（2026-08-14）

- partial を含む `GoScreenRenderer` に `*Bounds` 定義が 250 個ある。
- タイトル画面の BACK、ポン抜きプロバイダー選択の NEXT／RECHECK／CHANGE は移管済み。
  - `TitleScreen` と `PonnukiProviderSelectionScreen` が `Button`／`LinkUnderline` を所有する。
  - `TitleRenderer` はそれらの `IsHit` を直接呼ぶ。
- ローカル対局の操作領域 15 個は移管を開始済み。
  - [LocalMatchScreen.cs](Pages/LocalMatch/LocalMatchScreen.cs) が 14 個の `Button` と `LocalUseCardBounds` を所有する。
  - `GoScreenRenderer` の既存 private `...Bounds` は、現時点では同クラスの `.Bounds` を参照する互換ラッパーである。
- `TextInputDialog`、`CgosMatchNotification`、`LinkUnderline`、`BoardLensButtonStrip` は、すでに UI 側でヒット判定の本体を所有している。

## 移管の共通手順

1. 対象の `GoScreenRenderer.*.cs` で、`*Bounds` の用途を「操作 UI」と「表示専用」に分ける。
2. 対応する `Pages/<画面名>` または `Shared/<機能名>` に `<画面名>Screen` を作る。既存の独立クラスがあればそこへ追加する。
3. 操作 UI は `Button`、`LinkUnderline`、入力コンポーネントを生成し、矩形をその `.Bounds` に持たせる。
4. renderer の描画を新しい UI の `Draw` または `.Bounds` 参照へ置換する。描画ロジックも独立できる場合は描画コールバックを渡す。
5. ヒット判定の呼び出し元（通常は `Game1`、一部は Application 層）を、独立クラスの `Button.IsHit`、`LinkUnderline.IsHit`、または画面固有の `Is...Hit` へ変更する。
6. `GoScreenRenderer` の公開／内部公開 `Get...Hit` と private `...Bounds` を削除する。外部公開 API を残す必要があるときだけ、独立クラスへ委譲する薄い互換ラッパーにする。
7. `rg` で古い名前の参照がないことを確認し、次を実行する。

```powershell
dotnet build KifuwarabeGo2026.Gui\KifuwarabeGo2026.Gui.Core.csproj --no-restore
```

## 推奨作業順

| 優先 | 対象 | Bounds 数 | 移管先 | 理由 |
| ---: | --- | ---: | --- | --- |
| 1 | コメント入力ダイアログ | 4 | `Shared/TextAreaDialog` | 独立したモーダルであり、影響範囲が小さい。 |
| 2 | ローカル対局 | 31 | `Pages/LocalMatch/LocalMatchScreen` | 15 個は所有済み。プレイヤー選択・カード・テキスト欄を続けて移せる。 |
| 3 | アプリ設定 | 10 | `Pages/ApplicationSettings/ApplicationSettingsScreen` | タブ・リンク・フォルダ選択を 1 画面へ集約できる。 |
| 4 | CGOS 観戦 | 3 | `Pages/CgosWatching/CgosWatchingScreen` | 小規模で、SGF 出力 UI の分離例に向く。 |
| 5 | 大会ルール編集 | 25 | `Pages/EditTournamentRule/TournamentRulesScreen` | 既存の `TournamentRulesSetting` と責務を確認してから移す。 |
| 6 | 盤編集・検討 | 35 | `Pages/BoardAndReview/BoardAndReviewScreen` | Board Lens など既存 UI を再利用できる。 |
| 7 | エントリープロファイル／選択 | 9 | `Shared/EntryProfiles`、`Shared/SelectEntry` | 複数画面から再利用するため Shared に置く。 |
| 8 | CGOS 接続 | 67 | `Pages/Cgos/CgosScreen` | 最大規模。接続一覧、管理パネル、編集パネルにさらに小分けする。 |
| 9 | GTP エンジン | 52 | `Pages/GtpEngine/GtpEngineScreen` | 選択、編集、GUI オプション、ランダム着手を小クラスに分割して移す。 |
| 10 | コメント・チャート・タイトル表示 | 18 | 各 Page の Screen | 表示専用 Bounds も多く、上記パターンを確立してから行う。 |

## 画面別の配置方針

- `Pages/LocalMatch/LocalMatchScreen`
  - 既存の 14 `Button` とカード矩形を維持する。
  - `PlayerKindButtonBounds`、人間名入力欄、盤サイズ、SGF 操作を追加する。
- `Shared/TextAreaDialog/TextAreaDialog`
  - `TextAreaDialogBounds`、`TextAreaTextBounds`、DISCARD／SAVE ボタンを所有する。
  - `Game1` は renderer の static API ではなく、このダイアログのボタンへ問い合わせる。
- `Pages/ApplicationSettings/ApplicationSettingsScreen`
  - BACK、設定タブ、フォルダ／ログのリンク下線を所有する。
- `Pages/CgosWatching/CgosWatchingScreen`
  - BACK、REVIEW、SGF OUTPUT と自動保存チェックを所有する。
- `Pages/BoardAndReview/BoardAndReviewScreen`
  - Board Editing、Variation Editing、Review の各操作列を小さな UI グループに分ける。
- `Pages/Cgos/CgosScreen`
  - `CgosConnectionScreen`、`CgosAdminPanel`、`CgosConnectionEditPanel` のように領域ごとへ分割する。
- `Pages/GtpEngine/GtpEngineScreen`
  - 選択ダイアログ、編集パネル、GUI オプション、ランダム着手ダイアログを別クラスにする。

## 注意点

- `Rectangle` の座標を移すだけでは完了ではない。ヒット判定と描画が同じ UI オブジェクトの `.Bounds` を参照していることを確認する。
- `Button.IsHit` は `IsEnabled` を考慮する。既存の `Contains` が無効状態でも反応していた場合は、呼び出し側の有効条件を明示的に見直す。
- Tab 移動、選択中の配色、ホバー表示を維持する。必要なら `Button.IsSelected` と `UpdatePointer` を利用する。
- 互換ラッパーを残す間も、座標の唯一の正本は画面クラスとする。renderer に新しい raw `Rectangle` を追加しない。
- 1 画面の移管ごとに、古い `Get...Hit`／`...Bounds` 名を `rg` で検索して残存参照を確認する。

## 完了条件

- `GoScreenRenderer.cs` とすべての `GoScreenRenderer.*.cs` に、画面固有の raw `*Bounds` と `Get...Hit` が残っていない。
- 各操作領域は対応する画面／共有 UI クラスが所有し、ヒット判定はその UI の API を使う。
- renderer は描画コールバックまたは描画面実装だけを提供する。
- `KifuwarabeGo2026.Gui.Core` のビルドが警告・エラーなしで成功する。
