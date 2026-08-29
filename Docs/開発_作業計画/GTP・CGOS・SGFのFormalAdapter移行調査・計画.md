# GTP・CGOS・SGFのFormalAdapter移行調査・計画

状態：作業段階0・1・2・3・4完了、作業段階5実装中（2026年8月29日）

## 目的

リポジトリー内でGTP、CGOS、SGFを使用している場所を調査し、外部仕様の解釈、意味変換、実装差の吸収に当たる処理を`KifuwarabeGo2026.FormalAdapter.*`へ段階的に移す計画を定めます。

調査結果を基準に段階的な移行を進めます。USI、CSA、KIFは、対応するゲームをまだ実装していないため今回の調査対象外です。

## 判断基準

次のものを`FormalAdapter`への移行候補とします。

* 外部仕様の字句、コマンド、応答、ファイル形式を解釈する処理。
* 外部仕様とゲームオアシスのProtocol G、P、M、Sとの意味変換。
* 外部実装の能力差、方言、独自拡張、エラー差を吸収する処理。
* 外部仕様固有の接続、初期化、終了ライフサイクル。
* 外部仕様の適合性試験、実装差試験、テストベクトル。

次のものは、名前にGTP、CGOS、SGFが含まれていても原則として移しません。

* ロビーの登録・選択画面、プレイルームの描画・入力。
* 保存先、画面表示、ダイアログ、選択中状態。
* 権威あるゲーム局面、合法手、勝敗判定、プレイヤー戦略。
* アダプターを選択・起動するアプリケーションの構成点。
* リリース手順、過去の開発日誌、完了済み計画の履歴。

## 現在の全体像

```text
GTP
  Reference.Communication.Gtp
    ├─ Protocolプリミティブ
    ├─ クライアントと子プロセス
    ├─ Protocol Pへのプレイヤー変換
    └─ GTPサーバー
  Reference.PlaySpace.Go.GtpExtensions
    ├─ 能力調査と実装プロフィール
    ├─ 初期局面の再現方法
    └─ SGFを使うloadsgf経路
  GameOasis.Gui
    ├─ エンジン登録・選択・設定画面
    └─ 対局・ポン抜きからの利用

CGOS
  Reference.Communication.Cgos.Host/Program.cs
    ├─ コマンドライン
    ├─ TCP・ログイン
    ├─ CGOSクライアント状態機械
    ├─ 管理者クライアント
    ├─ 標準入出力リレー
    └─ GTPエンジン子プロセス
  GameOasis.Gui
    ├─ Host起動・監視・ログ解釈
    ├─ 接続先と資格情報の画面
    ├─ CGOS setup/play/gameoverの観測状態
    ├─ 解析JSONの解釈
    └─ 観戦・結果・棋譜保存画面

SGF
  GameOasis.Gui/Sgf/SgfGameRecordConverter.cs
    ├─ SGF字句・構文解析
    ├─ SGF書出し
    ├─ GoGameRecord変換
    └─ CGOS解析拡張
  Reference.PlaySpace.Go.GtpExtensions/Sgf
    └─ 初期局面SGFの別実装
  Reference.Communication.Gtp/GtpSgfFileStore.cs
    └─ loadsgf用一時ファイル
  GameOasis.Gui
    ├─ 読込・保存・自動保存
    ├─ レビュー画面
    └─ ファイル名と保存先
```

## GTPの調査結果

### 現在ほぼそのまま移せる候補

| 現在の場所 | 現在の責務 | 移行候補 | 難易度 | 判断 |
|---|---|---|---|---|
| `Reference.Communication.Gtp/Protocol/GtpCommandArgument.cs` | コマンド引数の引用・整形 | `FormalAdapter.Gtp.Protocol` | 低 | 純粋なGTP形式処理 |
| `Protocol/GtpCommandResult.cs` | GTP応答の最小結果 | `FormalAdapter.Gtp.Protocol` | 低 | 外部仕様固有の結果 |
| `Protocol/GtpFilePathArgumentStyle.cs` | ファイルパス引数の方言 | `FormalAdapter.Gtp.Protocol` | 低 | 実装差の吸収 |
| `Protocol/IGtpCommandSession.cs` | GTPコマンドセッション境界 | `FormalAdapter.Gtp.Protocol` | 低 | GUIやゲーム状態へ非依存 |
| `Client/GtpEngineClientCommandSession.cs` | クライアントをコマンドセッションへ適合 | `FormalAdapter.Gtp.Client` | 低 | 薄いGTPアダプター |
| `Client/GtpEngineClient.cs` | 標準入出力GTPクライアント | `FormalAdapter.Gtp.Client` | 中 | プロセス所有・ログ境界を確認して移せる |
| `IGtpCommandTransport.cs`、`ProcessGtpCommandTransport.cs` | GTPプロセス起動、要求・応答 | `FormalAdapter.Gtp.Client` | 中 | 外部プロセスのライフサイクルを所有 |
| `Client/GtpEngineSettings.cs` | GTPクライアント起動設定 | `FormalAdapter.Gtp.Client` | 低 | 画面型を含まない |
| `Client/GtpGuiOptionsDocument.cs` | GTPオプション文書 | `FormalAdapter.Gtp.Options` | 中 | GUI表示モデルと外部オプション文法を分離して移す |

`GtpCoordinate.cs`はGTP座標と囲碁の`GoPoint`を直接相互変換します。GTP字句だけではなく囲碁型へ依存するため、`FormalAdapter.Gtp.Protocol`ではなく`FormalAdapter.Gtp.Go`へ置くのが適切です。

### Protocol Pとの変換候補

`Reference.Communication.Gtp/KifuwarabeGtpPlayerProtocol.cs`は約445行あり、GTPエンジンをゲームオアシスのProtocol Pプレイヤーとして扱う意味変換を所有します。これは`FormalAdapter.Gtp.PlayerEngine`の中核候補です。

