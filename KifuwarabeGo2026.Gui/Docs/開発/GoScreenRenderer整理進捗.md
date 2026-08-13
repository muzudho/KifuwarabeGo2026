# GoScreenRenderer 整理進捗

最終棚卸し: 2026-08-13

## 目的

`Presentation/GoScreenRenderer.cs` は、MonoGame の低水準描画を提供する共通レンダラーに近づける。
画面固有のレイアウト、ボタン判定、画面状態に依存する描画は、次のどちらかへ置く。

- `Presentation/Pages/<画面名>/`: 特定画面だけが使う描画、レイアウト、ヒットテスト。
- `Presentation/Shared/<部品名>/`: 複数画面で再利用する UI 部品、ダイアログ、レイアウト。

partial によるファイル分割は完了扱いにしない。画面固有の部品は callback / drawing callbacks または小さな描画インターフェースを受ける独立クラスにし、`GoScreenRenderer` の private メンバーへ直接依存させない。

## 現在地

- `GoScreenRenderer.cs`: 2,195 行（2026-08-13 の棚卸し時点）。
- 大会ルール編集の KOMI 欄だけは `TournamentRuleKomiField` として独立済み。
- 大会ルールの選択・編集本体は `Pages/EditTournamentRule/GoScreenRenderer.EditTournamentRule.cs` にあるが、これは **partial 分割済み（未完了）** である。
- 本体には、ローカル対局、大会ルール編集の補助、盤面レンズ、モーダル描画がまだ混在している。
- `Pages/` と `Shared/` 配下の partial は、依存を断てるまで「partial 分割済み」と記録し、独立化済みとは扱わない。

## 完了済み

| 状態 | 対象 | 配置 | 内容 |
| --- | --- | --- | --- |
| partial 分割済み（未完了） | 大会ルール選択・編集画面 | `Pages/EditTournamentRule/GoScreenRenderer.EditTournamentRule.cs` | ファイル位置は移したが、`GoScreenRenderer` の private メソッドへ直接依存している。独立クラス化が必要。 |
| 完了 | 大会ルールのコミ欄 | `Pages/EditTournamentRule/TournamentRuleKomiField.cs` | `SinglelineTextUnderline` を所有する独立クラス。描画は `TournamentRuleKomiFieldDrawingCallbacks` だけを受ける。 |
| partial 分割済み（未完了） | 大会ルールのキャレット補助 | `Pages/EditTournamentRule/TournamentRuleRenderer.cs` | 現在は `GoScreenRenderer` のキャレット計算を呼ぶ薄い補助。専用の text-measure callback に置換する。 |
| 部品独立済み／呼び出し整理は残る | カタログ並び替え | `Shared/CatalogOrder/` | フレーム、カード、ページ送り、レイアウトは独立済み。`GoScreenRenderer` 側の呼び出しは残る。 |
| 部品独立済み／呼び出し整理は残る | エントリープロフィール編集 | `Shared/EditEntryProfile/` と `Shared/EntryProfiles/` | 編集パネル部品は分離済み。画面側の入力・遷移は別途整理対象。 |
| 完了 | 下線・入力用 UI | `StationeryUI/Controls/` | `LinkUnderline`、`SinglelineTextUnderline`、`PopupNumberUnderline`、`TextInputDialog` などを独立部品化。 |
| 完了 | 画面遷移・一時表示 | `Pages/ScreenTransition/`、`Pages/ScreenshotEffect/`、`Pages/ReviewUnsavedChangesConfirmation/` | 専用コンポーネントとして分離。 |
| partial 分割済み（未完了） | タイトル画面 | `Shared/Title/`、`Shared/TitleBackground/` | ファイル分割は済んでいる。`GoScreenRenderer` への private 依存を callbacks 化する余地がある。 |

## 残作業

### 優先度 A: 本体に残る大会ルール編集の補助を移す

移設先は `Pages/EditTournamentRule/`。

- `GetBoardSizeButtonHit`、`GetRuleKindButtonHit`
- `GetMainTimeStepButtonHit`、`GetMoveLimitStepButtonHit`
- `GetTournamentRulesMainTimeTextBoxHit`、`GetTournamentRulesMoveLimitTextBoxHit`
- `GetTournamentRulesNumericCaretIndex`
- `DrawDisplayNameTextBox`、`DrawFilePathSelector`
- `DrawBoardSizeButtons`、`DrawRuleKindButtons`
- `DrawTournamentRulesTimeStrip`、`DrawTournamentRulesMoveLimitStrip`
- `DrawTournamentRulesNumericTextBox` と大会ルール専用の角丸ボタン・タブ補助
- `AddPanelControlX`、ルール種別・時間・手数入力欄の各 `Rectangle`

理由: これらは大会ルール編集画面以外では使わず、KOMI 欄と同じ画面責務である。

### 優先度 A: ローカル対局のサイドパネルを画面別に分離する

候補の移設先:

