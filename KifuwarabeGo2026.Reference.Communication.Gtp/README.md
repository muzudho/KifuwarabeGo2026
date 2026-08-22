# KifuwarabeGo2026.Reference.Communication.Gtp

Protocol PとGTPエンジンを接続する参照アダプターです。`IGtpCommandTransport`により、標準入出力プロセス、TCP、インメモリテストなどの配送方法を分離します。

最初の`KifuwarabeGtpPlayerProtocol`は通常囲碁専用で、きふわらべGTPエンジンの原子的初期配置拡張`kfw-begin-position`、`kfw-add-black`、`kfw-add-white`、`kfw-set-to-play`、`kfw-commit-position`を使用します。一つのGTPトランスポートは一つの参加割り当てだけを担当します。

外部プロセスの起動、監視、再起動、タイムアウト、標準エラー収集はトランスポート実装側の責務であり、この最小段階には含みません。汎用GTPエンジンへの`fixed_handicap`、`set_free_handicap`、`loadsgf`フォールバックも次段で追加します。