移行時は、次を分けます。

* GTPコマンド送受信は`FormalAdapter.Gtp.Client`。
* GTPの着手・パス・投了・オプションとProtocol Pの対応は`FormalAdapter.Gtp.PlayerEngine`。
* Goの座標・局面文書との対応は`FormalAdapter.Gtp.Go`。
* プレイヤー戦略そのものは`Reference.PlayerEngine`に残す。

### GTPサーバー候補

`Reference.Communication.Gtp/Server`には、`GtpEngine.cs`約658行、オプション処理約479行、ポン抜き拡張約131行、原子的局面設定約104行があります。これは公式プレイヤー実装を既存GTP GUIから利用できるようにする、外向きのフォーマル・アダプターです。

候補配置は次の通りです。

```text
FormalAdapter.Gtp.PlayerEngine.Server
FormalAdapter.Gtp.Options
FormalAdapter.Gtp.Go
FormalAdapter.Gtp.Ponnuki
```

ただし、現在のHostは出力実行ファイル名`KifuwarabeGo2026.Engine`を互換維持しています。サーバー実装を移しても、`Reference.Communication.Gtp.Host`は当面、互換実行ファイル名を持つ薄い構成点として残します。

### `Go.GtpExtensions`の再分類

`Reference.PlaySpace.Go.GtpExtensions`は、すべてを`FormalAdapter`へ移す対象ではありません。

| 分類 | 主な型 | 方針 |
|---|---|---|
| GTP能力・実装差 | `GtpCapabilityProbe`、`GtpCapabilitySet`、`BuiltInGtpProfiles`、`GenericGtpProfile` | `FormalAdapter.Gtp.Go`へ移行候補 |
| GTPコマンドによる局面再現 | 各`*Strategy`、`GtpInitialPositionCommandBuilder`、`GtpInitialPositionExecutionHost` | `FormalAdapter.Gtp.Go.InitialPosition`へ移行候補 |
| カジュアルな初期局面モデル | `InitialPositionRequest`、分類、試行結果、検証結果 | Go共有領域またはPlaySpace側に残す |
| 囲碁ルール由来 | `FixedHandicapPoints` | Go共有領域に残す候補 |
| SGF文書生成 | `InitialPositionSgfBuilder`、`InitialPositionDocument` | `FormalAdapter.Sgf.Go`へ統合候補 |
| loadsgf用一時ファイル | `GtpInitialPositionSgfFile`、`GtpSgfFileStore` | `FormalAdapter.Gtp.Go`に残し、SGF文書モデルを利用する |

### 移さないGTP関連箇所

`GameOasis.Application`の`GtpEngineCatalog`、`GtpEngineProfile`と保存処理は、ロビーが外部プレイヤーのインストール情報を管理する責務です。GTP固有項目は将来、汎用プレイヤー・アダプタープロフィールへ正規化できますが、カタログ所有そのものは`FormalAdapter`へ移しません。

GUI Coreの登録、編集、選択、描画、開始可否表示もロビーGUIに残します。GTPの能力調査やプロセス起動だけをインターフェース越しに呼び出す形へ変えます。

## CGOSの調査結果

### 独立Hostの現在地

`Reference.Communication.Cgos.Host/Program.cs`は約1,300行の単一ファイルで、次の型が同居しています。

| 型 | 責務 | 候補配置 |
|---|---|---|
| `Program` | 起動、キャンセル、親プロセス監視、構成 | 互換Hostに残す |
| `CgosClientOptions`、`CgosAccount` | コマンドラインと資格情報 | Host設定。公開プロフィールとは分ける |
| `CgosTcpConnector` | DNS、TCP接続、タイムアウト | Hostインフラストラクチャーまたは再利用可能な接続層 |
| `CgosConnectionSession` | CGOSログイン、行送受信、quit | `FormalAdapter.Cgos.Protocol` |
| `CgosClient` | setup、play、genmove、gameover等の状態機械 | `FormalAdapter.Cgos.PlayerEngine` |
| `CgosAdminClient` | who、match等の管理コマンド | `FormalAdapter.Cgos.GameMasterEngine`候補 |
| `CgosPlayerControl` | 人間着手、投了要求の待機 | HostとProtocol G/Pの接続側 |
| `CgosStandardInputRelay` | Host制御用標準入力 | Hostに残す |
| `GtpEngineProcess` | GTP子プロセス起動・同期 | `FormalAdapter.Cgos.Host`から`FormalAdapter.Gtp`を利用する構成点 |

最初に`Program.cs`を物理分割するだけでは責務移行になりません。先に、CGOSのサーバー行を型付きイベントへ変換する純粋なパーサーと、クライアントコマンドを生成する純粋なフォーマッターを`FormalAdapter.Cgos.Protocol`へ作るのが安全です。

### GUI Core側の候補

| 現在の場所 | 現在の責務 | 方針 |
|---|---|---|
| `CgosConnectionProcess.cs` | Host探索・起動、標準入出力、ログ保存、親プロセス引数 | プロセス制御を`FormalAdapter.Cgos.Client`候補へ分離。画面状態はGUIに残す |
| `CgosGameObservation.cs` | Hostログの解析、setup/play/gameover、囲碁盤、時計、棋譜投影 | CGOS行解析を`FormalAdapter.Cgos.Protocol`、Goへの投影を`FormalAdapter.Cgos.Go`へ分ける。表示状態はPlayRoom側に残す |
| `CgosMoveAnalysisParser.cs` | zakki系解析JSONの解釈 | `FormalAdapter.Cgos.Analysis`候補 |
| `CgosConnectionProfile`とカタログ | 接続先、イベント、ラウンド、資格情報 | LobbyEngine/Applicationに残す。Host用設定への変換だけアダプター側 |
| CGOS各Renderer/Page | 接続、観戦、結果の表示・入力 | Lobby GUI／PlayRoom GUIに残す |
| `CgosSgfFileNameBuilder` | 保存ファイル名方針 | 保存・記録サービス側。CGOSプロトコルには入れない |

