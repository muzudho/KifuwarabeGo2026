# ゲーム構成要素と境界の用語整理

> 過去の計画・調査記録。本文の「現在」「次回」、旧名称、チェック欄は記録当時のものです。
> 現状と後続作業は [ゲーム構成要素と境界の用語](../CurrentState/GameOasisTerminology.md) / [Game Oasisの将来構想とプロセス名](../Plans/GameOasisVision.md) を参照してください。


## 目的

`Provider Engine` という名称だけでは責務が伝わりにくいため、ゲームを構成するオブジェクトと、その境界に使う用語を整理します。

この文書では、ゲームルールを適用し、プレイヤーの行動によって状態を変化させるプログラムを「プレイスペースエンジン」と呼びます。

また、GUIからゲームの運営処理を切り離し、プレイヤー、ゲームマスター、GUI、プレイスペースエンジンを接続するコンピュータープログラムを「ゲームコンシェルジュ」と呼びます。

これは将来の設計に向けた用語案です。現行コードや既存プロトコルに残る `Provider` を、ただちに一括改名することを定めるものではありません。

## 表示名と内部名

この節は[内容別の文書](../CurrentState/GameOasisTerminology.md)へ移しました。

## 全体像

この節は[内容別の文書](../CurrentState/GameOasisTerminology.md)へ移しました。

## オブジェクト

この節は[内容別の文書](../CurrentState/GameOasisTerminology.md)へ移しました。

## 境界

この節は[内容別の文書](../CurrentState/GameOasisTerminology.md)へ移しました。

## ゲームの分類

この節は[内容別の文書](../CurrentState/GameOasisTerminology.md)へ移しました。

## 推奨用語

この節は[内容別の文書](../CurrentState/GameOasisTerminology.md)へ移しました。

## `Provider Engine` の扱い

この節は[内容別の文書](../CurrentState/GameOasisTerminology.md)へ移しました。

## 将来の拡張

この節は[内容別の文書](../Plans/GameOasisVision.md)へ移しました。

## Kifuwarabe Game Oasis構想

### 製品名の二つの意味

この節は[内容別の文書](../Plans/GameOasisVision.md)へ移しました。

### 将来の実行プロセス名

この節は[内容別の文書](../Plans/GameOasisVision.md)へ移しました。

### 代表的なプレイスペース

最初の代表的なプレイスペースとして、次の二つを用意する構想です。

| プレイスペース | 目的 | 主な相違点 |
|---|---|---|
| 通常囲碁 | 通常の囲碁対局、研究、観戦 | 囲碁の合法手、コウ、パス、終局、勝敗判定 |
| ポン抜き | 入門、配信企画、実験的なゲーム | 初期配置、ハンディキャップ、石を取ることによる終了判定 |

通常囲碁とポン抜きを同じ境界Sへ接続することで、囲碁固有の処理がGUIやゲームコンシェルジュへ混入していないかを検証できます。

```text
［Kifuwarabe Game Oasis］
├─ GUI
├─ ゲームコンシェルジュ
├─ プレイヤー接続
├─ ゲームマスター接続
└─ プレイスペース
   ├─ 通常囲碁
   └─ ポン抜き
```

## 将来のC#プロジェクト構成案

この節は将来の大規模な仕様変更に向けた構想です。現時点ではプロジェクトの作成、改名、移動、参照変更を実施しません。

この再構成はv4.0.0の破壊的変更として扱います。現行プロジェクトからの詳細な分割・統合・移行方針は、[`v4.0.0プロジェクト再構成計画.md`](GameOasisV4Migration.md)を参照してください。

### Game Oasis本体に属するプロジェクト

`KifuwarabeGo2026.GameOasis.*` の下には、ゲームオアシスの境界契約と公式コンシェルジュを置きます。プレイヤー、ゲームマスター、GUI、プレイスペースの各実装は、境界の外側から接続されるため、この名前空間の下には置きません。

