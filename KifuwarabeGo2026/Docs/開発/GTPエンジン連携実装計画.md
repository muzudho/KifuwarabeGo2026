# GTPエンジン連携実装計画

最終更新: 2026-07-10

## 目的

GUI と囲碁思考エンジンを別プロセスに分け、GUI から GTP (Go Text Protocol) で思考エンジンを操作できるようにする。

当面の目標は、白番または黒番をコンピューターに設定し、GUI が別プロセスのランダム合法手エンジンへ `genmove` を送り、返ってきた手を盤上に反映するところまで。

## 基本方針

- GUI は MonoGame アプリケーション `KifuwarabeGo2026` が担当する。
- 思考エンジンは別プロジェクトのコンソールアプリとして作る。
- GUI とエンジンの通信は GTP を使う。
- GTP 通信は標準入力と標準出力を使う。
- GUI 本体から直接 `Process` を触らず、GTP クライアント層に閉じ込める。
- 最初から強い思考は作らず、ランダム合法手エンジンで別プロセス連携を確認する。
- GTP ログは初期段階から残す。

## 推奨プロジェクト構成

```text
KifuwarabeGo2026                 GUI / MonoGame
KifuwarabeGo2026.Gtp             GTP通信・座標変換・プロセス管理
KifuwarabeGo2026.Engine          別プロセスの思考エンジン
```

最初は `KifuwarabeGo2026.Gtp` を別プロジェクトにせず GUI プロジェクト内のフォルダーで始めてもよい。ただし、責務は最初から分けておく。

## 現在の実装状況

2026-07-10 時点で、GUI から別プロセス GTP エンジンを起動し、対局開始、着手同期、コンピューター着手、基本的なエラー表示まで実装済み。

実装済み:

- GUI セットアップ画面で黒番・白番を `Human` / `Computer` から選択できる。
- `GoAppSession` に `BlackPlayerKind`、`WhitePlayerKind` を持たせている。
- `KifuwarabeGo2026.Engine` コンソールプロジェクトをソリューションへ追加済み。
- `KifuwarabeGo2026.Engine` は標準入力から GTP コマンドを読み、標準出力へ GTP 応答だけを返す。
- GUI プロジェクト内の `Gtp` フォルダーに `GtpEngineClient`、`GtpEngineSettings`、`GtpResponse`、`GtpCoordinate` を追加済み。
- `GtpEngineClient` は別プロセス起動、標準入出力、応答読み取り、タイムアウト、`quit`、送受信ログの土台を持つ。
- GUI の対局開始時に `GtpEngineClient` を起動する。
- 対局開始時に `boardsize` / `clear_board` を送る。
- 人間の着手とパスを `play` コマンドとしてエンジンへ同期する。
- `Computer` 手番で `genmove` を送り、応答手を盤へ反映する。
- エンジン準備中や思考中は盤への入力を抑制する。
- エンジンエラー時は盤面領域にエラー内容と GTP ログ保存先を表示する。

未実装:

- 投了をエンジンへ伝えるか、投了時にエンジンプロセスを閉じるかの整理。
- エンジン設定ファイル。
- エンジン登録 UI。

## 実装ステップ

### 1. GUI に対局者設定を追加する

状態: 完了

- 黒番を `Human` / `Computer` から選べるようにする。
- 白番を `Human` / `Computer` から選べるようにする。
- 最初は右パネルに簡易選択 UI を置く。
- 本格的な登録ダイアログは後回しにする。

必要になる状態:

```text
BlackPlayerKind = Human | Computer
WhitePlayerKind = Human | Computer
```

### 2. 思考エンジン用プロジェクトを追加する

状態: 完了

- `KifuwarabeGo2026.Engine` をコンソールアプリとして追加する。
- まずはランダム合法手を返す GTP エンジンにする。
- 最低限、以下の GTP コマンドに対応する。

```text
protocol_version
name
version
boardsize 9
boardsize 13
boardsize 19
clear_board
play black D4
play white pass
genmove black
genmove white
quit
```

### 3. GUI 側に GTP クライアント層を作る