現在、GUIはHostの人間向けログに含まれる`> setup`、`> play`、`> gameover`などを再解析しています。ログ文面が事実上の内部プロトコルになっているため、移行前にHost通知を型付き契約またはJSON Linesへ分ける必要があります。標準出力を機械向け通知、標準エラーを診断ログとする構成が候補です。

### CGOSで先に固定する契約

```text
CgosServerMessage
  ProtocolAdvertised
  LoginRequested
  LoginAccepted
  MatchSetup
  MovePlayed
  MoveAnalysis
  GameOver
  ServerError

CgosClientCommand
  ClientIdentity
  Username
  Password
  Genmove
  Play
  Resign
  Quit
```

実際の名称は実装段階で既存CGOSコマンドを再調査して決定します。資格情報を通知やログへ含めないこと、未知コマンドを保持または診断できることを完了条件にします。

## SGFの調査結果

### 最優先の切り出し候補

`GameOasis.Gui/Sgf/SgfGameRecordConverter.cs`は約693行あり、次が一つのクラスに混在しています。

* SGFゲーム木の字句・構文解析。
* プロパティ値のエスケープと書出し。
* SGF座標と`GoPoint`の変換。
* SGFルート情報とGUI互換`GoGameRecord`の変換。
* 着手、時間、コメント、セットアップ石の変換。
* CGOS解析JSONと独自`CC`、`KFW`、旧`KFA`プロパティの変換。
* 先頭ゲーム木の主分岐だけを読む制限。

SGF文書パーサーはGUI、MonoGame、ファイルシステムへ本質的に依存しないため、最初の切り出し候補です。既存の[`SGF文書モデル実装計画.md`](../開発/SGF文書モデル実装計画.md)にある、未知プロパティと値順序を保持する`SgfDocument`、`SgfGameTree`、`SgfNode`、`SgfProperty`を`FormalAdapter.Sgf`へ実装する方針とします。

### 候補配置

| 現在の処理 | 候補配置 | 前提 |
|---|---|---|
| パーサー、ライター、例外、プロパティ値 | `FormalAdapter.Sgf` | 未知プロパティ、複数値、複数ゲーム木を失わない |
| `SgfCoordinate` | `FormalAdapter.Sgf.Go` | `GoPoint`との対応だけを所有 |
| SGFと`GoGameRecord`の変換 | `FormalAdapter.Sgf.Go` | GUI互換型を直接参照しない中立な棋譜型または変換境界が必要 |
| `CC`、`KFW`、`KFA`解析拡張 | `FormalAdapter.Sgf.Kifuwarabe`候補 | CGOS解析モデルをGUIから分離する |
| `InitialPositionSgfBuilder` | `FormalAdapter.Sgf.Go` | 共通SGF文書ライターを利用する |
| `GtpSgfFileStore` | `FormalAdapter.Gtp.Go` | SGF形式ではなくloadsgf配送用一時ファイルの責務 |
| ローカル・CGOS用ファイル名 | 記録保存サービス | SGF形式そのものではない |
| 保存先設定、自動保存状態、レビュー画面 | Lobby／PlayRoom GUI | 利用者操作と画面状態 |

`SgfGameRecordConverter`は`GoGameRecord`、`CgosMoveAnalysisParser`、`GtpCoordinate`へ依存しているため、そのままプロジェクト移動するとGUIとGTPへの逆依存を持ち込みます。最初に純粋なSGF文書モデルを抽出し、その後、Go棋譜とCGOS解析の中立モデルを境界として変換を分けます。

### 現在対応している主なSGF要素

調査時点で、`GM`、`FF`、`CA`、`AP`、`RU`、`SZ`、`KM`、`TM`、`GN`、`PB`、`PW`、`BR`、`WR`、`DT`、`PC`、`RE`、`AB`、`AW`、`C`、`B`、`W`、`BL`、`WL`、`CC`、`KFW`、旧`KFA`を扱っています。

現行パーサーは主分岐を`GoGameRecord`へ変換する際、変化図、未知プロパティ、複数ゲーム木を完全には保持しません。新しい文書モデルはまず損失なしの往復を担い、`GoGameRecord`への縮約は別の明示的な変換として扱います。

## 推奨する移行順序

### 作業段階0：基準を固定する

状態：完了（2026年8月29日）

* 現行GTP単体試験、Windows実プロセス試験、GUI移植性試験を記録する。
* CGOS Hostの`--help`、ログイン、setup、play、gameover、投了、人間着手のサンプル入出力を匿名化してテストベクトル化する。
* SGFの読込、保存、コメント、解析、初期配置、時間、旧形式更新の代表文書を固定する。

完了条件：移動後の互換性を機械的に比較できる。

#### 作業段階0の実施記録

言語非依存の匿名化ベクトルを`Conformance/FormalAdapters/v1`へ追加しました。

| ベクトル | 固定した内容 |
|---|---|
| `gtp-baseline.json` | ファイルパス引数の引用、改行・引用符拒否、9路・19路の座標、pass、不正頂点 |
| `cgos-baseline.json` | `--help`必須項目、匿名ログイン、setup、生成着手、play、gameover、人間着手、投了、quit |
| `sgf-baseline.sgf` | 対局情報、9路、コミ、持ち時間、初期配置、着手、pass、BL/WL、コメント、CC解析JSON |
| `sgf-legacy-kfa.sgf` | 旧KFAからKFWへの更新と、未解釈JSONの保持 |

ベクトルの検査は既存の`KifuwarabeGo2026.Tests.GameOasis.Gui.Portability`へ`FormalAdapterBaselineChecks`として追加しました。移行前の実装に対して次を機械的に比較します。

