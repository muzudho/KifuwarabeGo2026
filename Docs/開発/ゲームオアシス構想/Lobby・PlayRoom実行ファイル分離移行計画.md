# Lobby・Play Room実行ファイル分離移行計画

## 文書の位置づけ

この文書は、現在一つのGUI実行ファイルに同居しているLobby相当画面とPlay Room相当画面を、独立した実行ファイルへ段階的に分離する移行計画です。

最終目的は、Kifuwarabe Go 2026本体のソースコードを変更しなくても、外部の開発者が次のいずれか一つだけを実装してGame Oasisへ参加できる状態を作ることです。

- Lobbyだけを開発する
- Play Roomだけを開発する
- PlaySpaceだけを開発する
- PlayerまたはGame Masterだけを開発する

この計画は将来構想であり、現行の`KifuwarabeGo2026.GameOasis.Gui.Windows.exe`を直ちに削除、改名、分割する指示ではありません。各段階で既存経路を動作させたまま、新経路を縦方向に完成させてから切り替えます。

本計画は[`v4.0.0プロジェクト再構成計画.md`](./v4.0.0プロジェクト再構成計画.md)を補足します。同計画にある「GUIを備えたゲームスペース実行ファイル」という表現は、本計画では次の二つへ分けて扱います。

- `Play Room`: 利用者へ盤、操作、レビュー、編集画面を提供するGUIプロセス
- `PlaySpace`: ゲームルール、ゲーム状態、行動適用、終局判定を所有するモデルプロセス

## 目標

### 利用者から見た目標

利用者はLobbyで遊ぶ内容、参加者、ルール、開始局面を選び、`START`または`EDIT BOARD`から目的のPlay Roomへ入ります。プロセスが分かれていることを意識しなくても、現在と同じ一続きの操作として利用できます。

```text
［Lobby］
├─ START ──────> ［Match Play Room］
└─ EDIT BOARD ─> ［Board Editor Play Room］
```

### 開発者から見た目標

外部実装者は、リポジトリー内部のGUIクラス、MonoGame型、`GoAppSession`、Concierge具象クラスを参照しません。公開Contracts、直列化仕様、プロセスライフサイクル仕様、適合性テストだけを使って実装します。

| 開発対象 | 実装者が知る境界 | 知らなくてよいもの |
|---|---|---|
| Lobby | Protocol GのLobby能力、カタログ、入室要求 | PlaySpace具象型、盤描画、ゲームルール |
| Play Room | Protocol GのPlay Room能力、入室チケット、表示状態、意味操作 | Lobbyの画面構成、Concierge具象型、PlaySpace内部状態 |
| PlaySpace | Protocol S、設定・状態・行動スキーマ | Lobby、Play Room、MonoGame、Player実装 |
| Player | Protocol P、公開観測状態 | Lobby、Play Room、PlaySpace具象型 |
| Game Master | Protocol M、運営状態と命令 | Lobby、Play Room、PlaySpace具象型 |

### 成功条件

次をすべて満たしたとき、分離完了とします。

1. `KifuwarabeGo2026Lobby.exe`を単独で置き換えられる。
2. `KifuwarabeGo2026PlayRoom.exe`を単独で置き換えられる。
3. LobbyとPlay Roomが互いのアセンブリを参照しない。
4. 両方がConciergeの具象アセンブリを参照せず、公開Protocol Gクライアントだけを使用する。
5. Play RoomがPlaySpace具象実装を参照せず、ゲーム固有文書または登録済み表示アダプターだけを扱う。
6. 標準入出力の切断、子プロセス異常終了、タイムアウトからLobbyへ安全に戻れる。
7. 公式実装以外のLobbyまたはPlay Roomをマニフェスト登録し、同じ適合性テストへ合格させられる。
8. 現行設定、SGF、エンジンプロフィール、Launcher、公開リリース資産の移行経路がある。

## 対象外

最初の分離では、次を同時に完成させません。