- `Pages/LocalSetup/`: `DrawSetupSidePanel`、プレイヤー種別・人間名・エンジン選択の行、設定画面のボタン判定。
- `Pages/LocalPlaying/`: `DrawPlayingSidePanel`、パス・投了・中止、局面操作、局面情報。
- `Pages/LocalGameOver/`: `DrawGameOverSidePanel`、棋譜出力、レビュー開始、結果表示。
- `Pages/PonnukiSetup/`: プロバイダー変更、ゲーム設定、乱数シード自動更新、ポン抜き用のプレイヤー選択。

本体に残る代表例:

- `DrawSidePanel`、`DrawSetupSidePanel`、`DrawPlayingSidePanel`、`DrawGameOverSidePanel`
- `GetLocalUseButtonHit`、`GetStartPlayingButtonHit`、`GetPassButtonHit`、`GetResignButtonHit` など
- `GetPonnukiRandomSeedAutoChangeHit`、`GetAppProviderGameSettingsButtonHit` など
- プレイヤー選択の Y 座標、各画面のボタン `Rectangle`

理由: ローカル対局とポン抜きの画面固有の配置が、共通レンダラーに残っている。

### 優先度 B: モーダルの呼び出しと実装を分ける

実装本体は多くが既に `Pages/` または `StationeryUI/Controls/` にある。次は `GoScreenRenderer` のフィールド公開と呼び出しを、画面側の専用ビュー/コントローラーへ寄せる。

| 対象 | 現在 | 次の整理 |
| --- | --- | --- |
| `PopupNumberUnderline` | 共通部品だが `GoScreenRenderer` が公開インスタンスを所有 | 呼び出し側が drawing callbacks を渡す形を検討。 |
| `TextInputDialog` / コメント編集 | 本体に描画、ヒットテスト、レイアウトが残る | `Pages/CommentEditor/` を作り、ダイアログ操作を集約。 |
| `MessageDialog` | 共通部品 | 本体は描画サーフェス提供だけに縮小。 |
| `InitialPositionConcierge` | 専用部品 | `Pages/InitialPositionConcierge/` の画面側へ呼び出し責務を移す。 |

### 優先度 B: 盤面レンズを専用レンダラーにする

候補の移設先: `Pages/BoardLens/`（または既存 `Presentation/BoardLens/` を画面単位に整理）。

- `DrawRenNumbers` から `DrawRenMetricUnit` までの Ren 解析描画。
- `DrawNobiLens`、`DrawBoardRenAnalysis`。
- `LocalPlayingBoardLensButtons` とローカル対局用レンズボタン。
- 公開されている `GetBoardPoint`、`DrawBoardLensLine` などは `IGoScreenDrawingSurface` のような小さな描画インターフェースにまとめる。

理由: 解析表示は共通描画プリミティブではなく、盤面レンズという一画面機能である。

### 優先度 C: 画面横断の見直し

- `DrawBackground` は全画面背景として残すか、タイトル/対局背景へ分けるかを決める。
- `DrawBoard` は複数画面で使うため `Shared/Board/` の共通部品として維持する。
- `DrawCommandButton`、`DrawDataRowFrame`、文字・線・矩形・円の描画は共通プリミティブとして残す。
- `DrawPathTooltip` は複数画面で使うなら `Shared/PathTooltip/` を作り、callback で独立する。

## GoScreenRenderer に残す共通責務

最終形で本体に残す候補は次に限る。

- MonoGame リソースの所有: `GraphicsDevice`、`SpriteBatch`、フォント、共通テクスチャ。
- 描画サイクル: `SpriteBatch.Begin/End`、仮想画面座標への変換。
- 基本描画: 矩形、線、円、文字、選択範囲、キャレット、テクスチャ作成。
- 汎用レイアウト支援: 文字のフィット、中央寄せ、共通ボタン、共通データ行。
- 複数画面で再利用する `Shared/` 部品へ渡す drawing callbacks。

画面固有の `Rectangle`、ボタン判定、画面文言、画面状態分岐は残さない。

## 実施順

1. 大会ルール編集の partial を `EditTournamentRulePage` 等の独立クラスへ置換する。まず必要な描画・計測・入力欄 callback を定義する。
2. ローカル対局を Setup / Playing / GameOver / PonnukiSetup へ分ける。
3. コメント編集と盤面レンズを専用ページ化する。
4. 各ページから `GoScreenRenderer` への依存を drawing callbacks または小さな描画インターフェースに置換する。
5. 本体に画面固有メソッド・画面固有 `Rectangle` が残っていないことを棚卸しして、この表を更新する。

## 更新ルール

- ファイルを `Pages/` または `Shared/` へ移した時点で、該当行を「完了済み」へ移す。
- ファイル移動だけで `GoScreenRenderer` private メンバーへ直接依存している場合は、「partial 分離済み」と記載し、独立化完了とはしない。
- ビルド成功を確認してから進捗を更新する。