* `GtpCommandArgument`と`GtpCoordinate`の現行結果。
* 実際のCGOS Hostを`--help`で起動した出力。
* `CgosGameObservation`へ匿名化ログを順に適用した対局状態、棋譜、解析結果。
* `SgfGameRecordConverter`による代表SGFの読込、現行モデル往復、コメント、解析JSON、旧形式更新。

専用の新規テスト実行ファイルは作らず、既にGUI、GTP、Go基盤を参照している移植性試験へ統合しました。これにより、同じ製品DLLを複数の試験出力へ複製する量を増やさず、将来は利用側だけを新`FormalAdapter`へ切り替えて同じベクトルを継続利用できます。

検証結果：

* ベースライン追加後の移植性試験プロジェクトはReleaseで警告0件、エラー0件でビルド成功した。
* 既存`Tests.Reference.Communication.Gtp`は`PASS`し、現行GTPのProtocol P同期、拒否着手からの回復、相手着手中継が動作している。
* 当初は再ビルドされた`Reference.Communication.Gtp.dll`をWindows Application Controlが`0x800711C7`で遮断した。第1段階の新しい依存グラフで全生成物を再ビルドすると遮断は解消し、検査本体まで到達した。
* 最初の実行で`sgf-baseline.sgf`のCC内JSONにある配列閉じ角括弧がSGF値として未エスケープだったことを検出した。`\]`へ修正後、GTP、CGOS、SGFの全ベースラインが`PASS`した。

再試験コマンド：

```powershell
dotnet build KifuwarabeGo2026.Tests.GameOasis.Gui.Portability\KifuwarabeGo2026.Tests.GameOasis.Gui.Portability.csproj -c Release --no-restore
dotnet KifuwarabeGo2026.Tests.GameOasis.Gui.Portability\bin\Release\net8.0\KifuwarabeGo2026.Tests.GameOasis.Gui.Portability.dll
```

新ベースラインのPASSを確認したため、この段階は完了です。

### 作業段階1：GTPプリミティブを移す

状態：完了（2026年8月29日）

`GtpCommandArgument`、`GtpCommandResult`、`GtpFilePathArgumentStyle`、`IGtpCommandSession`を`FormalAdapter.Gtp.Protocol`へ移します。次にクライアントとプロセストランスポートを移します。

完了条件：旧利用側を新プロジェクト参照へ切り替え、GTP試験、GUI移植性試験、Windows実プロセス試験がPASSする。旧名前空間を互換維持する必要がある場合は期限付きの薄い型転送またはアダプターだけを残す。

#### Protocolプリミティブの実施記録

`GtpCommandArgument`、`GtpCommandResult`、`GtpFilePathArgumentStyle`、`IGtpCommandSession`を`KifuwarabeGo2026.FormalAdapter.Gtp.Protocol`へ物理移動しました。旧プロジェクトに重複実装や互換型は残していません。

`Reference.Communication.Gtp`と`Reference.PlaySpace.Go.GtpExtensions`は新しいProtocol契約を参照します。`GtpCoordinate`は囲碁の`GoPoint`へ依存するため旧配置に残し、将来の`FormalAdapter.Gtp.Go`移行対象としました。

検証結果：

* ソリューション全体のReleaseビルドが警告0件、エラー0件で成功した。
* `Tests.Reference.Communication.Gtp`が`PASS`した。
* GTP、CGOS、SGFベースラインを含む`Tests.GameOasis.Gui.Portability`が`PASS`した。
* `Tests.GameOasis.Gui.Windows`の非対話Windowsプラットフォーム検査が`PASS`した。

#### ClientとOptionsの実施記録

次を`KifuwarabeGo2026.FormalAdapter.Gtp.Client`へ物理移動しました。

* `GtpEngineSettings`
* `GtpEngineClient`
* `GtpEngineClientCommandSession`
* `IGtpCommandTransport`と`GtpCommandResponse`
* `GtpProcessOptions`と`ProcessGtpCommandTransport`

クライアントから参照される`GtpGuiOptionsDocument`、`GtpOptionSchemaDocument`、`GtpOptionEvaluationDocument`と関連定義は、逆依存を作らないよう同時に`KifuwarabeGo2026.FormalAdapter.Gtp.Options`へ移しました。GUI、GTP拡張、旧GTP通信、試験プロジェクトは新プロジェクトを直接参照します。Hostの実行ファイル名とサーバー実装は変更していません。

移動後のソリューション全体Releaseビルドは警告0件、エラー0件で成功しました。一時的に再生成済み`StationeryUI.dll`をWindows Application Controlが`0x800711C7`で遮断したため、ReleaseとDebugの両構成で再生成し、コード内に迂回処理を入れず再評価を待ちました。最終的に次の全試験が`PASS`しました。

* 子プロセスを使う`Tests.Reference.Communication.Gtp`。
* GTP、CGOS、SGFベースラインと所有権検査を含む`Tests.GameOasis.Gui.Portability`。
* `Tests.GameOasis.Gui.Windows`の非対話Windowsプラットフォーム検査。

### 作業段階2：SGF文書モデルを作る

状態：完了（2026年8月29日）

`FormalAdapter.Sgf`へ、外部依存のない文書モデル、パーサー、ライター、例外を追加します。既存SGFを新モデルで読み書きし、未知プロパティ、値順序、変化図、複数ゲーム木を保持する試験を先に完成させます。

完了条件：文書の損失なし往復ができ、まだGUIの保存経路を切り替えなくても単独で検査できる。

#### 作業段階2の実施記録

外部プロジェクトへ依存しない`KifuwarabeGo2026.FormalAdapter.Sgf.Document`を実装しました。

