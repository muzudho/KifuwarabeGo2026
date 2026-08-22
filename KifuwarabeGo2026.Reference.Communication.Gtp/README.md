# KifuwarabeGo2026.Reference.Communication.Gtp

Protocol PとGTPエンジンを接続する参照アダプターです。`IGtpCommandTransport`により、標準入出力プロセス、TCP、インメモリテストなどの配送方法を分離します。

最初の`KifuwarabeGtpPlayerProtocol`は通常囲碁専用で、きふわらべGTPエンジンの原子的初期配置拡張`kfw-begin-position`、`kfw-add-black`、`kfw-add-white`、`kfw-set-to-play`、`kfw-commit-position`を使用します。一つのGTPトランスポートは一つの参加割り当てだけを担当します。

`ProcessGtpCommandTransport` は外部 GTP エンジンを非シェル起動し、標準入出力のコマンドを直列化して、成功・失敗・複数行応答を解析します。標準エラーはデッドロック防止のため並行して排出し、破棄時は `quit` を送り、終了しないプロセスを停止します。コマンド単位のタイムアウトは呼び出し側の `CancellationToken` で指定します。応答待ちが中断されるとコマンドと応答の対応を保証できないため、プロセスを停止し、そのトランスポートを再利用不可にします。

プロセスの再起動と標準エラーの永続的な収集、汎用 GTP エンジンへの `fixed_handicap`、`set_free_handicap`、`loadsgf` フォールバックは次段で追加します。
