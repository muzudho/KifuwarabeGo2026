# KifuwarabeGo2026.Reference.GUI

Protocol Gを利用する公式参照GUIの、描画技術に依存しないクライアント状態モデルです。`GameOasisGuiClient`はプレイスペースカタログ、一つの選択中セッション、最新スナップショット、直近エラーを保持します。

このプロジェクトは`GameOasis.Contracts`だけを参照します。Concierge、通常囲碁、ポン抜き、MonoGame、Windows UIには依存しません。具体的な画面はこの状態を描画し、意味的な操作をメソッドへ渡します。