- ネットワーク越しの分散実行
- 未信頼コードを完全に隔離するサンドボックス
- Protocol G/P/M/Sすべてのバイナリ通信
- 複数端末から同じセッションへ接続する観戦配信
- 現行GUIの全面的なデザイン変更
- 既存SGF形式の変更
- 最初の段階での旧GUI実行ファイル削除

これらを先に含めると、Exe分離、公開契約、画面移行、配布方式を同時に変更することになります。まず同一PC上のローカルプロセス間通信を完成させます。

## 用語と責務

| 表示名 | 内部名 | 将来の既定実行ファイル |
|---|---|---|
| Launcher | Launcher | `KifuwarabeGo2026Launcher.exe` |
| Lobby | GUIクライアントのLobby能力 | `KifuwarabeGo2026Lobby.exe` |
| Game Coordinator | Concierge | `KifuwarabeGo2026Concierge.exe` |
| Play Room | GUIクライアントのPlay Room能力 | `KifuwarabeGo2026PlayRoom.exe` |
| Go PlaySpace | PlaySpace Engine | `KifuwarabeGo2026PlaySpace.Go.exe` |

`KifuwarabeGo`は`Kifuwarabe Game Oasis`の略称を兼ねるため、`KifuwarabeGo2026GameOasis.exe`は使用しません。ConciergeはPlay RoomだけでなくLobby、Player、Game Master、PlaySpaceを仲介するため、`KifuwarabeGo2026PlayRoomConcierge.exe`も使用しません。

## 目標プロセス構成

### 起動と所有

標準入力と標準出力は、原則として親プロセスが子プロセスを起動して所有します。既定構成ではLauncherがConciergeを起動し、ConciergeがLobby、Play Room、PlaySpaceを起動します。

```text
KifuwarabeGo2026Launcher.exe
└─ KifuwarabeGo2026Concierge.exe
   ├─ KifuwarabeGo2026Lobby.exe
   ├─ KifuwarabeGo2026PlayRoom.exe
   └─ KifuwarabeGo2026PlaySpace.Go.exe
```

Launcherを終了した後もConciergeが動作を継続する構成を許可します。Conciergeは自分が起動した子プロセス、セッション、終了順序を管理します。LobbyがPlay RoomやPlaySpaceを直接起動してはいけません。

### 通信境界

```text
Lobby.exe ───── Protocol Gui-Concierge-Presentation ────┐
PlayRoom.exe ── Protocol Gui-Concierge-Presentation ────┤
Player ──────── Protocol Concierge-Player-Turn ─────────┼─ Concierge.exe
Game Master ─── Protocol GameMaster-Concierge-Operations┘        │
                                                                 │
                                  Protocol Concierge-PlaySpace-GameState
                                                                 │
                                                                 ▼
                                                          PlaySpace.exe
```

LobbyとPlay Roomは直接通信しません。Lobbyが作成した入室要求、選択した設定、棋譜、局面はConciergeが所有する不透明な入室チケットを介してPlay Roomへ渡します。

### 標準入出力の位置づけ

```text
Protocol = メッセージの意味、順序、エラー、ライフサイクル
JSON Lines = 1メッセージを1行で表す直列化形式
stdin/stdout = メッセージを運ぶ経路
stderr = 人間向け診断。ただしプロトコル応答には使用しない
```

標準出力にはプロトコルメッセージ以外を出しません。ログは標準エラーまたは明示されたログファイルへ出します。すべての要求と応答はメッセージIDを持ち、同じIDで対応付けます。

## LobbyとPlay Roomの公開能力

### Lobby能力

Lobby実装は、少なくとも次の意味操作を提供します。

- Conciergeとのハンドシェイク
- Lobby実装ID、表示名、バージョン、対応Protocol G版の申告
- PlaySpace、Player、Game Master、保存済み設定の一覧表示
- ゲーム設定文書の選択または編集
- SGFや局面文書の読込要求
- Match Play Roomへの入室要求
- Board Editor Play Roomへの入室要求
- 入室準備中、成功、失敗の表示
- Play Roomから返された採用局面、棋譜、結果の受領
- 利用者によるLobby終了要求

