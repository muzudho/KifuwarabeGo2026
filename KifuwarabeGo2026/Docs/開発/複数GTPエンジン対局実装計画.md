# 複数GTPエンジン対局実装計画

最終更新: 2026-07-10

## 目的

黒番と白番に別々の GTP 思考エンジンを割り当て、コンピューター同士の対局をできるようにする。

現状は黒番・白番を `Human` / `Computer` から選べるが、`Computer` が使うエンジンは `Kifuwarabe Random GTP` に固定されている。また、GUI 側の `GtpEngineClient` は 1 個だけなので、黒番と白番の両方を `Computer` にしても、同じエンジンプロセスが両方の手を打つ構造になっている。

次の段階では、黒番用エンジンと白番用エンジンを別プロセスとして起動できる構造へ変える。

## 到達目標

最初の到達目標:

1. セットアップ画面で黒番・白番を `Human` / `Computer` から選べる状態は維持する。
2. 黒番 `Computer` には黒番用 GTP エンジンを割り当てる。
3. 白番 `Computer` には白番用 GTP エンジンを割り当てる。
4. 黒番・白番が両方 `Computer` の場合、2 つの GTP エンジンプロセスを起動する。
5. 片方のエンジンが打った手は、もう片方のエンジンへ `play` で同期する。
6. 人間が打った手も、対局中の全コンピューターエンジンへ `play` で同期する。
7. 終局、投了、キャンセル時は起動中の全エンジンプロセスを閉じる。

## 基本方針

- まずはエンジン登録 UI ではなく、内部的に黒用・白用の既定エンジン設定を持つ。
- 最初は黒用・白用とも同じ `Kifuwarabe Random GTP` を使ってよい。ただし、プロセスは別々に起動する。
- `GtpEngineClient` は 1 対 1 で 1 つの外部エンジンプロセスを管理する責務のままにする。
- GUI 側に、石色ごとのエンジンクライアントを管理する薄い層を作る。
- すべてのエンジンが同じ盤面状態を持つように、対局開始時と着手時の同期順序を明確にする。

## 推奨する状態

`GoAppSession` 側に持たせる候補:

```text
BlackPlayerKind = Human | Computer
WhitePlayerKind = Human | Computer
BlackEngineName
WhiteEngineName
```

エンジン設定として持たせる候補:

```text
BlackEngineSettings
WhiteEngineSettings
```

GUI 実装側で管理する候補:

```text
Dictionary<GoStone, GtpEngineClient> _gtpEngines
Dictionary<GoStone, GtpEngineSettings> _engineSettingsByStone
```

または、辞書にせず以下のように明示的に持ってもよい:

```text
GtpEngineClient? _blackGtpEngine
GtpEngineClient? _whiteGtpEngine
```

最初は黒白だけなので、読みやすさを優先して明示フィールドでもよい。

## 同期ルール

### 対局開始時

`Computer` に設定されている石色ごとにエンジンを起動する。

各エンジンへ以下を送る:

```text
boardsize N
komi X
clear_board
```

全エンジンの準備が成功してから `IsEngineReady = true` にする。

### 人間の着手

人間が着手したら、起動中の全エンジンへ同じ手を送る。

```text
play black D4
```

人間がパスした場合:

```text
play white pass
```

### コンピューターの着手

現在手番の石色に対応するエンジンへだけ `genmove` を送る。

```text
genmove black
```

返ってきた手を GUI 側の合法手処理へ通す。成功したら、その手を他の起動中エンジンへ `play` で同期する。

例: 黒エンジンが `D4` を返した場合

```text
黒エンジン: genmove black -> D4
GUI: 黒 D4 を盤へ反映
白エンジン: play black D4
```

`genmove` を実行した本人のエンジンには、同じ手を追加で `play` しない。多くの GTP エンジンは `genmove` 成功時点で自分の内部盤面を進めるため。

### 終局時

以下の場合は起動中の全エンジンを閉じる。