| 型 | 責務 |
|---|---|
| `SgfDocument` | SGF Collectionに含まれる複数ゲーム木と順序の保持 |
| `SgfGameTree` | ノード列と、その末尾から分岐する変化図の保持 |
| `SgfNode` | プロパティと記載順の保持 |
| `SgfProperty` | 未知識別子、複数値、値順序の保持 |
| `SgfDocumentParser` | Collection全体、変化図、エスケープ、行継続の解析 |
| `SgfDocumentWriter` | SGFエスケープと改行を正規化した損失なし書出し |
| `SgfParseException` | 問題位置をオフセット付きで報告 |

専用の`KifuwarabeGo2026.Tests.FormalAdapter.Sgf`を追加し、次をGUIやGo型なしで検査します。

* 複数ゲーム木と左右の変化図。
* 未知プロパティとプロパティ順。
* 一プロパティの複数値と値順序。
* `\]`、`\\`、行継続、CRLF正規化。
* 作業段階0の通常SGFと旧KFAベクトルの再解析可能な往復。
* 空Collection、未閉鎖値、小文字識別子、空ゲーム木の拒否とエラー位置。

専用試験はReleaseで警告0件、エラー0件でビルドし、全項目が`PASS`しました。現行GUIの読込・保存経路はまだ切り替えず、次の作業段階3でGo変換とともに接続します。

### 作業段階3：SGFのGo変換を移す

状態：完了（2026年8月29日）

`SgfCoordinate`、初期局面SGF生成、Go棋譜との変換を`FormalAdapter.Sgf.Go`へ集約します。GUI互換`GoGameRecord`を直接参照しない中立な棋譜投影、またはGUI側の薄い変換口を定めます。CGOS解析拡張は別の`Kifuwarabe`拡張層へ分けます。

完了条件：GUI Coreの`Sgf`フォルダーから形式解析が消え、GUIは文書モデルと変換サービスだけを利用する。既存SGF回帰試験とReview Play Room試験がPASSする。

#### 第1縦切りの実施記録

* `SgfCoordinate`をGUI Coreから`KifuwarabeGo2026.FormalAdapter.Sgf.Go`へ物理移動した。
* GUIやCGOS型に依存しない`SgfGoGameRecord`、`SgfGoSetupStone`、`SgfGoMove`を追加した。
* `SgfGoGameRecordConverter`でルート情報、9・13・19路、コミ、持ち時間、初期配置、着手、パス、コメント、BL/WL、CC/KFW/KFA原文を文書モデルと相互変換できるようにした。
* 解析JSONはフォーマル層で内容を決めつけず、プロパティ識別子と原文を保持する。GUI表示用解析は従来どおりGUI側に残した。
* 中立棋譜への投影は最初のゲーム木の主手順だけに縮約する。複数ゲーム木と変化図は`SgfDocument`には残り、縮約操作と損失なし文書操作を区別した。
* GTP `loadsgf`用の`InitialPositionSgfBuilder`を、文字列の手組みではなく共通`SgfDocumentWriter`利用へ切り替えた。
* 現行GUIの読込入口を共通`SgfDocumentParser`へ接続した。意味変換、CGOS解析表示、互換例外はGUI側に維持した。

専用`Tests.FormalAdapter.Sgf`は、座標、初期配置、主手順、パス、持ち時間、コメント、解析原文、変化図を含む投影で`PASS`しました。全ソリューションReleaseビルドも警告0件、エラー0件です。

#### 第2縦切りの実施記録

GUIの公開`SgfGameRecordConverter.ToSgf`と`FromSgf`を、`GoGameRecord`と`SgfGoGameRecord`の薄い双方向写像へ切り替えました。GUI側には次だけを残しました。

* GUI互換`GoGameRecord`との値の写像。
* CGOS解析JSONを表示モデルへ解釈する処理と、表示モデルをCC JSONへ直す処理。
* 旧KFAをKFWへ文字列内の値を変えず更新する互換API。
* 既存GUIが捕捉する`SgfParseException`への例外変換。

GUI内の旧SGFパーサー、旧SGFライター、座標変換、プロパティ組立ては物理削除しました。保存、読込、自動保存、Review、PlayRoom Launch Requestは既存の公開APIを通して新FormalAdapterを利用するため、呼出し側を一括変更せず移行できました。

最終検証結果：

* ソリューション全体Releaseビルド：警告0件、エラー0件。
* `Tests.FormalAdapter.Sgf`：`PASS`。
* GTP、CGOS、SGFベースラインと所有権検査を含むGUI移植性試験：`PASS`。
* Board Editor、Review、Match Play Roomと異常終了を含む`Tests.PlayRoom.JsonLines`：`PASS`。
* Windows非対話プラットフォーム試験：`PASS`。

### 作業段階4：CGOS純粋プロトコルを抽出する

状態：完了（2026年8月29日）

CGOSサーバー行のパーサー、クライアントコマンドのフォーマッター、ログイン状態を`FormalAdapter.Cgos.Protocol`へ追加します。最初はHostの既存状態機械から利用し、実行ファイルとGUI経路は変えません。

完了条件：ネットワーク、GUI、ファイルシステムを使わないテストベクトルでCGOSメッセージを検査できる。

#### 作業段階4の実施記録

`KifuwarabeGo2026.FormalAdapter.Cgos.Protocol`へ、ネットワーク、GUI、囲碁盤に依存しない次の境界を実装しました。

* `CgosServerMessage`を基底とするprotocol、username、password、ok、setup、play、genmove、gameover、info、error、unknownの型付きメッセージ。
* setup棋歴の色、頂点、残り時間を保持する`CgosHistoricalMove`。
* identity、username、password、move、resign、ready、quit、who、matchの型付きクライアントコマンド。
* `CgosServerMessageParser`と`CgosClientCommandFormatter`。
* 不正なフィールドと原文を報告する`CgosProtocolException`。

