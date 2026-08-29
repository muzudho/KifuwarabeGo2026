# KifuwarabeGo2026.Reference.Communication.Gtp

Protocol PとGTPエンジンを接続する参照アダプターです。`IGtpCommandTransport`により、標準入出力プロセス、TCP、インメモリテストなどの配送方法を分離します。

最初の`KifuwarabeGtpPlayerProtocol`は通常囲碁専用です。`known_command`で能力を確認し、きふわらべGTPエンジンでは原子的初期配置拡張を使用します。拡張を持たない汎用エンジンでは、9・13・19路盤の標準星配置なら`fixed_handicap`、黒石だけを置いて白番から始める自由配置なら`set_free_handicap`、それ以外の静的局面なら標準GTPの`boardsize`、`clear_board`、`komi`、`play`へ自動的にフォールバックします。`fixed_handicap`ではエンジンが返す頂点集合を要求局面と照合します。一つのGTPトランスポートは一つの参加割り当てだけを担当します。

観測に`setupBlack`、`setupWhite`、`moveHistory`があり、一手以上の履歴が存在してエンジンが`loadsgf`を実装している場合は、初期配置と受理済み着手・パスからSGF FF[4]を再構築して履歴ごと同期します。`IGtpSgfFileStore`がSGFの物理配置を抽象化し、既定の`TemporaryGtpSgfFileStore`はUTF-8一時ファイルを作成して`loadsgf`応答後に削除します。ファイルパスは改行、NUL、二重引用符を拒否し、空白を含む場合だけ二重引用符で囲みます。

`ProcessGtpCommandTransport`を含む外部プロセスクライアントは`KifuwarabeGo2026.FormalAdapter.Gtp.Client`へ移行済みです。このプロジェクトはそのトランスポート契約を使ってProtocol PとGTPを接続します。

既存GUI経路と互換性のある`GtpEngineClient`と、きふわらべオプション拡張文書も`KifuwarabeGo2026.FormalAdapter.Gtp`へ移行済みです。GTP応答、コマンド引数、コマンドセッションの最小契約は同ProjectFamilyの`Protocol`が所有します。

標準`play`フォールバックは、盤上の石を捕獲なしに順次再現できる静的初期局面を対象にします。履歴があるのに`loadsgf`を持たないエンジンでは、現在盤面だけを同期するためコウ履歴などは失われます。プロセスの再起動、標準エラーの永続的な収集、SGF読込後のエンジン固有検証は次段で追加します。
