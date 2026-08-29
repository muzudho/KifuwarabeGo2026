# KifuwarabeGo2026.Reference.PlayRoomGui.Go

通常囲碁のPlay Room GUI固有実装を所有します。

最初の縦切りとして、囲碁固有の盤面、活動モード、手番、捕獲数、終局、レビュー位置をまとめたフレームワーク非依存の表示状態を所有します。入力変換、Presenter、Rendererも段階的に共通GUI境界から分離して配置します。

ロビーの設定・登録処理と囲碁ルールの正本は所有しません。
