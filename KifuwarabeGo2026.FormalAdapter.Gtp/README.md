# KifuwarabeGo2026.FormalAdapter.Gtp

GTPとゲームオアシスのカジュアル・コアを接続するフォーマル・アダプターのProjectFamily入口です。

GTPの純粋なプロトコル契約、外部プロセスクライアント、オプション文書、囲碁座標変換をこの配下へ集約しています。`KifuwarabeGo2026.Reference.Communication.Gtp`にはProtocol Pとの接続と参照GTPサーバー、`KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions`には初期局面戦略と能力プロファイルを残し、実行ファイル名、設定、発行物との互換性を維持します。

`Protocol`名前空間は、GUI、プロセス、囲碁盤に依存しない最小契約を所有します。

* `GtpCommandArgument`
* `GtpCommandResult`
* `GtpFilePathArgumentStyle`
* `IGtpCommandSession`

`Go`名前空間は、共有の`GoPoint`とGTP頂点を相互変換する`GtpCoordinate`を所有します。盤ルールやGUI型は所有しません。

`Client`名前空間は、外部GTPエンジンのプロセス起動、標準入出力トランスポート、起動設定、コマンドセッションを所有します。`Options`名前空間は、標準外のGUIオプション拡張文書を所有します。どちらもGUI型やゲーム局面型を参照しません。
