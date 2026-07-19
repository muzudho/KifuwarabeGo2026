# ソースコード概要とCGOS接続フロー

最終更新: 2026-07-18

## この文書の目的

KifuwarabeGo2026 のソースコードを、次の開発作業へ入る前に短時間で見渡せるようにまとめる。

特に CGOS への接続は、GUI 本体と独立した通信クライアントに分かれているため、画面操作から TCP 接続、CGOS プロトコル、GTP エンジン中継までの流れをここに整理する。

## プロジェクト構成

リポジトリ直下の主なプロジェクトは次の 3 つ。

| プロジェクト | 種類 | 役割 |
| --- | --- | --- |
| `KifuwarabeGo2026` | MonoGame GUI | 画面表示、ローカル対局、設定編集、CGOS 接続画面、外部プロセス起動 |
| `KifuwarabeGo2026.Engine` | コンソール GTP エンジン | 標準入力と標準出力で GTP を話すランダム合法手エンジン |
| `KifuwarabeGo2026.Gui.Communication.Cgos` | コンソール通信クライアント | CGOS サーバーへ TCP 接続し、CGOS と GTP エンジンの間を中継 |

依存の向きは大まかに次の通り。

```text
KifuwarabeGo2026
  -> Domain / Gtp / Application / Presentation
  -> 外部プロセスとして KifuwarabeGo2026.Gui.Communication.Cgos.exe を起動

KifuwarabeGo2026.Gui.Communication.Cgos
  -> TCP で CGOS サーバーへ接続
  -> 外部プロセスとして GTP エンジンを起動

KifuwarabeGo2026.Engine
  -> Domain を使って合法手生成
  -> GTP だけを標準入出力で処理
```

## GUI 本体の概要

### `Game1.cs`

MonoGame のメインループ。入力処理、画面遷移、描画呼び出し、CGOS 接続プロセス操作の入口になっている。

主な責務:

- 起動直後の `Local (推奨)` / `Connect To CGOS` 選択を処理する。
- ローカル対局では `PlayingScene` へマウス操作や更新を渡す。
- CGOS 画面では接続先プロファイルの CRUD、接続開始画面への遷移、Admin / Black / White プロセスの起動停止を処理する。
- `CgosConnectionProcess` の状態を毎フレーム更新し、`GoAppSession` へ反映する。

CGOS 関連の重要メソッド:

- `ToggleCgosPlayerConnectionProcess(GoStone stone)`: 黒番または白番の CGOS 通信プロセスを起動停止する。
- `ToggleCgosAdminProcess()`: 管理用 CGOS 接続を起動停止する。
- `SendCgosAdminCommand(string command)`: 管理接続へ `who` / `match` を送る。
- `UpdateCgosConnectionProcessStatus()`: 黒白プロセスの実行状態と最近の出力をセッションへ反映する。
- `UpdateCgosAdminProcessStatus()`: 管理プロセスの実行状態と最近の出力をセッションへ反映する。

### `GoAppSession.cs`

アプリ全体の状態を保持するクラス。盤面、モード、対局設定、GTP エンジン設定、CGOS 接続画面の状態が集約されている。

CGOS 関連では次を保持する。

- 接続先プロファイル一覧: `CgosConnectionProfiles`
- 選択中の接続先: `SelectedCgosConnectionProfile`
- CGOS 画面の段階: `CgosConnectionFlowKind`
- 黒番・白番に割り当てる GTP エンジン: `SelectedCgosBlackGtpEngineProfile` / `SelectedCgosWhiteGtpEngineProfile`
- Admin / Black / White 各プロセスの状態、ログディレクトリ、最近の出力
- 接続先プロファイル編集パネルの入力状態

`CgosConnectionFlowKind` は現在 2 段階。

| 値 | 意味 |
| --- | --- |
| `ProfileSelection` | CGOS 接続先一覧を表示して選ぶ段階 |
| `ConnectionStart` | 選んだ接続先に対して Admin / Black / White を起動する段階 |

### `GoScreenRenderer.cs`

画面描画とヒット判定の大部分を持つ。CGOS 画面もこのクラス内に実体がある。

CGOS 関連の描画:

- タイトルの `Connect To CGOS` ボタン。
- CGOS 接続先一覧、接続先詳細、追加・編集・複製・削除ボタン。
- 接続開始画面の Admin / Black / White プロセスパネル。
- 各プロセスの `START` / `STOP`、`TAIL`、`CODE`、Admin の `WHO` / `MATCH`。
- 選択中 GTP エンジンの `PREV` / `NEXT` / `CLEAR`。