未知サーバーコマンドは失敗させず、コマンド名、引数、原文を保持します。パスワードは送信用文字列を生成できますが、コマンド自体に機密フラグを持たせ、ログ用フォーマットでは`(password)`へ置換します。すべてのクライアントコマンドは改行とNULを拒否します。

現行CGOS Hostも新しい境界へ接続しました。

* 接続セッションは受信行を一度だけ純粋パーサーへ通し、型でログインを進める。
* プレイヤー状態機械は型付きsetup、play、genmove、gameover、infoを受け取る。
* setupの数値検査、ランク除去、棋歴の交互色復元はFormalAdapterが所有する。
* identity、資格情報、着手、解析付き着手、ready、quit、who、matchは型付きフォーマッターを使う。
* Hostのコマンドライン、TCP、エンジン子プロセス、ログ、実行ファイル名は変更していない。

専用`KifuwarabeGo2026.Tests.FormalAdapter.Cgos`を追加し、匿名ベースラインと不正入力をネットワークなしで検査しました。

検証結果：

* ソリューション全体Releaseビルド：警告0件、エラー0件。
* CGOS純粋プロトコル専用試験：`PASS`。
* 実CGOS Hostの`--help`、GTP・CGOS・SGFベースライン、所有権検査を含むGUI移植性試験：`PASS`。
* Windows非対話プラットフォーム試験：`PASS`。

### 作業段階5：CGOS Hostを薄くする

状態：接続セッションとプレイヤー状態機械実装完了・Hostエンジン適合前（2026年8月29日）

`CgosConnectionSession`、`CgosClient`、`CgosAdminClient`を役割別にライブラリーへ移し、Hostにはコマンドライン、標準入出力、プロセス寿命、構成だけを残します。GTP子プロセス処理は`FormalAdapter.Gtp`を利用する構成点にします。

完了条件：既存Hostの実行ファイル名、オプション、発行場所を維持し、Hostが新ライブラリーを組み立てるだけになる。

#### 第1縦切りの実施記録

`KifuwarabeGo2026.FormalAdapter.Cgos.Client`へ次を実装しました。

* `CgosConnectionOptions`：ホスト、ポート、接続タイムアウト、最初のサーバー行タイムアウト。
* `CgosCredentials`：セッションへ注入する利用者名とパスワード。保存方法は所有しない。
* `CgosNetworkSession`：TCP接続、UTF-8行送受信、ログイン交換、解析能力、型付きメッセージ通知、機密ログ、終了時quit。

現行Hostのプレイヤー接続と管理接続を`CgosNetworkSession`利用へ切り替えました。Hostはコマンドライン設定とアカウントを新しい接続設定へ写し、型付きメッセージのコールバックを渡します。ネットワークセッションはHostのオプション型、ログファイル、GTPエンジン、GUIへ依存しません。

`Tests.FormalAdapter.Cgos`へループバックTCP縦試験を追加しました。外部ネットワークを使わず、protocol、username、password、okの交換、`genmove_analyze`能力、パスワード送信通知、ログへの資格情報非露出を検査して`PASS`しました。接続後の全ソリューションReleaseビルドも警告0件、エラー0件です。

#### 第2縦切りの実施記録

`KifuwarabeGo2026.FormalAdapter.Cgos.PlayerEngine`へ次を実装しました。

* `ICgosPlayerEngine`：盤設定、棋歴・相手着手、通常／解析付き着手生成、プロセス寿命を抽象化。
* `CgosPlayerEngineFactory`と`CgosPlayerEngineSetup`：対局ごとのエンジン構成点。
* `CgosGeneratedMove`：頂点と任意の解析JSON。
* `CgosPlayerStateMachine`：setup、棋歴再現、play、genmove、解析能力の合意、人間着手、投了要求、gameover、ready。

状態機械はTCP、標準入出力、具体的GTPプロセス、Host設定へ依存しません。偽エンジン試験で、9路・コミ設定、棋歴再現、相手着手、解析付き着手、投了優先、gameover時のエンジン破棄とreadyを検査して`PASS`しました。

#### 第3縦切りの実施記録

Hostに`CgosGtpPlayerEngineAdapter`を追加し、既存の`GtpEngineProcess`を`ICgosPlayerEngine`へ適合しました。エンジンの起動とGUIオプション適用はHostの構成責務として維持し、盤設定、棋歴再現、通常／解析付き着手生成、破棄はFormalAdapterの状態機械から抽象越しに呼び出します。

現行`CgosClient`からsetup、play、genmove、gameoverの分岐と対局状態を除き、`CgosPlayerStateMachine`へ切り替えました。人間着手とGUI投了要求もデリゲートとして注入するため、状態機械はGUI型へ依存しません。エンジン生成途中の失敗時にも子プロセスを破棄します。

全ソリューションReleaseビルドは警告0件、エラー0件です。`Tests.FormalAdapter.Cgos`、GTP・CGOS・SGF所有権を含むGUI移植性試験、Windows非対話プラットフォーム試験がすべて`PASS`しました。

#### 第4縦切りの実施記録

`FormalAdapter.Cgos.GameMasterEngine`へ`CgosAdminStateMachine`を追加しました。ログイン受理後の準備状態と、`who`、`match`、`quit`入力から型付きCGOSコマンドへの変換を所有します。Hostの`CgosAdminClient`は標準入力監視、ログ、送信だけを担当します。専用試験へログイン前拒否、準備遷移、管理コマンド変換、未知入力拒否を追加して`PASS`しました。

#### 第5縦切りの実施記録

Host内で新しい`CgosNetworkSession`だけが接続を所有していることを参照検索で確認し、未使用になった旧`CgosConnectionSession`と専用`CgosTcpConnector`を物理削除しました。Hostから不要になったネットワーク名前空間の参照も除去しました。