Lobbyは盤を描画せず、合法手判定やゲーム状態の正本を所有しません。

### Play Room能力

Play Room実装は起動時に、自分が対応する部屋種別と表示能力を申告します。

最初の公式実装は一つの`KifuwarabeGo2026PlayRoom.exe`で複数の部屋種別を扱います。

```text
match
board-editor
review
```

最低限必要な意味操作は次のとおりです。

- Conciergeとのハンドシェイク
- Play Room実装ID、表示名、バージョン、対応Protocol G版の申告
- 入室チケットの受領とセッションへの接続
- 最新表示状態または差分イベントの取得
- 着手、パス、投了など、申告能力に対応する意味操作の送信
- 編集局面の採用または破棄
- 棋譜レビューの移動、局面採用
- セッション終了またはLobbyへ戻る要求
- 通信切断時の安全なエラー表示

Play RoomはLobbyの設定画面を複製せず、PlaySpaceのゲーム状態を独自に正本化しません。

### 入室チケット

LobbyからPlay Roomへ、プロセス引数で棋譜本文、パスワード、巨大なJSONを直接渡しません。Conciergeが短命な不透明チケットを発行します。

```text
RoomTicketId
RoomType
GameOasisSessionId または編集ワークスペースID
RequestedGuiCapabilities
ExpiresAt
SingleUse
```

Play RoomはチケットをProtocol Gで引き換え、必要な表示状態を取得します。チケットは別プロセスからの再利用、期限切れ、部屋種別不一致を拒否します。

## 棋譜と局面の受け渡し

画面間の通常遷移では、一時SGFファイルを必須にしません。Conciergeが次の構造化文書を保持し、不透明IDで受け渡します。

- Game record document: 初期配置、着手履歴、結果、コメント
- Position document: 盤サイズ、黒白配置、手番、出典
- Room result document: 採用、破棄、保存、対局結果

SGFは利用者が保存、読込、外部交換を選んだ場合に使用します。Board Editor Play Roomは元文書のコピーを編集し、`ADOPT`が成功した場合だけ新しいPosition documentをLobbyへ返します。`DISCARD`ではワークスペースだけを破棄します。

## 外部実装の登録

### マニフェスト

LobbyとPlay Roomは、実行ファイルをハードコードせず、マニフェストから登録します。最小項目は次のとおりです。

```json
{
  "manifestVersion": 1,
  "componentId": "io.example.my-play-room",
  "componentKind": "play-room",
  "displayName": "Example Play Room",
  "version": "1.0.0",
  "executable": "ExamplePlayRoom.exe",
  "protocols": {
    "guiConciergePresentation": "1.0"
  },
  "capabilities": ["room.match", "room.board-editor"],
  "platforms": ["win-x64"]
}
```

実際のキー名とスキーマIDは、実装前の契約決定段階で固定します。実行ファイルの相対パスはマニフェスト配置場所から解決し、ルート外参照、未知の必須項目、重複component IDを拒否します。

### 独立開発者向け成果物

外部開発者がリポジトリー全体を参照しなくても参加できるよう、次を公開します。

- バージョン付きContractsパッケージ
- JSON Schemaとサンプルメッセージ
- 標準入出力ホスト／クライアントの小さなSDK
- Lobby最小実装
- Play Room最小実装
- 偽Conciergeを使う適合性テストランナー
- 正常終了、拒否、タイムアウト、切断、再接続の試験シナリオ
- マニフェストスキーマとパッケージ例
- 互換性表と廃止予定一覧

公式MonoGame UIやStationeryUIへの依存は任意とします。外部実装者はWinForms、WPF、Avalonia、SDL、WebViewなど、別の表示技術を選べます。

## 将来のプロジェクト構成案