`Presentation/Cgos/...` の `CgosConnectRenderer` と `CgosConnectionTargetRenderer` は薄いラッパーで、実際には `GoScreenRenderer.DrawCgosClientTop(...)` を呼んでいる。

## ドメインとローカル対局

### `Domain/GoBoard.cs`

囲碁盤の中核。9 路、13 路、19 路に対応する。

主な機能:

- 着手可否判定と石の配置。
- 取り上げ、自己アタリ禁止、単純なコウ点管理。
- 連の解析、隣接グラフ作成、単点眼の判定。
- Zobrist hash の更新。

### `Application/Local/Playing/PlayingScene.cs`

ローカル対局中画面の進行役。

主な流れ:

1. 対局開始時、コンピューター担当の色にだけ `GtpEngineClient` を作る。
2. GTP エンジンへ `boardsize`、`komi`、`clear_board` を送る。
3. 人間の着手は GUI の盤面操作から `GoAppSession` に反映し、必要なら他エンジンへ `play` を送る。
4. コンピューター番では `genmove` を送り、返ってきた頂点を盤面へ反映する。
5. 終局時やキャンセル時に GTP エンジンへ `quit` を送って破棄する。

### `Gtp/GtpEngineClient.cs`

GUI 本体から外部 GTP エンジンを起動し、標準入力・標準出力でコマンドを送受信するクライアント。

CGOS 用通信クライアントにも似た処理があるが、現状は共有クラス化されておらず、GUI ローカル対局用と CGOS 通信用で別実装になっている。

## 設定ファイル

### CGOS 接続先

`KifuwarabeGo2026/Content/CgosConnections/cgos-connection-list.json`

`CgosConnectionCatalog` が読み書きする。各要素は `CgosConnectionProfile`。

| 項目 | 意味 |
| --- | --- |
| `displayName` | 画面に表示する接続先名 |
| `host` | CGOS ホスト名 |
| `port` | CGOS ポート番号 |
| `role` | `PRACTICE`、`QUALIFIER`、`FINAL` などの用途メモ |
| `note` | 補足 |

既定値は `uec-go.com:6809` の練習、大会予選、大会本戦。

### GTP エンジン

`KifuwarabeGo2026/Content/GtpEngines/gtp-engine-list.json`

`GtpEngineCatalog` が読み書きする。GUI のローカル対局と CGOS 接続開始画面の両方で使う。

CGOS 接続では、選択した `GtpEngineProfile` から `executablePath` と `arguments` を組み立て、`--engine-command` として通信クライアントへ渡す。

## CGOS 接続の全体フロー

現在の設計は、GUI 本体が CGOS へ直接 TCP 接続するのではなく、GUI が CGOS 通信用コンソール exe を起動し、その標準出力を監視する形。

```text
ユーザー
  -> GUI: Connect To CGOS
  -> GUI: 接続先プロファイルを選ぶ
  -> GUI: USE で接続開始画面へ進む
  -> GUI: Black / White の GTP エンジンを選ぶ
  -> GUI: Admin / Black / White の START を押す
  -> CgosConnectionProcess
  -> KifuwarabeGo2026.Gui.Communication.Cgos.exe
  -> TCP: CGOS サーバー
  -> CGOS の setup/play/genmove/gameover を処理
  -> GTP エンジンプロセスへ boardsize/komi/clear_board/play/genmove/quit
```

### 1. 接続先プロファイルを読み込む

GUI 起動時、`Game1` のコンストラクターで `CgosConnectionCatalog.LoadFromDefaultLocation()` を呼ぶ。

読み込み先:

```text
KifuwarabeGo2026/Content/CgosConnections/cgos-connection-list.json
```

読み込んだ一覧は `GoAppSession.SetCgosConnectionProfiles(...)` でセッションへ入る。ファイルがない、または有効な接続先がない場合は `CgosConnectionCatalog` の既定プロファイルが使われる。

### 2. タイトルから CGOS 画面へ入る

起動直後に `Connect To CGOS` を押すと、`GoAppSession.SelectUseKind(GoAppUseKind.CgosClient)` が呼ばれる。

この段階では `CgosConnectionFlowKind.ProfileSelection` なので、CGOS 接続先一覧画面が出る。

### 3. 接続先一覧でプロファイルを選ぶ

接続先一覧画面では次の操作ができる。

- 接続先の選択。
- `ADD` / `EDIT` / `DUPLICATE` / `DELETE`。
- `PREV` / `NEXT` によるページ切り替え。
- `USE` で接続開始画面へ進む。

編集保存時は `Game1.SaveCgosConnectionEditDraft()` から `CgosConnectionCatalog.Save(...)` が呼ばれ、JSON へ保存される。

