# KifuwarabeGo2026.FormalAdapter.Cgos

CGOSとゲームオアシスのカジュアル・コアを接続するフォーマル・アダプターのProjectFamily入口です。

現在のCGOS実装は`KifuwarabeGo2026.Reference.Communication.Cgos.Host`とGUI CoreのCGOS画面・接続処理から段階的に移行しています。ホストの実行ファイル名、設定、発行物との互換性を維持しながら、再利用可能なプロトコル解析、状態機械、意味変換をこの配下へ集約します。

`Protocol`名前空間はネットワーク、GUI、囲碁盤に依存しない型付きサーバーメッセージ、クライアントコマンド、純粋パーサー／フォーマッターを所有します。未知サーバー行は原文と引数を保持し、パスワードコマンドはログ用の機密フラグを持ちます。

`Client`名前空間は、接続設定と資格情報を受け取り、TCP接続、最初の行のタイムアウト、ログイン、型付き送受信、quitを所有する`CgosNetworkSession`を提供します。ログ出力先や資格情報の保存方法は所有しません。

`PlayerEngine`名前空間は、`ICgosPlayerEngine`越しにsetup、棋歴再現、play、genmove、解析付き着手、投了、人間着手、gameoverを処理する`CgosPlayerStateMachine`を所有します。GTPプロセスの起動方法とエンジン固有オプションはHost側の実装へ注入します。

`GameMasterEngine`名前空間は、管理者ログインの準備状態と、標準的な`who`、`match`、`quit`入力から型付きCGOSコマンドへの変換を所有します。標準入力の監視方法はHost側に残します。

`Observability`名前空間は、setup、play、解析付きの自分の着手、gameoverに加え、接続、ログイン、GTP待機、終了、異常を運ぶversion 1 JSON Lines通知と、その損失のない読み書きを所有します。人間向けログの文言を機械契約にはしません。

`Go`名前空間は、CGOS対局通知をGUI非依存の囲碁イベントへ投影します。CGOSの色表現、パス、I列を飛ばすGTP座標、盤サイズ、棋歴を検証し、0始まりの中立座標へ変換します。盤面ルールと描画モデルは所有しません。

`Compatibility`名前空間は、JSON Lines導入前のHost表示ログを現在の通知へ変換する期限付き互換境界です。GUIの観戦状態は旧ログの字句を直接解釈しません。
