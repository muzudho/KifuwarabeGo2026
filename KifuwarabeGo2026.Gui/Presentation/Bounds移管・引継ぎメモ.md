# GoScreenRenderer の Bounds 移管・引継ぎメモ

## 目的

`GoScreenRenderer` の partial class に残っている `*Bounds` とヒット判定を、対応する `Pages` または `Shared` 配下の独立クラスへ移す。

移管先の画面クラスは、操作可能な要素を文房具 UI の `Button`、`LinkUnderline`、既存の入力 UI として所有する。パネルやテキスト領域など操作を伴わないものは、その画面クラスの `Rectangle` プロパティとして所有する。

renderer は描画面の実装に限定し、移行期間だけ既存の `Get...Hit` API と private `...Bounds` を互換ラッパーとして残す。

詳細な棚卸は [GoScreenRenderer.ヒット判定棚卸.md](GoScreenRenderer.ヒット判定棚卸.md) を参照する。

## 構造化の原則

- 原則として、アプリケーション上の1画面に対して1つの `<画面名>Screen` クラスを置く。
- 画面固有のボタン、リンク、入力欄、表示領域、選択・ホバーなどの内部状態は、その画面クラスまたは画面配下の小さな部品へ置く。
- `GoScreenRenderer` は移行中の描画バックエンドとし、画面固有の機能や状態を段階的に減らす。
- 実行速度やメモリ使用量よりも、ソースコードがアプリケーションの画面構造に沿い、調査しやすいことを優先する。ただし、毎フレームの不要な大量生成は避け、画面オブジェクトを再利用する。

## 進捗を知りたいとき

まずこの節を読む。この節を進捗の正本とし、移管を一まとまり終えるたびに更新する。

### 今わかっている終わったもの

- [x] コメント入力ダイアログ
  - `Shared/TextAreaDialog/TextAreaDialog` がダイアログ、本文、DISCARD、SAVE & CLOSE の4領域を所有する。
  - `Game1` はボタンの `IsHit` を直接呼ぶ。
  - renderer の旧4 Bounds と旧ヒットAPIは削除済み。
- [x] アプリ設定
  - `Pages/ApplicationSettings/ApplicationSettingsScreen` が SETTINGS、UPDATE、BACK、3タブ、5設定リンク、5ログ行を所有する。
  - `Game1` と `TitleRenderer` は画面オブジェクトの `Button.IsHit`／`LinkUnderline.IsHit` を直接呼ぶ。
  - `GoScreenRenderer.ApplicationSettings.cs` の旧10 Bounds と旧ヒットAPIは削除済み。
- [x] CGOS観戦
  - `Pages/CgosWatching/CgosWatchingScreen` が LEAVE VIEW、KIFU REVIEW、SGF OUTPUT の3ボタンを所有する。
  - rendererの旧3 Boundsと旧ヒットAPIは削除済み。`Game1` と描画側は同じボタンを参照する。
- [ ] 大会ルール編集（進行中）
  - `TournamentRulesScreen` が選択ダイアログと削除確認の18領域を所有する。
  - `TournamentRulesSetting` は画面のボタンとリスト行判定を直接参照し、対応するrendererヒットAPIは削除済み。
  - 追加・編集パネルの7 Boundsと入力状態の移管が残る。
- [x] タイトル画面の BACK、ポン抜きプロバイダー選択の NEXT／RECHECK／CHANGE
  - `TitleScreen` と `PonnukiProviderSelectionScreen` が所有する。
- [x] ローカル対局のヒット判定呼び出し側
  - 14ボタンは `LocalMatchScreen`、プレイヤー種別と人間名入力は `PlayerKindSelectionRow` が所有する。
  - `Game1`／`PlayingScene` から renderer の旧ヒットAPIを呼ぶ経路は削除済み。
  - 通常対局とポン抜きで異なる人間名入力座標を明示済み。
- [x] 既存UIへ委譲済みの本体
  - `TextInputDialog`、`CgosMatchNotification`、`LinkUnderline`、`BoardLensButtonStrip` はUI側がヒット判定の本体を所有する。

### 今わかっている残っているもの

- [ ] ローカル対局を完全移管する。
  - 14ボタンの renderer private `...Bounds` は描画互換ラッパーとして残る。
  - カード、プレイヤー選択、盤サイズ、SGF表示領域など、`GoScreenRenderer.cs` の残りを用途別に移す。