### 4. 接続開始画面で GTP エンジンを選ぶ

`USE` を押すと `GoAppSession.OpenCgosConnectionStartScreen()` が呼ばれ、`CgosConnectionFlowKind.ConnectionStart` になる。

接続開始画面には 3 つのプロセスパネルがある。

| パネル | 役割 |
| --- | --- |
| Admin | CGOS へ admin/admin でログインし、`who` / `match` を中継する |
| Black | `--account black` で `KifuwarabeB` として接続する |
| White | `--account white` で `KifuwarabeW` として接続する |

Black / White では、あらかじめ GTP エンジンを選ぶ。選択は `SelectedCgosBlackGtpEngineIndex` / `SelectedCgosWhiteGtpEngineIndex` として `GoAppSession` に保存される。

### 5. GUI が CGOS 通信プロセスを起動する

Black または White の `START` を押すと `Game1.ToggleCgosPlayerConnectionProcess(...)` が呼ばれる。

内部では `CgosConnectionProcess.Start(...)` を呼び、以下を行う。

1. リポジトリルートを探す。
2. `KifuwarabeGo2026.Gui.Communication.Cgos/bin/{Debug|Release}/net8.0/KifuwarabeGo2026.Gui.Communication.Cgos.exe` を探す。
3. 実行ファイルがなければ `CGOS communication executable was not found. Build the solution first.` で失敗する。
4. ログディレクトリを `Logs/Cgos/BlackPlayer`、`Logs/Cgos/WhitePlayer`、または `Logs/Cgos/Players` に決める。
5. 通信 exe を `ProcessStartInfo` で起動する。
6. 標準出力と標準エラーを非同期で読み、最近 8 行を GUI に表示する。

起動時に渡す主な引数:

```text
--host <profile.Host>
--port <profile.Port>
--account black|white
--log-directory <LogDirectory>
--engine-command <選択したGTPエンジンのコマンドライン>
```

Admin の `START` では `CgosConnectionProcess.StartAdmin(...)` が呼ばれ、`--admin` を付けて起動する。

### 6. 通信クライアントが起動オプションを解釈する

`KifuwarabeGo2026.Gui.Communication.Cgos/Program.cs` の `CgosClientOptions.Parse(...)` がコマンドラインを読む。

主な既定値:

| 項目 | 既定値 |
| --- | --- |
| Host | `uec-go.com` |
| Port | `6809` |
| EngineCommand | `dotnet run --project KifuwarabeGo2026.Engine\KifuwarabeGo2026.Engine.csproj` |
| LogDirectory | `Logs\Cgos` |
| Account | `black` |

`--account black` は `KifuwarabeB` / `KifuwarabeB`、`--account white` は `KifuwarabeW` / `KifuwarabeW` に変換される。ログではパスワードは `(password)` としてマスクされる。

`--both` を使った場合は同じプロセス内で黒白 2 アカウントを並列実行する。ただし GUI からの Black / White START は、それぞれ独立した通信プロセスとして起動している。

### 7. TCP 接続とログイン

通信クライアントは `CgosConnectionSession.RunAsync(...)` から `CgosTcpConnector.ConnectAsync(...)` を呼び、DNS 解決後に `TcpClient` で接続する。

タイムアウト:

- DNS 解決と TCP 接続: 15 秒。
- TCP 接続後、CGOS の最初のプロトコル行を待つ時間: 15 秒。

接続後は UTF-8 の `StreamReader` / BOM なし UTF-8 の `StreamWriter` を使い、改行は `\n`。

CGOS からのログイン要求に対して、次のように応答する。

| CGOS からの行 | クライアントの応答 |
| --- | --- |
| `protocol ...` | `e1 KifuwarabeGo2026.Cgos <version>` |
| `username` | アカウント名 |
| `password` | パスワード |

それ以外の行は通常の CGOS コマンドとして `CgosClient.HandleLineAsync(...)` へ渡される。

### 8. CGOS コマンドを GTP エンジンへ中継する

通常接続では `CgosClient` が CGOS コマンドを処理する。

| CGOS コマンド | 処理 |
| --- | --- |
| `setup` | 既存 GTP エンジンを終了し、新しい GTP エンジンを起動。`boardsize`、`komi`、`clear_board` と、必要なら過去着手の `play` を送る |
| `play` | GTP エンジンへ `play <color> <vertex>` を送る |
| `genmove` | GTP エンジンへ `genmove <color>` を送り、最初の応答行を CGOS へ返す |
| `gameover` | ログへ結果を出し、GTP エンジンを終了し、CGOS へ `ready` を送る |
| `info` | 現状は無視する |