名前は実装段階で最終決定しますが、責務の配置先は次のように分けます。

```text
KifuwarabeGo2026.GameOasis.Contracts
KifuwarabeGo2026.GameOasis.Transport.Text
KifuwarabeGo2026.GameOasis.Concierge
KifuwarabeGo2026.GameOasis.Concierge.Host

KifuwarabeGo2026.Lobby
KifuwarabeGo2026.Lobby.Windows

KifuwarabeGo2026.PlayRoom
KifuwarabeGo2026.PlayRoom.Windows

KifuwarabeGo2026.Reference.Gui
KifuwarabeGo2026.Reference.PlaySpace.Go
KifuwarabeGo2026.Reference.PlaySpace.Go.Host
```

依存方向は次を守ります。

```text
Lobby ────────> Contracts / Protocol G client SDK
Play Room ────> Contracts / Protocol G client SDK
Concierge ────> Contracts
PlaySpace ────> Contracts
Transport.Text -> Contracts

Lobby ──×──> Play Room
Lobby ──×──> PlaySpace実装
Play Room ──×──> Lobby
Play Room ──×──> Concierge具象実装
Concierge ──×──> MonoGame / Windows Forms
Contracts ──×──> GUI / Storage / ゲーム固有実装
```

## 現状と主な分離対象

現在は`KifuwarabeGo2026.GameOasis.Gui`が、次の責務を同時に参照しています。

- MonoGame／StationeryUIによるLobbyとPlay Roomの描画
- `Game1`による画面遷移とプロセス全体の入力制御
- `GoAppSession`によるLobby設定、対局表示、編集、レビュー状態
- `GameOasisGuiComposition`によるConciergeとPlaySpace参照実装のインプロセス生成
- GTPプレイヤープロセスの起動とProtocol P接続
- Storage、Launcher、CGOSなどの構成

最初からファイルをフォルダー単位で移動してはいけません。先に公開契約と状態所有者を決め、利用側をインターフェースへ切り替えてから、最後にプロジェクトを移します。

| 現在の主な場所 | 将来の責務 |
|---|---|
| `Presentation/Pages/Title` | Lobby |
| `Presentation/Pages/LocalMatch/Intermission` | Lobby |
| `Presentation/Pages/EditTournamentRule` | Lobbyまたはゲーム設定プラグイン |
| `Presentation/Pages/Board` | Play Roomの共通盤表示 |
| `Presentation/Pages/LocalMatch/Play` | Match Play Room |
| `Presentation/Pages/BoardAndReview` | Board Editor／Review Play Room |
| `Application/Local/Playing` | ConciergeクライアントとPlay Room表示状態へ分解 |
| `Application/Sessions` | Lobby draft、Room view、Concierge sessionへ状態を分割 |
| `Application/GameOasis/GameOasisGuiComposition` | Concierge Hostの構成点へ移動 |

## 移行原則

1. **契約を先に作る。** ファイル移動やExe作成より先に、意味操作、状態、エラーをContractsへ定義します。
2. **正本を一つにする。** ゲーム状態はPlaySpace、運営状態と入室チケットはConcierge、Lobbyの未確定入力はLobby、描画中の一時状態はPlay Roomが所有します。
3. **インプロセスと別プロセスを同じ契約で動かす。** 先にインプロセスアダプターで挙動を固定し、その後Transport.Textへ差し替えます。
4. **縦方向に一部屋ずつ移す。** Board Editor、Review、Matchの順に、起動から終了まで一本完成させます。
5. **旧経路を切り替え可能に保つ。** 段階中は設定または開発フラグで旧単一Exeへ戻せるようにします。
6. **ファイル共有を隠れたAPIにしない。** 設定ディレクトリや一時ファイルを監視して画面間連携しません。
7. **プロセス異常を通常状態として設計する。** 起動失敗、無応答、壊れたJSON、突然終了を、例外的な未定義挙動にしません。
8. **公開境界にGUI技術を含めない。** MonoGameの`Point`、`Color`、`Texture2D`、フレーム更新型をContractsへ入れません。

