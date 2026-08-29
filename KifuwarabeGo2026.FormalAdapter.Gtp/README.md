# KifuwarabeGo2026.FormalAdapter.Gtp

GTPとゲームオアシスのカジュアル・コアを接続するフォーマル・アダプターのProjectFamily入口です。

現在のGTP実装は`KifuwarabeGo2026.Reference.Communication.Gtp`と`KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions`から段階的に移行しています。実行ファイル名、設定、発行物との互換性を維持しながら、外部仕様の解釈と意味変換に当たる部分だけをこの配下へ集約します。

`Protocol`名前空間は、GUI、プロセス、囲碁盤に依存しない最小契約を所有します。

* `GtpCommandArgument`
* `GtpCommandResult`
* `GtpFilePathArgumentStyle`
* `IGtpCommandSession`

囲碁の`GoPoint`へ変換する`GtpCoordinate`はこの純粋な層へ含めず、移行後の`FormalAdapter.Gtp.Go`に置く方針です。

`Client`名前空間は、外部GTPエンジンのプロセス起動、標準入出力トランスポート、起動設定、コマンドセッションを所有します。`Options`名前空間は、標準外のGUIオプション拡張文書を所有します。どちらもGUI型やゲーム局面型を参照しません。