未対応コマンドを受けると `Unsupported CGOS command: ...` で例外になり、GUI 側の状態判定では `ERROR` として扱われる。

### 9. GTP エンジンプロセス

`KifuwarabeGo2026.Gui.Communication.Cgos` 内の `GtpEngineProcess` が、`--engine-command` で受け取ったコマンドラインを起動する。

Windows では次の形になる。

```text
cmd.exe /c <engine-command>
```

標準入力、標準出力、標準エラーはリダイレクトされる。

GTP 応答は、空行が来るまで読む。`=` で始まる行は成功、`?` で始まる行は失敗として扱う。失敗時は例外を投げる。

破棄時は `quit` を送り、3 秒で終了しなければプロセスツリーごと kill する。

## ログと状態表示

GUI 側の `CgosConnectionProcess` は、通信クライアントの出力から状態文字列を推定する。

主な状態:

- `CONNECTING`
- `PROTOCOL`
- `LOGIN`
- `SETUP`
- `PLAY`
- `GENMOVE`
- `GENMOVE DONE`
- `GAME OVER`
- `CLOSED`
- `ERROR`

ログ出力先の概略:

| 種類 | 例 |
| --- | --- |
| GUI が見ているプロセス出力ログ | `Logs/Cgos/BlackPlayer/gui-blackplayer-*.log` |
| 通信クライアントの CGOS ログ | `Logs/Cgos/BlackPlayer/cgos-black-*.log` |
| GUI 側が保存する標準エラーログ | `Logs/Cgos/BlackPlayer/standard-error-blackplayer-*.log` |
| 通信クライアントが保存する GTP ログ | `Logs/Cgos/BlackPlayer/gtp-black-*.log` |

GUI の `TAIL` は PowerShell の `Get-Content -Wait` を別ウィンドウで開く。`CODE` は実行中でないときにログを VS Code で開こうとする。

## 現在の未整理点

### GUI の接続状態は盤面へ統合されていない

CGOS 通信プロセスは起動できるが、CGOS の盤面・棋譜・着手情報を GUI の囲碁盤へ戻す流れはまだない。現在の GUI は通信クライアントの標準出力を最近行として表示するだけ。

### CGOS 通信処理と GUI ローカル GTP 処理は重複している

GUI ローカル対局用の `GtpEngineClient` と、CGOS 通信クライアント内の `GtpEngineProcess` はどちらも GTP エンジンプロセスを起動して標準入出力を扱う。現状では別実装なので、今後の安定化では共通化候補になる。

### `CgosConnectionProfile.Role` は接続処理には使われていない

接続先プロファイルには `Role` があるが、実際に通信 exe へ渡しているのは `Host` と `Port`。`Role` は今のところ画面表示・分類用のメモに近い。

### 通信 exe はビルド済みファイル前提

GUI からは `KifuwarabeGo2026.Gui.Communication.Cgos.exe` を直接探して起動する。`dotnet run` ではなくビルド済み exe 前提なので、GUI から CGOS を試す前に対象構成でビルドが必要。

### パスワードは固定

`black` / `white` は `KifuwarabeB` / `KifuwarabeW` の固定アカウントに変換される。GUI から任意の CGOS アカウントやパスワードを設定する機能はまだない。

## 次に CGOS 周りを触るときの入口

目的別の入口。

| やりたいこと | 主に見る場所 |
| --- | --- |
| GUI の接続先一覧を変える | `Application/Cgos/ConnectionTarget/CgosConnectionCatalog.cs`、`GoAppSession.cs`、`GoScreenRenderer.cs` |
| GUI の接続開始画面を変える | `Game1.cs`、`GoAppSession.cs`、`GoScreenRenderer.cs` |
| GUI から起動する通信プロセスの引数を変える | `Application/Cgos/Connect/CgosConnectionProcess.cs` |
| CGOS のログインや TCP 接続を変える | `KifuwarabeGo2026.Gui.Communication.Cgos/Program.cs` の `CgosConnectionSession`、`CgosTcpConnector` |
| CGOS コマンド対応を増やす | `KifuwarabeGo2026.Gui.Communication.Cgos/Program.cs` の `CgosClient.HandleLineAsync(...)` |
| GTP エンジン起動・応答読み取りを変える | `KifuwarabeGo2026.Gui.Communication.Cgos/Program.cs` の `GtpEngineProcess` |
| CGOS の局面を GUI へ反映する | まず `CgosConnectionProcess` の出力監視だけでは不足。通信結果を構造化して `GoAppSession` や対局画面へ渡す設計が必要 |

