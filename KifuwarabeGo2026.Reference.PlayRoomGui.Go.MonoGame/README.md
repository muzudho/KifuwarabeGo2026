# KifuwarabeGo2026.Reference.PlayRoomGui.Go.MonoGame

`Reference.PlayRoomGui.Go`が生成したフレームワーク非依存の囲碁盤面描画要素を、MonoGameの描画命令へ変換する参照実装です。盤材背景、枠、罫線、星、座標ラベル、石、コウ印、スーパーコウ印、最終手、着手候補を描画し、石テクスチャの生成と寿命も管理します。

Lobby、互換`GameOasis.Gui`、Play Roomセッションには依存しません。ゲーム状態や表示判断を所有せず、MonoGame固有のフォント、色、プリミティブ描画だけを所有します。
