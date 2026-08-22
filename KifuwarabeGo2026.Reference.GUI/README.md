# KifuwarabeGo2026.Reference.GUI

Protocol Gを利用する公式参照GUIの、描画技術に依存しないクライアント状態モデルです。`GameOasisGuiClient`はプレイスペースカタログ、一つの選択中セッション、最新スナップショット、直近エラーを保持します。

このプロジェクトは`GameOasis.Contracts`だけを参照します。Concierge、通常囲碁、ポン抜き、MonoGame、Windows UIには依存しません。具体的な画面はこの状態を描画し、意味的な操作をメソッドへ渡します。

`GameBoardProjection`は通常囲碁state v1とポン抜きstate v1を、共通の`GuiBoardView`へ変換します。盤サイズ、黒石、白石、手番、捕獲数、コウ点、終局、結果を画面へ渡し、範囲外の点、黒白重複、未知のスキーマを境界エラーとして拒否します。

`GameBoardActionFactory`は共通盤面の空点クリックを、現在手番と公式スキーマIDを持つ通常囲碁またはポン抜きの`play`文書へ変換します。画面側がゲーム固有JSONを組み立てる必要はありません。終局後、盤外、占有点、未対応ゲームへの入力は送信前に拒否します。
