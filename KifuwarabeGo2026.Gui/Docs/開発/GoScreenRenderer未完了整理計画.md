# GoScreenRenderer 未完了整理計画

最終棚卸し: 2026-08-14

## 目的

`GoScreenRenderer` は MonoGame の描画サーフェスと共通プリミティブに限定する。画面固有のレイアウト、ヒットテスト、画面状態分岐は `Pages/`、複数画面で再利用する部品は `Shared/` または `StationeryUI/` へ置く。

partial によるファイル分割だけは完了扱いにしない。各ページ・部品は drawing callbacks または小さな描画インターフェースだけへ依存し、`GoScreenRenderer` の private メンバーを直接参照しない。

完了済みの文房具UIと大会ルール／アプリケーション設定の整理は、[完了記録](./完了/20260814_文房具UIとGoScreenRenderer整理_完了項目.md) を参照する。

## 残作業

### 優先度 A: 大会ルール選択・編集ページの独立化

- `Pages/EditTournamentRule/GoScreenRenderer.EditTournamentRule.cs` の partial 依存を `EditTournamentRulePage` などの独立クラスへ置換する。
- 大会ルール選択、表示名、ファイルパス、盤サイズ、ルール種別の画面固有レイアウトとヒットテストをページ側へ移す。
- 描画・計測・テキスト入力は callbacks で受け取り、ページから `GoScreenRenderer` の private メンバーを参照しない。

### 優先度 A: ローカル対局画面を分離する

- `Pages/LocalSetup/`: セットアップのサイドパネル、プレイヤー選択、各ボタンの配置とヒットテスト。
- `Pages/LocalPlaying/`: パス、投了、中止、局面情報、対局中のサイドパネル。
- `Pages/LocalGameOver/`: 結果、棋譜出力、レビュー開始。
- `Pages/PonnukiSetup/`: プロバイダー変更、ゲーム設定、乱数シード、ポン抜き用プレイヤー選択。

### 優先度 B: モーダルと盤面レンズの呼び出し責務を分離する

- コメント編集を `Pages/CommentEditor/` へ集約する。
- `PopupNumberUnderline`、`TextInputDialog`、`MessageDialog` は、呼び出し側が drawing callbacks を渡す形へ近づける。
- 連・ノビ・局面解析などの盤面レンズ描画を `Pages/BoardLens/` または既存の専用領域へ移す。
- 盤面レンズが必要とする描画は `IGoScreenDrawingSurface` のような小さなインターフェースに限定する。

### 優先度 C: 画面横断の共通化を確認する

- `DrawBoard`、背景、文字・線・矩形・円、共通ボタンなどは共通描画として残す。
- `DrawPathTooltip` は複数画面で利用する場合だけ `Shared/PathTooltip/` へ独立させる。
- 画面固有の `Rectangle`、文言、ヒットテストが本体に残っていないことを最終確認する。

## 完了条件

- `GoScreenRenderer` が MonoGame リソース、描画サイクル、基本描画、共通レイアウト支援だけを所有する。
- 画面固有の `Rectangle`、文言、状態分岐、ボタン判定が `Pages/` 側へ移っている。
- 各ページと共有部品が callbacks または小さな描画インターフェースを介して描画する。
- ビルド成功後に、この文書の該当項目を完了記録へ移す。