境界契約を所有するプロジェクト名には、`KifuwarabeGo2026.GameOasis.Contracts` を採用します。`KifuwarabeGo2026.Contracts` という製品全体を対象に見える名称は使用せず、契約の所有者がGame Oasisであることを名前で明示します。

```text
KifuwarabeGo2026.GameOasis.Contracts
KifuwarabeGo2026.GameOasis.Concierge
```

| プロジェクト | 所有するもの |
|---|---|
| `KifuwarabeGo2026.GameOasis.Contracts` | プロトコルP、M、G、Sの契約、コマンド、応答、通知、共通識別子、エラー、能力情報 |
| `KifuwarabeGo2026.GameOasis.Concierge` | セッション、参加者、進行、時計、記録、権限確認、運営コマンド、プレイスペースの呼び出し |

Contractsは最初は一つのC#プロジェクトとし、境界ごとに名前空間を分けます。

```text
KifuwarabeGo2026.GameOasis.Contracts.Common
KifuwarabeGo2026.GameOasis.Contracts.ProtocolP
KifuwarabeGo2026.GameOasis.Contracts.ProtocolM
KifuwarabeGo2026.GameOasis.Contracts.ProtocolG
KifuwarabeGo2026.GameOasis.Contracts.ProtocolS
```

必要性が明らかになった場合は、将来それぞれを別アセンブリまたは別パッケージへ分割できます。

### 公式の参照実装

Kifuwarabe Goリポジトリーで用意するプレイヤー、ゲームマスター、GUI、プレイスペースは、Game Oasisへ外側から接続する公式の参照実装と位置づけます。名前には学習用・非本番用という印象が強い `Examples` を使用せず、契約の模範となる実装を示す `KifuwarabeGo2026.Reference.*` を採用します。

```text
KifuwarabeGo2026.Reference.PlayerEngine
KifuwarabeGo2026.Reference.GameMasterEngine
KifuwarabeGo2026.Reference.Gui
KifuwarabeGo2026.Reference.PlayDomain.Go
KifuwarabeGo2026.Reference.PlayRoomEngine.Go
KifuwarabeGo2026.Reference.PlayRoomEngine.Ponnuki
```

| 参照実装 | 使用する主な契約 | 所有するもの |
|---|---|---|
| `KifuwarabeGo2026.Reference.PlayerEngine` | プロトコルP | 公式のコンピュータープレイヤー実装 |
| `KifuwarabeGo2026.Reference.GameMasterEngine` | プロトコルM | 公式の自動ゲームマスター実装 |
| `KifuwarabeGo2026.Reference.Gui` | プロトコルG | 公式GUIと人間参加者向けの操作アダプター |
| `KifuwarabeGo2026.Reference.PlayDomain.Go` | 囲碁横断 | 囲碁座標、石、盤、連、呼吸点など、GUI、各種Engine、形式アダプターで共有する囲碁ドメイン |
| `KifuwarabeGo2026.Reference.PlayRoomEngine.Go` | プロトコルS | 通常囲碁の設定、状態、合法手、行動適用、終局、勝敗判定 |
| `KifuwarabeGo2026.Reference.PlayRoomEngine.Ponnuki` | プロトコルS | ポン抜きの設定、初期局面、ハンディキャップ、行動適用、終了、勝敗判定 |

`Go.Foundation` は候補名です。実装開始時に `Go.Domain` などと比較して最終決定します。

参照実装はGame Oasis本体の内部実装ではありません。別リポジトリーの開発者も、自分の組織または製品の名前空間を使用して独立した実装を作成できます。

Game Oasisの公開識別子について、きふわらべ公式実装は`io.github.muzudho.kifuwarabego2026`を所有ルートとします。識別子比較は大文字と小文字を区別しますが、公式発行値はASCII小文字、数字、ピリオド、ハイフンだけで構成します。外部実装は自分が所有・管理できる名前空間を使用します。詳細と移行判断は[v4.0.0の再構成記録](GameOasisV4Migration.md)の「公開識別子の命名規約」を参照します。