## 段階的移行計画

### 第0段階: 設計決定記録と現状固定

#### 作業

- Lobby、Play Room、Concierge、PlaySpaceの状態所有表を確定する。
- Protocol Gの現行操作と不足操作を一覧化する。
- 現行のLobbyからPlay Roomへ渡している値を記録する。
- ローカル対局、盤面編集、レビュー、CGOS、ポン抜きの画面遷移特性テストを追加する。
- 現行実行ファイル、リリースZIP、設定保存場所、ログ場所を記録する。
- 本計画とv4.0.0計画の用語差分を設計決定記録へ残す。

#### 完了条件

- 状態ごとの唯一の所有者が表で説明できる。
- 現行の主要画面遷移とデータ受け渡しを自動試験または手動試験表で再現できる。
- 分離作業前のReleaseビルドと既存全スモークがPASSする。

### 第1段階: Protocol G vNextの役割分離

#### 作業

- GUIクライアント記述子へ`lobby`、`play-room`の役割と能力を追加する。
- Lobby向けのカタログ、設定選択、入室要求契約を追加する。
- Play Room向けの入室チケット引換、attach、detach、room result契約を追加する。
- Match、Board Editor、Reviewのroom typeを安定IDとして定義する。
- Game record、Position、Room resultの自己記述文書とスキーマを追加する。
- エラーコード、メッセージID、リビジョン、冪等性規則を確定する。
- Protocol G v1クライアントをvNextへ適合する互換アダプターを作る。

#### 完了条件

- Contractsだけを参照する偽Lobbyと偽Play Roomが、インメモリConciergeを介して入室と帰室を完了する。
- LobbyとPlay Roomが互いの型を参照しない。
- 古いProtocol G試験が互換アダプター経由でPASSする。

### 第2段階: 共通テキストトランスポート

#### 作業

- JSON Linesのエンベロープ、最大メッセージ長、UTF-8、改行、終了手順を仕様化する。
- request、response、event、cancel、goodbyeを区別する。
- メッセージID、protocol version、component ID、correlation IDを定義する。
- 標準出力汚染、未知メッセージ、重複ID、応答タイムアウトを検出する。
- 標準エラーのログ取り込みとファイルログの相関IDを実装する。
- インプロセス実装と標準入出力実装へ同じ契約テストを適用する。

#### 完了条件

- テスト用子プロセスを使い、Lobby、Play Room、PlaySpaceの各役を往復できる。
- 壊れたJSON、無応答、途中終了、大きすぎるメッセージを安全に失敗として返す。
- stdoutへ診断文字列が混入した試験を失敗として検出する。

### 第3段階: Concierge Hostの独立

#### 作業

- `GameOasisGuiComposition`の組み立て責務を`Concierge.Host`へ移す。
- Concierge HostがLobby、Play Room、PlaySpaceのマニフェストを読み込む。
- 子プロセスの起動、標準入出力、終了、タイムアウト、ログを一元管理する。
- 入室チケットと編集ワークスペースの所有を実装する。
- LauncherからConcierge Hostを起動する経路を追加する。
- 現行GUIを一つのLegacy GUIクライアントとして接続するアダプターを用意する。

#### 完了条件

- Concierge Hostを別プロセスにしても、現行GUIがProtocol G経由で通常囲碁セッションを開閉できる。
- Conciergeが終了すると、所有する子プロセスとセッションが定義順序で終了する。
- 現行インプロセス構成へ戻す開発用切替が機能する。

### 第4段階: Lobby状態の抽出

#### 作業

- `Title`、Local MatchのResting／Intermission、プレイヤー選択、ルール選択、設定をLobby側へ分類する。
- `GoAppSession`からLobby draftを独立したアプリケーションモデルへ移す。
- `START`を画面モード変更からMatch room入室要求へ変更する。
- `EDIT BOARD`を画面モード変更からBoard Editor room入室要求へ変更する。
- 入室中の二重押下防止、進捗、キャンセル、失敗時のLobby復帰を実装する。
- 公式Lobby CoreとWindows Hostを作る。