- [ ] 既存UIへ委譲するだけの renderer 互換ヒットAPIを呼び出し側から外す。
  - `TextInputDialog`、`CgosMatchNotification`、大会ルール設定リンクなどが対象。
- [ ] 以下の画面別 renderer Bounds を独立クラスへ移す。

| 残作業 | 現在の Bounds 定義数 | 主な移管先 |
| --- | ---: | --- |
| `GoScreenRenderer.cs` | 27 | LocalMatch、Shared、Title |
| CGOS 接続 | 67 | `Pages/Cgos/CgosScreen` |
| GTP エンジン | 52 | `Pages/GtpEngine/GtpEngineScreen` |
| 盤編集・検討 | 35 | `Pages/BoardAndReview/BoardAndReviewScreen` |
| 大会ルール編集 | 7 | `Pages/EditTournamentRule/TournamentRulesScreen` |
| 手の傾向チャート | 8 | `Pages/MoveTrendChart/MoveTrendChartScreen` |
| コメント表示 | 7 | `Pages/MoveComments/MoveCommentsScreen` |
| エントリープロファイル | 7 | `Shared/EntryProfiles/EntryProfilesScreen` |
| 検討チャートポップアップ | 4 | `Pages/ReviewChartPopup/ReviewChartPopupScreen` |
| エントリー選択 | 2 | `Shared/SelectEntry/SelectEntryScreen` |
| タイトル表示 | 2 | `Pages/Title/TitleScreen` |
| **合計** | **218** | |

この218個は、2026-08-14に現在のコードを機械的に再集計した値である。旧表の `MoveTrendChart` は5個ではなく8個だったため補正した。ボタンだけでなく表示専用領域も含む。

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

| 優先 | 状態 | 対象 | Bounds 数 | 移管先 | 理由 |
| ---: | --- | --- | ---: | --- | --- |
| 1 | 完了 | コメント入力ダイアログ | 4 | `Shared/TextAreaDialog` | 独立したモーダルであり、影響範囲が小さい。 |
| 2 | 進行中 | ローカル対局 | 31 | `Pages/LocalMatch/LocalMatchScreen` | 操作UIの所有とヒット判定直結は済み。描画互換ラッパーと表示領域が残る。 |
| 3 | 完了 | アプリ設定 | 10 | `Pages/ApplicationSettings/ApplicationSettingsScreen` | タブ・リンク・フォルダ選択を1画面へ集約済み。 |
| 4 | 完了 | CGOS 観戦 | 3 | `Pages/CgosWatching/CgosWatchingScreen` | 3ボタンとヒット判定を集約済み。 |
| 5 | 進行中 | 大会ルール編集 | 7 | `Pages/EditTournamentRule/TournamentRulesScreen` | 選択・削除確認は移管済み。追加・編集パネルが残る。 |
| 6 | 未着手 | 盤編集・検討 | 35 | `Pages/BoardAndReview/BoardAndReviewScreen` | Board Lens など既存 UI を再利用できる。 |
| 7 | 未着手 | エントリープロファイル／選択 | 9 | `Shared/EntryProfiles`、`Shared/SelectEntry` | 複数画面から再利用するため Shared に置く。 |
| 8 | 未着手 | CGOS 接続 | 67 | `Pages/Cgos/CgosScreen` | 最大規模。接続一覧、管理パネル、編集パネルにさらに小分けする。 |
| 9 | 未着手 | GTP エンジン | 52 | `Pages/GtpEngine/GtpEngineScreen` | 選択、編集、GUI オプション、ランダム着手を小クラスに分割して移す。 |
| 10 | 未着手 | コメント・チャート・タイトル表示 | 18 | 各 Page の Screen | 表示専用 Bounds も多く、上記パターンを確立してから行う。 |

## 画面別の配置方針

- `Pages/LocalMatch/LocalMatchScreen`
  - 14 `Button`、カード矩形、`PlayerKindSelectionRow` を維持する。
  - rendererの描画互換ラッパーと残る表示領域を続けて移す。
- `Shared/TextAreaDialog/TextAreaDialog`
  - 移管完了。ダイアログ、本文、DISCARD／SAVEボタンを所有する。
- `Pages/ApplicationSettings/ApplicationSettingsScreen`
  - 移管完了。タイトル右下操作、BACK、設定タブ、フォルダ／ログのリンク下線を所有する。
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