第5段階の最終検証として、全ソリューションReleaseビルドは警告0件、エラー0件でした。CGOS専用試験、実CGOS Hostの`--help`起動、GTP・CGOS・SGF所有権を含むGUI移植性試験、Windows非対話プラットフォーム試験、Board Editor・Review・Match Play Roomの正常／異常終了試験がすべて`PASS`しました。

これにより作業段階5を完了とします。次は作業段階6として、CGOS Hostの人間向けログをGUIが再解析している境界を、型付き通知またはJSON Linesへ置換します。

### 作業段階6：CGOSとGUIのログ境界を置換する

人間向けログの再解析を廃止し、型付き通知またはJSON Linesでsetup、play、analysis、gameover、診断を分けます。`CgosGameObservation`からCGOS字句解析を除き、Go向け投影を`FormalAdapter.Cgos.Go`へ移します。

#### 開始時の再調査記録

現在の境界は次の2系統が同じ標準出力文字列へ依存しています。

* `CgosConnectionProcess.DeriveRunningStatus`が、接続、ログイン、setup、play、genmove、gameover、異常を部分文字列検索してプロセス状態を決定する。
* `CgosGameObservation.ProcessLogLine`が、`"] > "`以降のCGOSサーバー原文と`"] # Generated "`以降のHost独自行を再解析し、setup、相手着手、自分の着手と解析JSON、gameoverを盤面へ反映する。

最初の縦切りでは、既存の人間向け表示を残したまま、Hostが標準出力へ識別可能なversion 1 JSON Lines通知を併記します。通知DTOと読み書きは`FormalAdapter.Cgos`に置き、GUIは通知を優先し旧ログを互換入力として残します。setup、play、generated move、gameoverから始め、接続診断と実行状態は次の縦切りで分離します。これにより表示、棋譜保存、練習相手の重複抑止を一度に壊さず移行できます。

#### 第1縦切りの実施記録

`FormalAdapter.Cgos.Observability`へ、`CgosSetupNotification`、`CgosPlayNotification`、`CgosGameOverNotification`と`CgosNotificationJsonLines`を追加しました。各行は`@kifuwarabe-cgos-v1 `接頭辞、`version: 1`、`kind`を持ち、setupの棋歴、着手後の残り時間、任意の解析JSONを保持します。不正JSON、未知kind、通常の人間向けログは通知として受理しません。

CGOS Hostは状態機械の処理結果からsetup、相手着手、自分の通常／解析付き着手、gameover通知を標準出力へ発行します。GUI Coreは`FormalAdapter.Cgos`を直接参照し、`CgosGameObservation`が通知を盤面へ適用します。最初の構造化通知を受信した後は旧ログのCGOS字句解析を停止するため、併記された着手を二重適用しません。通知未対応の旧Hostに対するログ互換入力は残しています。

専用試験へ通知の往復、棋歴と解析JSONの保持、不正入力拒否を追加しました。GUI移植性試験へ、構造化setup、旧ログ抑止、構造化play、gameoverの一連の観戦状態試験と通知型の所有権検査を追加しました。

次の縦切りでは、接続、ログイン、GTP待機、異常、終了の実行状態と診断を型付き通知へ追加し、`CgosConnectionProcess.DeriveRunningStatus`の人間向け部分文字列依存を置換します。

#### 第2縦切りの実施記録

`CgosRuntimeNotification`と`CgosRuntimeState`をversion 1通知へ追加しました。接続中、TCP接続済み、プロトコル交換、ログイン、準備完了、GTP応答待機、通常実行、切断、異常を区別し、任意の詳細を保持します。`CgosPlayNotification`には自分で生成した着手かを示す`IsGenerated`を追加し、GUIの`PLAY`と`GENMOVE DONE`を文字列推測なしで区別します。

`CgosNetworkSession`は`CgosNetworkEvent`で接続ライフサイクルを報告します。Hostはプレイヤーと管理者の両経路でこれをruntime JSON Linesへ投影し、GTPプロセスの応答待機開始／完了と例外も通知します。パスワードは通知詳細へ含めません。

GUIの`CgosConnectionProcess`は構造化通知から実行状態とGTP待機時計を更新します。新Hostからruntime通知を一度受けた後は、人間向けGTP進捗ログを状態判定に使用しません。旧Hostとの互換性のため、通知が一度も来ない場合だけ`DeriveRunningStatus`をフォールバックとして残しています。

専用試験へruntime通知の往復と、ループバックTCP上の`Connecting → Connected → Protocol → Login → Ready → Closed`イベント順序を追加しました。

次の縦切りでは、`CgosGameObservation`の囲碁向け状態投影を`FormalAdapter.Cgos.Go`へ分離し、GUIからCGOS固有通知の意味変換を減らします。その後、旧Host互換ログ解析を削除できる条件を整理します。

#### 第3縦切りの実施記録

`FormalAdapter.Cgos.Go`へ`CgosGoEventProjector`と中立な`CgosGoSetup`、`CgosGoMove`、`CgosGoGameOver`、`CgosGoColor`、`CgosGoVertex`を追加しました。プロジェクターはsetup後の盤サイズを保持し、CGOSの色表現、パス、I列を飛ばすGTP頂点、盤外座標、棋歴を検証して0始まりの囲碁座標へ変換します。GUI、共有盤面型、描画、ファイルシステムには依存しません。

GUIの構造化通知経路は、通知を直接switchせず`CgosGoEventProjector`を通すよう変更しました。`CgosGameObservation`は中立な囲碁イベントを既存盤面へ写像するだけになり、新経路ではCGOS色文字列とGTP座標を解釈しません。旧Host互換ログ経路とGUIから送る人間着手の座標処理は、互換境界として残しています。

専用試験へ棋歴座標、I列スキップ、パス、色変換、不正色拒否を追加し、GUI所有権試験へ`CgosGoEventProjector`を追加しました。

