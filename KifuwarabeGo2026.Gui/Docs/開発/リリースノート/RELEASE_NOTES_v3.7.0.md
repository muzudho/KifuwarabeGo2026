# Kifuwarabe Go 2026 v3.7.0

Board Lensの観察機能を拡張し、その連面積分析をポン抜きプレイヤーの退避判断にも共用したリリースです。画面の分類とラベルも整理し、ポン抜きの開始画面では日本語のエンジン名を正しく表示します。

## アプリの入口を整理

- タイトル画面の分類を`FORMAL APPS`と`CASUAL APPS`へ変更しました。
- 外部のアプリ・サーバーとプロトコルを合わせるアプリは`FORMAL APPS`、きふわらべがプロトコルを先導するアプリは`CASUAL APPS`へ置きます。
- `Local Match`と`Online Match (CGOS)`のラベル、パンくず、フォルダー、名前空間を対応させました。
- SGF関連の操作を`KIFU INPUT (SGF)`、`KIFU CLEAR (SGF)`、`KIFU OUTPUT (SGF)`へ統一しました。

## BOARD LENS

- `L`で系統を切り替え、`J`で前のレンズ、`K`で次のレンズ、`1`でOFFにします。
- 棋譜レビューと盤編集の両方で、系統切替、戻る、進む、OFFのボタンを使えます。
- `Strong Lens`は、自連面積から隣接する全相手連面積を引いた値を表示します。
- `Nobi Lens`は、自連の境界空点にノビ候補を示します。自分の目とコウは候補から除外します。
- `Glasses System`に`Chipped Single Eye Glass Seed Lens`を追加しました。盤外を含む3x3パターンを全8対称で照合し、黒眼、白眼、両者に共通する眼候補地を区別して表示します。
- 計測値は足先マーカーの後に描画し、隠れにくくしました。

## ポン抜きプレイヤー

- 連面積を調べる`StrongAnalyzer`をSharedへ追加し、GUIのStrong LensとEngineで共用します。
- `ContactPriority`を`EvacuationNobiPriority`へ改名しました。
- 着手後に相手連へ接触し、自連面積と隣接する全相手連面積が等しいときだけ、ノビて逃げる候補を優先します。
- ポン抜きのアルゴリズム文書をEngineのソースコードと同じフォルダーへ配置しました。

## 表示と読みやすさ

- ポン抜きの［アプリ提供エンジン］名を、日本語対応の動的テキスト描画で表示します。
- 最小文字サイズとして`MinimumTextScale`を定義し、`SELECTED ENGINE`とキーボード操作案内をノートPCでも読みやすくしました。

## 配布物

- GUI版: `KifuwarabeGo2026.Gui-v3.7.0-win-x64.zip`
- Engine版: `KifuwarabeGo2026.Engine-v3.7.0-win-x64.zip`

GUIとEngineの両方を使う場合は、2つともダウンロードしてください。

## SHA-256

- GUI版: `95B2AA16417AF69E754E87E74FD058793123FE1B28B790C0D71AAD5DB5DF7A43`
- Engine版: `8985F4B3383E5999960C0080E5A22D934871D39C8B24982E747BF7C7FD3C59EE`

## テスト状況

- ソリューション全体のReleaseビルド: 警告0・エラー0
- 移植性スモーク: PASS
- Windowsスモーク: PASS
- GUI版・Engine版のWindows x64 publish
- 同梱CGOS通信コンポーネントの`--help`とEngine GTP基本応答を確認

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime
- フレームワーク依存配置
