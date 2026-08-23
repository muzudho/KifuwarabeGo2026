# GoScreenRenderer 構造化・引継ぎ

最終更新: 2026-08-14

## 進捗を知りたいとき

今後はこの文書を進捗の正本とする。完了した項目は
`Docs/開発/完了/GoScreenRenderer構造化・完了記録.md` へ移す。

## 目標

- アプリケーション上の1画面に対して1つの画面クラスを置く。
- UIは描画関数だけでなく、位置、サイズ、表示値、選択・ホバーなどの内部状態を所有する。
- 親画面または親コンポーネントが、子UIインスタンスをプロパティとして所有する。
- 操作可能な領域は `Button`、`LinkUnderline`、入力UIなどが所有する。
- 表示専用領域は対応する画面またはコンポーネントが所有する。
- `GoScreenRenderer` はSpriteBatch、フォント、テクスチャ、基本描画を提供するホストへ寄せる。
- 文房具UIとrendererの境界には `StationeryDrawingContext` を使う。
- rendererへ新しい画面固有raw `Rectangle` や専用描画インターフェースを増やさない。

## 今わかっている残作業

### 1. ローカル対局の完全移管

- 14ボタンの正本とヒット判定は `LocalMatchScreen` へ移管済み。
- rendererに残る描画互換Boundsを外す。
- カード、プレイヤー選択、盤サイズ、SGF表示領域など、`GoScreenRenderer.cs` の残りを用途別コンポーネントへ移す。

### 2. rendererの互換ヒットAPIを外す

UIが判定本体を所有している次の互換ラッパーを、呼び出し側の移管後に削除する。

- 大会ルール設定ファイルの `LinkUnderline`
- `TextInputDialog` のCANCEL、OK、DEFAULT、入力欄
- `CgosMatchNotification` の即時観戦、後で観戦、保留、保留バナー
- 対局中 `BoardLensButtonStrip`

### 3. 大規模画面のBounds・状態・描画手順を移す

2026-08-14の旧集計値。作業開始時に再集計すること。

| 対象 | 旧Bounds数 | 主な移管先 |
| --- | ---: | --- |
| `GoScreenRenderer.cs` | 24 | `Pages/LocalMatch`、`Shared` |
| CGOS接続 | 67 | `Pages/Cgos/CgosScreen` 配下の小パネル |
| GTPエンジン | 52 | `Pages/GtpEngine/GtpEngineScreen` 配下の小パネル |
| 手の傾向チャート | 8 | `Pages/MoveTrendChart/MoveTrendChartScreen` |
| 合計 | 151 | |

CGOSは接続一覧、管理パネル、接続編集へ分ける。GTPエンジンは選択、編集、GUIオプション、ランダム着手へ分ける。

### 4. 残るHostと共通部品

- `StickyNote` を既存画面が直接所有する形へ移し、旧Hostを不要にする。
- `CatalogOrder` の移行中部分を完了させる。
- コメント編集の本文描画、IME、罫線、ボタンを親画面が所有するコンポーネントへ集約し、rendererのダイアログHostを薄くする。
- `PopupNumberUnderline`、`TextInputDialog`、`MessageDialog` の呼び出し側も、共通描画境界だけを渡す形へ揃える。
- `Board`、`MoveTrendChart`、`GtpEngine`、`Cgos` の巨大renderer partialを、画面内パネル単位で分割する。
- Host partialは描画境界を接続する最小限のコードだけにする。

### 5. BoardLensの独立化

以下はページではなく盤面への重ね描画機能として、描画contextを受け取る独立コンポーネントへ移す。

- `BoardLensButtonStripRenderer`
- `BoardLensDispatcher`
- `BoardLensMetricDrawing`
- `ChippedSingleEyeGlassSeedLens`
- `AdjacentOpponentAreaLens`
- `BoundaryCountLens`
- `RenAreaLens`
- `NobiLens`
- `StrongLens`
- `RenIndexLens`
- `RenNetworkBasicLens`
- `RenNetworkEyeModeLens`
- `RenRectangleLens`

## 作業手順

1. 対象rendererでBoundsを操作UIと表示専用領域に分ける。
2. 対応する画面または部品クラスを作り、子UIをプロパティとして所有させる。
3. UI自身へ位置、サイズ、表示値、内部状態を持たせる。
4. 描画とヒット判定が同じインスタンスの `Bounds` を参照するよう接続する。
5. 呼び出し元をUIの `IsHit` または画面固有APIへ移す。
6. rendererの旧Bounds、旧ヒットAPI、不要なcallback recordを削除する。
7. 古い名前を `rg` で検索する。
8. Core、Windows版、必要なSmokeプロジェクトをビルドする。

## 1件ごとの完了条件

- ページ／部品本体が `GoScreenRenderer` を参照しない。
- レイアウト、構成部品、当たり判定、内部状態を独立クラスが所有する。
- 親コンポーネントが子UIインスタンスをプロパティとして所有する。
- renderer依存は `StationeryDrawingContext` などの共通描画境界だけで接続する。
- 古いBoundsと互換APIに残存参照がない。
- ビルドが警告・エラーなしで成功する。