- 投了
- 二連続パス
- エンジンが返した手による終局
- 対局キャンセル
- エンジンエラー後の復帰操作

## 実装ステップ

### 1. 既存の単一エンジン管理を整理する

状態: 未着手

- `_gtpEngine` を石色別に持てる形へ変更する。
- `HasComputerPlayer()` は維持する。
- `GetEngineForTurn()` または `GetEngine(GoStone stone)` のような取得関数を作る。
- `StopGtpGame()` はすべてのエンジンを閉じる処理へ変える。

### 2. 対局開始時に必要なエンジンを複数起動する

状態: 未着手

- 黒番が `Computer` なら黒用クライアントを作る。
- 白番が `Computer` なら白用クライアントを作る。
- 両方 `Computer` なら 2 プロセス起動する。
- 片方だけ `Computer` なら 1 プロセスだけ起動する。

### 3. `play` 同期を全エンジン対象にする

状態: 未着手

- 人間の着手は全エンジンへ同期する。
- コンピューターの着手は、着手した本人以外のエンジンへ同期する。
- 同期中は次の `genmove` を始めない。

### 4. `genmove` 対象を現在手番のエンジンに限定する

状態: 未着手

- 現在手番が `Computer` でなければ何もしない。
- 現在手番が `Computer` なら、その石色に対応するエンジンへ `genmove` を送る。
- 対応するエンジンが存在しない場合はエラー表示する。

### 5. エラー表示とログを見直す

状態: 未着手

- 黒用エンジンと白用エンジンのどちらでエラーが起きたか分かるメッセージにする。
- ログファイルを共有にするか、黒白で分けるか決める。
- 最初は共有ログでもよいが、黒白の送受信が区別できるようにする。

ログ例:

```text
[black-engine] -> genmove black
[black-engine] <- = D4
[white-engine] -> play black D4
[white-engine] <- =
```

### 6. セットアップ画面にエンジン名表示を追加する

状態: 未着手

- まずは選択 UI ではなく、`Computer` の横に既定エンジン名を表示するだけでよい。
- 例: `Computer: Kifuwarabe Random GTP`
- 複数エンジン登録 UI は後続作業に回す。

### 7. エンジン設定ファイルと選択 UI へ進む

状態: 後回し

複数プロセス対局が動いてから、次を追加する。

- `engine-settings.json`
- 複数エンジン登録
- 黒番エンジン選択
- 白番エンジン選択
- 接続テスト
- エンジンごとの GTP ログ出力先

## 注意点

- `genmove` 後に同じエンジンへ `play` を返すと、二重着手になる可能性がある。
- 片方のエンジンだけ同期に失敗した場合、対局を続けると盤面不一致になるのでエラー停止する。
- 黒白が同じ実行ファイルを使う場合でも、プロセスは別々に起動する。
- ランダムエンジン同士の対局では、終局までに時間がかかる可能性があるため、キャンセル操作は維持する。
- コンピューター同士の対局では、連続して高速に着手するため、描画が追いつくか確認する。

## 検証項目

- 黒 `Human`、白 `Computer` の既存挙動が壊れていない。
- 黒 `Computer`、白 `Human` で、黒が初手を打てる。
- 黒 `Computer`、白 `Computer` で、黒白が交互に自動着手する。
- コンピューター同士の対局で、両エンジンのログに相手手の `play` が残る。
- パス、二連続パス、投了、キャンセルで全エンジンプロセスが終了する。
- 片方のエンジン起動失敗時に、画面がエラー表示になり、対局をキャンセルできる。

## 関連ファイル

- [GTPエンジン連携実装計画.md](./GTPエンジン連携実装計画.md)
- [../続きはここから.md](../続きはここから.md)
- `KifuwarabeGo2026/Game1.cs`
- `KifuwarabeGo2026/Application/GoAppSession.cs`
- `KifuwarabeGo2026/Gtp/GtpEngineClient.cs`
- `KifuwarabeGo2026/Gtp/GtpEngineSettings.cs`
