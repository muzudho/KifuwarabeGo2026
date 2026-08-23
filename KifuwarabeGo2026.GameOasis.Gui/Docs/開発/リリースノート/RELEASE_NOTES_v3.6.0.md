# Kifuwarabe Go 2026 v3.6.0

盤面の連と境界を段階的に観察できるBOARD LENSを完成させ、その連解析をポン抜きプレイヤーの着手判断にも利用したリリースです。

## BOARD LENS

- `L`で次のレンズ、`K`で系統切替、`1`で終了する操作へ統一しました
- 棋譜レビュー欄へBOARD LENSボタンと系統切替ボタンを追加しました
- 連番号を、水色の縁取り付き`#番号`へ統一しました
- 連解析系4種類と計測系6種類を利用できます
- 境界の交点を丸、隣接する連を四角の足先マーカーで表現します
- 重複する足先を風車式にずらし、関係線の重なりを抑えました

## 連解析系

- `REN INDEX LENS`
- `REN RECTANGLE LENS`
- `REN NETWORK LENS - BASIC`
- `REN NETWORK LENS - EYE MODE`
- REN NETWORKのエッジを、黒連と空連は黒、白連と空連は白、黒連と白連は低彩度の青で表示します

## 計測系

- `REN AREA LENS`
- `BOUNDARY COUNT LENS`
- `BOUNDARY EMPTY COUNT LENS (a.k.a. LIBERTY COUNT)`
- `BOUNDARY OPPONENT COUNT LENS`
- `ADJACENT EMPTY AREA LENS`
- `ADJACENT OPPONENT AREA LENS`
- 空点・自連・相手連の意味に対応した色で値を表示します
- 3桁の面積値を2桁相当の幅へ縮小し、19路盤でも読みやすくしました

## ポン抜きプレイヤー

- 最も多く相手石を取れる手を、ランダム着手より優先します
- 着手後の自連面積が、隣接する全相手連面積を上回る接触手を優先します
- 相手へ接触しない通常手を、危険な接触手より優先します
- 同じ優先度の候補内では、従来のNormalランダムまたは星領域ランダムを使います

## テスト状況

- ソリューション全体のReleaseビルド
- 移植性スモークとWindowsスモーク
- 最大捕獲数と連面積比較の局面スモーク
- GUI版とEngine版のWindows x64 publish
- 同梱CGOS通信コンポーネントとEngine GTP応答のスモークテスト

## 配布物

- GUI版: `KifuwarabeGo2026.GameOasis.Gui-v3.6.0-win-x64.zip`
- Engine版: `KifuwarabeGo2026.Engine-v3.6.0-win-x64.zip`

GUIとEngineの両方を使う場合は、2つともダウンロードしてください。

## SHA-256

- GUI版: `E76F23AACCB74F82D17684D7B3D37CD697B80185FEA021596AF9CB4CFAFF2048`
- Engine版: `786F1156DF8BDB7E5AFD48F45436FD3B2138886AB0B5CCD71DE625239EE58EB8`

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime
- フレームワーク依存配置