#### 完了条件

- `KifuwarabeGo2026Lobby.exe`だけでカタログ表示、設定選択、二種類の入室要求を送信できる。
- LobbyプロジェクトがPlay Room、PlaySpace、MonoGame盤描画クラスを参照しない。
- Play Roomが存在しない、起動できない、互換性がない場合にLobbyへ理由を表示する。

### 第5段階: Board Editor Play Roomの抽出

Board Editorを最初に選ぶ理由は、ライブ時計、Player手番要求、終局処理を必要とせず、局面コピー、採用、破棄という部屋境界を小さく検証できるためです。

#### 作業

- Board Rendererと編集操作をPlay Room Coreへ移す。
- Position documentから独立編集ワークスペースを生成する。
- `ADOPT`、`DISCARD`、保存のRoom resultを実装する。
- Lobbyから`EDIT BOARD`で起動し、採用局面をLobbyへ戻す。
- 元局面、元棋譜、対局状態を変更しない試験を追加する。

#### 完了条件

- LobbyとBoard Editorが別Exeで動作する。
- 一時ファイルなしで局面を渡し、採用または破棄してLobbyへ戻れる。
- Board Editorだけを偽Lobby／偽Conciergeで適合性試験できる。

### 第6段階: Review Play Roomの抽出

#### 作業

- 棋譜タイムライン、コメント、結果位置、チャート表示をReview roomへ移す。
- Game record documentを読み取り専用の正本として接続する。
- `USE POSITION`でPosition documentのコピーを返す。
- ReviewからBoard Editorへの移動もConciergeの新しい入室チケットを介す。
- 未保存コメントの確認と保存要求の所有者を決める。

#### 完了条件

- SGF読込、レビュー、局面採用が別Play Roomプロセスで完了する。
- ReviewまたはBoard Editorの異常終了が元棋譜を破損しない。

### 第7段階: Match Play Roomの抽出

#### 作業

- 対局盤、時計表示、手番表示、パス、投了、停止、再開をMatch roomへ移す。
- Play RoomはProtocol Gの表示状態だけを正本として描画する。
- 人間入力は意味操作としてConciergeへ送り、ConciergeがProtocol Sへ適用する。
- コンピューター手番はProtocol P、運営操作はProtocol Mを維持する。
- 着手音、画面更新、リプレイ、終局、結果、保存をプロセス境界越しに検証する。
- Lobbyへ戻る前にPlayer、Game Master、PlaySpaceを正しい順序で終了する。

#### 完了条件

- 人間対人間、人間対コンピューター、コンピューター対人間、コンピューター対コンピューターが別Play Room Exeで完走する。
- ゲーム状態の正本がPlaySpaceだけであり、Play Roomに第二の対局状態機械がない。
- Play Roomを強制終了しても、Conciergeがセッションを検出して終了または再接続待ちにできる。

### 第8段階: PlaySpace Hostの独立

#### 作業

- Protocol Sの標準入出力ホストを追加する。
- 通常囲碁とポン抜き参照実装を別プロセスで起動できるようにする。
- `Describe`、設定スキーマ、セッション、行動、状態、終了をテキスト境界で検証する。
- 同一プロセス複数セッション能力と、1プロセス1セッション実装の両方を扱う。
- PlaySpace異常終了時のセッション失敗、Lobby通知、ログ保存を実装する。

#### 完了条件

- ConciergeがPlaySpace具象アセンブリを参照せず、マニフェストとProtocol Sだけで通常囲碁とポン抜きを起動する。
- 外部の最小PlaySpace実装を登録してPlay Roomから操作できる。

### 第9段階: 外部実装SDKと適合性テスト

#### 作業

