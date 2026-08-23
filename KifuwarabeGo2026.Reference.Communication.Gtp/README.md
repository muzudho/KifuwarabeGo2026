# KifuwarabeGo2026.Reference.Communication.Gtp

Protocol PとGTPエンジンを接続する参照アダプターです。`IGtpCommandTransport`により、標準入出力プロセス、TCP、インメモリテストなどの配送方法を分離します。

最初の`KifuwarabeGtpPlayerProtocol`は通常囲碁専用です。`known_command`で能力を確認し、きふわらべGTPエンジンでは原子的初期配置拡張を使用します。拡張を持たない汎用エンジンでは、9・13・19路盤の標準星配置なら`fixed_handicap`、黒石だけを置いて白番から始める自由配置なら`set_free_handicap`、それ以外の静的局面なら標準GTPの`boardsize`、`clear_board`、`komi`、`play`へ自動的にフォールバックします。`fixed_handicap`ではエンジンが返す頂点集合を要求局面と照合します。一つのGTPトランスポートは一つの参加割り当てだけを担当します。

観測に`setupBlack`、`setupWhite`、`moveHistory`があり、一手以上の履歴が存在してエンジンが`loadsgf`を実装している場合は、初期配置と受理済み着手・パスからSGF FF[4]を再構築して履歴ごと同期します。`IGtpSgfFileStore`がSGFの物理配置を抽象化し、既定の`TemporaryGtpSgfFileStore`はUTF-8一時ファイルを作成して`loadsgf`応答後に削除します。ファイルパスは改行、NUL、二重引用符を拒否し、空白を含む場合だけ二重引用符で囲みます。

`ProcessGtpCommandTransport` は外部 GTP エンジンを非シェル起動し、標準入出力のコマンドを直列化して、成功・失敗・複数行応答を解析します。標準エラーはデッドロック防止のため並行して排出し、破棄時は `quit` を送り、終了しないプロセスを停止します。コマンド単位のタイムアウトは呼び出し側の `CancellationToken` で指定します。応答待ちが中断されるとコマンドと応答の対応を保証できないため、プロセスを停止し、そのトランスポートを再利用不可にします。

既存GUI経路と互換性のある`GtpEngineClient`もこのプロジェクトが所有します。外部プロセス、GTP応答、コマンドセッション、きふわらべオプション拡張文書を扱いますが、GUI型やプレイスペースの盤面型には依存しません。

標準`play`フォールバックは、盤上の石を捕獲なしに順次再現できる静的初期局面を対象にします。履歴があるのに`loadsgf`を持たないエンジンでは、現在盤面だけを同期するためコウ履歴などは失われます。プロセスの再起動、標準エラーの永続的な収集、SGF読込後のエンジン固有検証は次段で追加します。
