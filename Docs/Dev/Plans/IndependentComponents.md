# Lobby・Play Roomの独立実装構想

外部作者がLobby・Play Room・各Engineを独立交換できるようにする長期構想です。旧計画の第0段階を現在の再開地点とは扱いません。[実装済みの境界](../CurrentState/LobbyPlayRoomSeparation.md)と[直近の作業](LobbyRenderer.md)を先に確認します。記載のプロセス名・入室チケットは将来案です。

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

## 元の検討経緯

[分割前の計画・調査記録](../Archive/IndependentComponentsProposal.md)