- Lobby SDK、Play Room SDK、PlaySpace SDKの参照範囲を最小化する。
- 言語非依存のJSON Schema、対話例、状態図を公開する。
- 偽Conciergeを含むコマンドライン適合性ランナーを作る。
- 公式実装をSDK利用者と同じ方法でテストする。
- component ID、署名、配布元、互換Protocol版を表示する診断画面をLobbyへ追加する。
- 外部実装のインストール、無効化、更新、削除の運用をLauncherへ追加する。

#### 完了条件

- 別リポジトリーで作ったサンプルLobbyとサンプルPlay Roomを、公式バイナリの再ビルドなしで登録できる。
- サンプル実装が公式適合性ランナーへ合格する。
- 壊れた外部実装を無効化して公式実装へ戻せる。

### 第10段階: 既定経路の切替と互換移行

#### 作業

- Launcherの既定起動先をConcierge Hostへ変更する。
- Conciergeが公式Lobbyを起動し、入室時に公式Play Roomを起動する。
- 旧単一GUIを互換ホストまたはフォールバックとして一定期間残す。
- 設定、カタログ、SGF保存先、ログ、ショートカットを移行する。
- Release資産、更新、アンインストール、障害報告手順を更新する。
- 十分な移行期間と利用実績の後にだけ旧経路削除を別計画で判断する。

#### 完了条件

- 新規インストールは分離構成で起動する。
- 既存利用者は設定と棋譜を失わず移行できる。
- 旧経路へ戻す手順と、新旧両方のログ採取手順が文書化されている。

## 試験戦略

### 契約試験

- 各Protocol版の正常メッセージと不正メッセージ
- 未知の任意項目を無視し、未知の必須能力を拒否する互換性
- メッセージID、リビジョン、冪等性、キャンセル
- ContractDocumentのMediaType、SchemaId、Content検証

### プロセス試験

- 起動成功、実行ファイル不存在、終了コード異常
- ハンドシェイク前終了、ハンドシェイクタイムアウト
- stdout汚染、壊れたJSON、巨大メッセージ
- 応答中の終了、無限待機、キャンセル無視
- Concierge終了時の子プロセス回収
- Play Room再起動と単一使用チケットの再利用拒否

### 縦方向シナリオ

1. LobbyからBoard Editorへ入り、局面を採用して戻る。
2. LobbyからBoard Editorへ入り、破棄して戻る。
3. LobbyからMatchへ入り、人間対人間を終局して結果を戻す。
4. コンピューター対コンピューターで着手音、棋譜、時計、終局を確認する。
5. Reviewから局面を採用してBoard Editorへ入る。
6. Play Roomを途中終了し、Lobbyへ安全に復帰する。
7. PlaySpaceを途中終了し、Play RoomとLobbyの両方へ原因を表示する。
8. 公式Lobby＋外部Play Room、外部Lobby＋公式Play Roomの組合せを検証する。

### 依存関係試験

ビルド時検査で次を固定します。

- LobbyアセンブリからPlay Room、PlaySpace、Concierge具象実装への参照禁止
- Play RoomアセンブリからLobby、PlaySpace、Concierge具象実装への参照禁止
- ContractsからGUI、Storage、Windows、MonoGame、ゲーム固有実装への参照禁止
- PlaySpaceからGUI、Lobby、Play Roomへの参照禁止

## 互換性とロールバック

各段階は次の3経路を必要な期間だけ併設します。

```text
LegacyInProcess
SeparatedProcessesOfficial
SeparatedProcessesExternal
```

切替は開発設定またはLauncherの診断設定で行い、保存データ形式を分岐させません。新経路で開始に失敗した場合、進行中セッションを暗黙に旧経路へ引き継いではいけません。Lobbyへ戻って理由を表示し、利用者が再開始を選んだ場合だけ旧経路を利用します。

旧コードを削除する条件は、単に新Exeが起動したことではありません。公式の全縦方向シナリオ、外部サンプル、更新・アンインストール、障害復旧が新経路で安定した後に、別の削除計画として判断します。