状態: 完了。GUI の対局ループから非同期に呼び出している。

候補クラス:

```text
GtpEngineClient
GtpCommand
GtpResponse
GtpCoordinate
GtpEngineSettings
```

`GtpEngineClient` の責務:

- `.exe` を `Process` で起動する。
- 標準入力へ GTP コマンドを書く。
- 標準出力から GTP 応答を読む。
- コマンドごとのタイムアウトを扱う。
- `quit` とプロセス終了を扱う。
- 送受信ログを残す。

GUI の描画ループを止めないよう、GTP 通信は非同期で実行する。

### 4. GUI の対局進行を整理する

状態: 基本実装済み。次は異常系と長時間対局の確認を厚くする。

現在手番が人間かコンピューターかで処理を分ける。

- `Human` の手番: 盤クリック、パス、投了を受け付ける。
- `Computer` の手番: `genmove` を送り、応答が返るまで入力を抑制する。
- コンピューターが返した着手は GUI 側の合法手処理にも通す。
- 人間が着手、パス、投了した場合は、必要に応じてエンジンへ `play` を送って同期する。

思考中の状態を追加する候補:

```text
IsEngineThinking
PendingEngineMove
EngineErrorMessage
```

実装済みの状態:

```text
IsEngineThinking
IsEngineReady
EngineErrorMessage
EngineLogPath
```

### 5. エンジン設定ファイルを作る

最初は 1 件だけ保存できればよい。

候補ファイル:

```text
engine-settings.json
```

保存項目:

```json
{
  "name": "Kifuwarabe Random GTP",
  "executablePath": "path-to-engine.exe",
  "workingDirectory": "path-to-engine-folder",
  "arguments": "",
  "enableGtpLog": true
}
```

複数エンジン登録、既定エンジン選択、接続テスト UI は、GTP 連携が動いた後に追加する。

### 6. GTP ログを残す

通信不具合の切り分けのため、送信コマンドと受信応答をログに残す。

例:

```text
-> boardsize 9
<- =

-> genmove white
<- = D4
```

ログの保存先は後で決める。まずは開発中に確認しやすい場所でよい。

### 7. エンジン登録 UI を作る

GTP 連携が一通り動いてから作る。

必要な機能:

- 追加
- 編集
- 削除
- `.exe` 選択
- 起動引数編集
- 作業ディレクトリ編集
- 接続テスト
- 既定エンジン選択

## 最初の到達目標

次の状態まで進めれば、GUI と思考エンジンの分離が実証できる。

1. 白番を `Computer` に設定できる。状態: 完了。
2. GUI が `KifuwarabeGo2026.Engine.exe` を起動できる。状態: 完了。
3. 対局開始時に `boardsize` と `clear_board` を送れる。状態: 完了。
4. 人間の黒番着手後、GUI がエンジンへ `play black ...` を送れる。状態: 完了。
5. GUI が `genmove white` を送り、返ってきた白番着手を盤に置ける。状態: 完了。
6. パスと投了の結果を GUI とエンジンの間で破綻なく扱える。状態: パスは完了。投了は次に整理。

## 検証メモ

ビルド確認:

```powershell
dotnet build KifuwarabeGo2026.slnx
```

エンジン単体の GTP 応答確認:

```powershell
@('protocol_version','name','boardsize 9','clear_board','play black D4','genmove white','quit') | dotnet run --project KifuwarabeGo2026.Engine\KifuwarabeGo2026.Engine.csproj
```

確認できた応答例:

```text
= 2

= Kifuwarabe Random GTP

=

=

=

= D3

=
```

2026-07-10 の確認:

- `dotnet build KifuwarabeGo2026.slnx` 成功。
- エンジン単体の代表コマンド確認成功。`genmove white` はランダム合法手のため、応答手は毎回変わる。

## 後回しにすること

- 強い思考アルゴリズム。
- 複数エンジン登録。
- 詳細なエンジン設定ダイアログ。
- 対局時計。
- SGF 保存。
- ネットワーク越しのエンジン接続。