現在はC#版Contractsと参照実装を先に完成させます。将来はプレイヤーエンジン、ゲームマスターエンジン、GUIエンジン、プレイスペースエンジンのすべてを、コンピューター言語を問わず接続できるテキストベース仕様へ展開します。テキスト形式と配送方式はC#版で意味契約を検証した後に決定し、現段階では固定しません。

```text
AliceGames.PonnukiPlaySpace       → ProtocolS
BobEngines.StreamingGameMaster   → ProtocolM
ExampleCompany.GoPlayer          → ProtocolP
```

### フォーマル・アダプター

ゲームオアシス内部は、外部界隈ごとの仕様から独立した［カジュアル・コア］として実装します。GTP、CGOS、USI、CSA、SGF、KIFなどの既存仕様は、`KifuwarabeGo2026.FormalAdapter.*`からゲームオアシスの契約へ接続します。

```text
KifuwarabeGo2026.FormalAdapter.Gtp
KifuwarabeGo2026.FormalAdapter.Cgos
KifuwarabeGo2026.FormalAdapter.Usi
KifuwarabeGo2026.FormalAdapter.Csa
KifuwarabeGo2026.FormalAdapter.Sgf
KifuwarabeGo2026.FormalAdapter.Kif
```

`FormalAdapter`は外部仕様の解釈、意味変換、実装差の吸収、診断を所有しますが、ゲーム状態やプレイヤー戦略の正本を所有しません。カジュアル・コアは特定の`FormalAdapter`を参照せず、アプリケーションの構成点が両者を接続します。

外部仕様の説明、仕様書と慣習の区別、内部契約との対応、情報損失、実装例、互換性試験も同じ領域へ集め、界隈を理解できる実行可能なリファレンスとして育てます。詳細は[`カジュアル・コアとフォーマル・アダプター.md`](FormalAdapterDesign.md)を参照してください。

### Game Oasisの外側に置くプロジェクト

製品の起動基盤と汎用UIライブラリーは、Game Oasisを利用または起動する外側の部品であり、ゲームオアシスの実行モデルには含めません。

```text
KifuwarabeGo2026.InstallerGui
KifuwarabeGo2026.InstallerGui.Platform
KifuwarabeGo2026.InstallerGui.Presentation
KifuwarabeGo2026.InstallerEngine
KifuwarabeGo2026.InstallerEngine.Platform
KifuwarabeGo2026.StationeryUI
```

- `InstallerGui.*` は利用者との対話、描画、入力、ＧＵＩ用プラットフォーム固有処理を所有します。
- `InstallerEngine.*` はインストール、更新、バージョン管理、管理対象実行ファイルの起動、エンジン用プラットフォーム固有処理を所有します。
- 利用者向け正規実行ファイル名は `KifuwarabeGo2026.Installer.exe` です。引数なしでは管理画面を開き、`--launch-lobby` では現在版のロビーを起動します。デスクトップの標準ショートカットには後者を指定します。
- ロビーの［インストーラーを起動］と、インストーラーの［ロビーを起動］で相互に移動できます。
- 旧 `KifuwarabeGo2026.Launcher.exe` と旧Launcher名の配布ZIPは、既存のショートカットと更新クライアント向けの互換入口として残します。設定ファイル `launcher-settings.json`、管理配置先 `Launcher/Current`、旧JSONキーも移行互換のため維持します。
- `StationeryUI` はボタン、付箋、入力部品、Canvasなどの汎用的なMonoGame UI部品を所有します。
- これらはGame Oasisから利用されても、Game Oasis固有のゲーム運営やルールを所有しません。

### 現行プロジェクトの扱い

#### `KifuwarabeGo2026.Match`

現行の `MatchSession` は囲碁盤、合法手、石の取得、コウ、パス、投了、局面、終局状態などを所有しています。そのため、名称は `Match` ですが、将来構成では主として通常囲碁のプレイスペースに相当します。

将来は内容を確認しながら、主に次の移行先へ分解します。