## 主なリスクと対策

| リスク | 対策 |
|---|---|
| `GoAppSession`をそのまま共有DTOにしてしまう | Lobby draft、Room view、Concierge session、PlaySpace stateへ分解する |
| GUI都合の型がContractsへ流入する | 意味操作と自己記述文書だけを公開し、描画型を禁止する |
| LobbyがPlay Roomを直接起動して密結合する | 起動とチケット発行をConciergeへ一元化する |
| 一時SGFが隠れたIPCになる | 通常遷移は文書ID、利用者の保存・読込だけSGFを使う |
| stdoutへログが混ざり通信が壊れる | stdoutをプロトコル専用、stderrとファイルを診断専用にする |
| 外部実装の無応答で全体が停止する | 操作別タイムアウト、キャンセル、プロセス終了、Lobby復帰を定義する |
| Play Roomがゲーム状態を複製する | 表示投影だけを保持し、行動結果はProtocol Gから再取得する |
| Protocol GがLobby専用とPlay Room専用で肥大化する | 共通エンベロープと役割別能力／サービス面を分ける |
| 公式UIライブラリーが事実上必須になる | Contracts適合性試験にStationeryUIを含めない |
| Exe数増加で利用者が不安になる | ファイル説明、親子関係、診断画面、正常なプロセス一覧を文書化する |
| 更新中に版が混在する | Launcherが互換性表を検証し、一組の原子的なバージョン選択を行う |

## 作業の分担単位

複数の開発者が独立して参加できるよう、実装作業も次の単位へ分けます。

| 担当 | 主な成果物 | 他担当との固定境界 |
|---|---|---|
| Contracts担当 | DTO、Schema、能力ID、互換性規則 | Protocol版と適合性試験 |
| Transport担当 | JSON Lines、標準入出力、タイムアウト、ログ | Contractsエンベロープ |
| Concierge担当 | 登録、起動、チケット、セッション、終了順序 | Protocol G/P/M/S |
| Lobby担当 | カタログ、設定、入室、復帰UI | Protocol G Lobby能力 |
| Play Room担当 | Match、Editor、Review UI | Protocol G Play Room能力 |
| PlaySpace担当 | ルール、状態、行動、終局 | Protocol S |
| Launcher担当 | インストール、更新、構成選択 | マニフェストと互換性表 |
| 適合性試験担当 | 偽相手、異常系、言語非依存例 | 公開仕様のみ |

各担当は他担当の具象プロジェクトを参照せず、公開契約とテストベクトルを介して並行開発します。

## 最初に着手する実装単位

最初の実装PRではExeを分割しません。次だけを行います。

1. LobbyとPlay Roomの状態所有表を作る。
2. 現在の`START`、`EDIT BOARD`、`ADOPT`、`DISCARD`で渡しているデータを型と所有者まで列挙する。
3. Protocol G vNextのLobby／Play Roomクライアント記述子とroom typeをContractsへ追加する。
4. 偽Lobbyと偽Board EditorをインメモリConciergeへ接続する契約試験を追加する。
5. 現行GUIの`EDIT BOARD`を、同一プロセス内の入室要求アダプター経由へ切り替える。

この小さな縦経路が完成するまで、`Game1`の分割、プロジェクトの大量移動、新Exe作成を開始しません。

## 中断・再開地点

作業を途中で中断する場合は、各段階について次を記録します。

- 完了した契約とProtocol版
- 現在有効な経路
- 旧経路へ戻す設定
- 最後にPASSしたビルドと試験
- 次に作る最小の縦方向シナリオ
- 未解決の状態所有または互換性判断

現時点の再開地点は**第0段階: 設計決定記録と現状固定**です。次回は`START`、`EDIT BOARD`、`ADOPT`、`DISCARD`の現行呼び出しとデータ所有者を調査し、状態所有表を作成します。
