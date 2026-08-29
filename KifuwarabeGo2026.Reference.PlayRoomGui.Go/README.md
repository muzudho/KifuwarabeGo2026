# KifuwarabeGo2026.Reference.PlayRoomGui.Go

通常囲碁のPlay Room GUI固有実装を所有します。

囲碁固有の盤面、活動モード、手番、捕獲数、終局、レビュー位置、最終手をまとめたフレームワーク非依存の表示状態を所有します。また、盤領域から交点配置を計算し、画面入力を囲碁盤座標へ変換する幾何モデルと、表示状態から石・コウ印・最終手・着手候補の描画要素を作るPresenterも所有します。Rendererは段階的に共通GUI境界から分離して配置します。

ロビーの設定・登録処理と囲碁ルールの正本は所有しません。

Local Match、盤面編集、変化図、レビューに加え、CGOS観戦も互換GUI側のAdapterから同じ表示状態とPresenterへ接続します。通信固有型はこのプロジェクトへ持ち込まず、外側で表示状態へ投影します。

Protocol Gの`GuiBoardView`は公開GUI契約なので、このプロジェクトの`GuiBoardViewAdapter`が直接表示状態へ投影します。Board Editor、Review、Match等のHost実装やプロセス配置に依存せず、同じ盤面Presenterを利用できます。

`GoPlayRoomLaunchInterpreter`は、保存・転送後の`PlayRoomLaunchRequest`から囲碁の盤サイズ、コミ、開始手番、初期石、持ち時間、参加者を`GoPlayRoomLaunchPlan`へ解釈します。Lobby GUIの画面型や保存型を参照せず、公開起動契約だけから新しい囲碁Play Roomセッションを準備できます。