```text
KifuwarabeGo2026.Match
    ├─ 囲碁ルールと状態 → KifuwarabeGo2026.Reference.PlayRoomEngine.Go
    └─ 時計や運営処理   → KifuwarabeGo2026.GameOasis.Concierge
```

特に時計はゲーム設定として時間値を受け取りますが、時計を実際に進行させる責務はゲームコンシェルジュ側に置きます。

#### `KifuwarabeGo2026.Shared`

`Shared` は長期的な所有者名として使用せず、内容を実際の所有者へ戻します。

```text
GoBoard、GoPoint、GoStone、盤面解析
    → KifuwarabeGo2026.Reference.PlayDomain.Go

ApplicationFamilySettingsなどの製品共通設定
    → Installerまたは将来の製品設定用プロジェクト
```

`Shared` をそのまま `GameOasis.Shared` へ改名すると、異なる責務を再び一つの箱へ集めることになるため行いません。

### 目標とする参照方向

ContractsはGame Oasisの内側と外側を接続する契約です。各実装は自分に必要な契約を参照し、Contractsは各実装を参照しません。GUIは具体的な通常囲碁やポン抜きの実装を直接参照せず、ゲームコンシェルジュを通じて利用します。

```text
［Reference.PlayerEngine］ ── P ──┐
［Reference.GameMasterEngine］─ M ─┤
［Reference.GUI］────────── G ──────┼── ［GameOasis.Concierge］
                                   │
［Reference.PlaySpace.Go］──── S ───┤
［Reference.PlaySpace.Ponnuki］─ S ─┘

すべての実装 ──参照──→ ［GameOasis.Contracts］
［GameOasis.Contracts］ ──×──→ 各実装
```

`Contracts` は境界を越える契約だけを所有し、GUI、MonoGame、コンシェルジュの具象実装、特定のプレイヤー、ゲームマスター、プレイスペース実装へ依存しません。通信が双方向でも、C#プロジェクトの参照は各実装からContractsへの一方向です。

### プロトコルの実装方針

プロトコルGとSの「プロトコル」は、最初から別プロセス通信を必須とするものではありません。まず同一プロセス内のC#インターフェースとデータ契約として実装し、必要になったときにJSON、標準入出力、ソケットなどの通信アダプターを追加します。

プロトコルGでは画面座標、ボタン、ダイアログなどの描画詳細を渡さず、「ポン抜きを選択した」「ゲームを開始する」などの意味を持つ操作と、表示に必要な状態を渡します。

プロトコルSの最小候補は次のとおりです。

- プレイスペースの種類と能力を取得する
- 設定スキーマを取得する
- ゲーム設定を検証する
- プレイスペースを初期化する
- 現在状態を取得する
- 行動を適用する
- 適用結果とゲームイベントを取得する
- 終了状態と勝敗を取得する
- セッションを破棄する

### 想定する移行順序

大きな一括改名や一括移動は行わず、動作する縦方向の経路を一本ずつ作ります。

1. 通常囲碁とポン抜きの代表的な操作シナリオを記述する。
2. `GameOasis.Contracts` に最小のプロトコルSを定義する。
3. `GameOasis.Concierge` を作り、GUIにある運営処理を段階的に移す。
4. GUIの必要に合わせてプロトコルGを定義する。
5. 比較的小さなポン抜きを最初の `Reference.PlaySpace` として切り出す。
6. GUI、ゲームコンシェルジュ、ポン抜きが接続された一本の経路を検証する。
7. 同じ契約を使って通常囲碁を切り出す。
8. `Match` と `Shared` の残った責務を所有者別に移す。
9. 必要になった場合だけ別プロセス通信を追加する。

### この構想で現時点では行わないこと

- C#プロジェクトの作成、改名、削除、移動
- 名前空間とアセンブリ名の一括変更
- プロジェクト参照の変更
- 現行 `Provider` プロトコルの削除
- GUIから既存処理を移動する実装作業
- プロトコルG、M、Sのデータ形式の確定

実装を開始する前に、境界契約、移行単位、互換性、テスト方法を改めて確認します。