次の縦切りでは、旧Host互換ログ解析を専用の互換アダプターへ隔離し、通常の`CgosGameObservation`から`ProcessServerCommand`、CGOS色解析、setup字句解析を除去します。

#### 第4縦切りの実施記録

`FormalAdapter.Cgos.Compatibility`へ`CgosLegacyLogNotificationAdapter`を追加しました。JSON Lines導入前のHost表示ログにあるsetup、play、generated move、gameoverを現行の型付き通知へ変換します。旧表示で使われた`black`／`white`色名も、この互換境界だけで正式なCGOS色へ正規化します。

`CgosGameObservation`から`ProcessServerCommand`、旧setup字句解析、CGOS色文字列解析を物理削除しました。構造化通知と旧ログはどちらもFormalAdapterの通知と囲碁イベントを経由し、GUIは一つのイベント適用経路だけを持ちます。

既存のCGOS結果レビュー、人間着手反映、練習対局状態を含むGUI移植性試験を通し、専用試験へ旧setupと解析付きgenerated moveの互換変換を追加しました。

次の縦切りでは、`CgosConnectionProcess.DeriveRunningStatus`に残した旧Host状態ログ解析も互換境界へ移し、GUIから人間向けCGOSログの意味解析を完全に除去します。そのうえで第6段階の最終回帰試験を行います。

#### 第5縦切りの実施記録

`FormalAdapter.Cgos.Compatibility`へ`CgosLegacyRuntimeLogAdapter`を追加しました。旧Hostログから実行状態、GTP待機開始／完了、Admin待機者を抽出する知識を集約し、中立な`CgosLegacyProcessState`と`CgosLegacyGtpWaitTransition`を返します。

GUIの`CgosConnectionProcess`から、CGOSエラー、接続、ログイン、setup、play、genmove、gameover、Admin、GTP待機に関する部分文字列検索を物理削除しました。GUIは構造化通知を通常経路とし、旧Hostの場合だけ互換アダプターの結果を表示文字列へ写像します。人間向けログ自体は閲覧・保存用に従来どおり残します。

全ソリューションReleaseビルドとFormalAdapter.Cgos専用試験は警告0件、エラー0件、`PASS`です。GUI移植性、Windows非対話、PlayRoom回帰試験は、生成DLLがWindowsアプリケーション制御ポリシーの`0x800711C7`で拒否され、対象再ビルド後の再試行でも起動前に停止しました。コード失敗ではないものの、第6段階の完了判定はこれらの再実行まで保留します。

完了条件：標準出力には機械向け通知だけ、標準エラーには診断だけが流れ、GUI表示と棋譜保存が従来どおり動く。Host異常終了でもGUIが復帰できる。

### 作業段階7：旧配置を整理する

利用側、テスト、発行スクリプト、開発者文書を新ProjectFamilyへ追随させます。履歴文書内の過去名称は書き換えません。互換Host、実行ファイル名、保存形式を残す必要性を再評価します。

完了条件：外部仕様固有の型がカジュアル・コアへ漏れず、旧プロジェクトに実装の重複が残らない。

## 優先順位

| 優先度 | 対象 | 理由 |
|---|---|---|
| 1 | GTP Protocolプリミティブ | すでに小さく分かれ、依存が明瞭で移行効果を検証しやすい |
| 2 | SGF文書モデル | GUIにある大きな混在を解消し、Review、CGOS、GTP初期局面から共用できる |
| 3 | CGOS純粋パーサー | 1,300行Hostと360行観測状態の間に安定境界を作れる |
| 4 | GTP Protocol P・サーバー変換 | 互換実行ファイルと多数の利用側があり、プリミティブ移行後が安全 |
| 5 | CGOS Host・GUI通知境界 | 実ネットワーク、子プロセス、GUI、棋譜にまたがるため最後に縦移行する |

## リスクと対策

| リスク | 対策 |
|---|---|
| 名前だけ移して責務が混ざったままになる | 純粋パーサー、意味変換、Host、GUIを別々の完了条件で扱う |
| 旧名前空間の一括変更で差分が肥大化する | 一段階につき一境界だけ移し、必要なら期限付き互換層を置く |
| GTPとSGFの循環参照 | SGF文書モデルをGTP非依存にし、GTP側がSGFを参照する一方向にする |
| CGOSとGTPが一つのHostで再結合する | Hostを構成点とし、`FormalAdapter.Cgos`と`FormalAdapter.Gtp`同士を直接参照させない |
| GUI互換型がFormalAdapterへ流入する | 中立な契約または変換DTOを先に定め、GUI型を参照しない検査を追加する |
| 人間向けログ変更でGUIが壊れる | 機械向け通知と診断ログを分離し、旧ログ境界の縦試験を置いてから切り替える |
| SGFの未知情報が失われる | 文書モデルの損失なし往復を、GoGameRecordへの縮約より先に完成させる |

## Protocolプリミティブ移行で変更しなかったもの

* GTP、CGOSの実行ファイル名と発行物。
* SGFの保存形式、保存場所、レビュー画面。
* USI、CSA、KIFのプレースホルダーと将来実装。

## 実装再開地点

```text
現在の状態：作業段階0～5完了。作業段階6の実装完了、最終回帰試験はWindowsアプリケーション制御解除待ち
次の最小作業：GUI移植性、Windows非対話、PlayRoom回帰試験を再実行して第6段階を完了判定する
次の実装候補：第6段階の最終回帰試験
移行先：KifuwarabeGo2026.FormalAdapter.Cgos.Observability、FormalAdapter.Cgos.Go、Host構成点、GUI受信境界
禁止事項：GTPプロジェクト全体の一括改名、CGOS Hostの一括分解、SgfGameRecordConverterの型ごとの単純移動を同時に行わない
```
