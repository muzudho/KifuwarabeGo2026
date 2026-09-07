# PLAYER 統合実装計画

最終更新: 2026-08-12

## 目的

従来の対局設定は、黒白それぞれについて次の 2 段階でした。

1. `HUMAN` または `COMPUTER` を選ぶ。
2. `COMPUTER` のときだけ GTP エンジンを選ぶ。

Local Match と Ponnuki は、対局者を共通に扱う `PLAYER` 選択へ移行済みです。人間とコンピューターを同じ一覧から選べ、表示名と外部連携用の識別子を登録できます。

ただし、CGOS と大会ルールへの適用、および旧来の `BlackPlayerKind` / エンジン選択インデックスの完全削除は未完了です。この文書は、残った移行作業を明確にする実装計画として維持します。

## 実装状況（2026-08-11）

| 区分 | 状態 | 現在の実装・残作業 |
| --- | --- | --- |
| Player データと永続化 | 完了 | `EntryProfile` と `EntryCatalog` を追加済み。`player-list.json` へ ID、表示名、識別子、種別、参照エンジン ID を保存・再読込する。 |
| 既存設定からの初期移行 | 一部完了 | Player 未登録時に Black/White の Human と、各 GTP エンジンに対応する Computer Player を生成する。旧エンジン ID の再生成による自動 Player の参照切れも表示名ベースで補正する。旧 UI の人間名・黒白エンジン選択を完全に引き継ぐ永続移行は未実装。 |
| 対局セッション | 完了（旧状態併存） | `BlackEntryProfileId` / `WhiteEntryProfileId` を保持し、選択 Player から Human/Computer と参照エンジンを解決する。互換のため旧 `BlackPlayerKind` / `WhitePlayerKind` とエンジン選択インデックスも同時に更新している。 |
| Local Match / Ponnuki の選択 UI | 完了 | 黒白とも同じ Player 一覧から選択する `PLAYER SELECT` UI を使用する。 |
| Player 一覧・編集 | 完了 | Human/Computer の追加、編集、削除保護、並べ替え、ページ送りを実装。表示名・Identifier はクリック編集、貼り付け、IME、Enter/Escape、Tab に対応する。Computer は参照エンジンを切替え、`EDIT PROFILE` でエンジン設定を開ける。 |
| SGF の対局者名 | 完了 | ローカル棋譜の `PB` / `PW` には選択 Player の `DisplayName` を保存する。 |
| Random Seed | 一部完了 | Human/Computer 判定に追従して自動変更を有効・無効化する。ただし表示とルートコメントはまだ `Player1` / `Player2` で、Player の表示名・Identifier は記録していない。 |
| CGOS と大会ルール | 未着手 | CGOS は GTP エンジン選択と個別ログイン認証のままで、Player の `Identifier` を送信していない。大会ルールにも Player 選択は未接続。 |
| 旧状態の削除 | 未着手 | `BlackPlayerKind` / `WhitePlayerKind` と黒白の GTP エンジン選択インデックスは、互換アダプターとして残っている。 |

## 方針

- `EntryProfile` は「誰が打つか」を表す。
- `GtpEngineProfile` は「どの実行可能な碁エンジンをどう起動するか」を表す。
- `ClientIdentityProfile` は「Player がどの用途・接続先へ、どの Login Name で参加するか」を表す。
- コンピューター Player は、1 個の `GtpEngineProfile` を参照する。
- Player は、使用可能な複数の `ClientIdentityProfile` を一方向に参照する。Engine Profile と Client Identity Profile は独立したカタログであり、Player を逆参照しない。
- `ClientIdentityProfile.ConnectionProfileId` は接続先（現時点では CGOS）の不変 ID を参照する。正規化済み接続文字列は重複検出・候補選択用であり、参照キーにはしない。
- 同じエンジンを参照しても、表示名・識別子の異なる複数 Player を作れる。
- 対局設定は黒白とも `EntryProfile` の ID を持つ。
- SGF の `PB` / `PW` には Player の表示名を保存する。
- 識別子はログ、ファイル名、サーバー連携などで使うために保存する。ただし、相手先ごとの制約が異なるため、このアプリは文字種・文字数を独自に制限しない。

### Player の成立条件（2026-08-12 追記）

この計画でいう **Player** は、`EntryProfile` と `ClientIdentityProfile` の組です。どちらか片方だけでは Player として成立しません。

```text
Player = Entry Profile + Client Identity
```

- Entry Profile: 対局者、Human / Computer、Computer の参照 Engine を決める。
- Client Identity: 今回のサービスでの HANDLE と認証情報を決める。
- Client Identity は、選択した Entry Profile が参照し、かつ LocalMatch または選択中の OnlineMatch (CGOS) 接続先に適合するものだけを選択できる。
- Player 選択 UI は黒白それぞれで Entry Profile と Client Identity の両方を選び、両方の選択結果を表示する。既定 Client Identity は初期候補であり、Player を構成する選択そのものを省略しない。

## データモデル

### EntryProfile

新設する Player の登録情報です。

| 項目 | 内容 |
| --- | --- |
| `Id` | アプリ内部で不変の一意 ID。UUID などを使う。 |
| `DisplayName` | 画面と SGF の `PB` / `PW` に表示する名前。 |
| `Identifier` | ログイン名・ログ名・ファイル名など外部用途の識別子。文字種・文字数は検査しない。 |
| `Kind` | `Human` または `Computer`。 |
| `EngineProfileId` | `Computer` のときに参照する GTP エンジンプロファイル ID。Human は空。 |
| `ClientIdentityProfileIds` | Player が利用可能な Client Identity の ID 一覧。先頭を既定の使用先とする。 |

`Identifier` の妥当性確認は、このアプリでは実施しません。CGOS など各接続先が必要とする検査は、その接続機能が送信直前に行います。

### GtpEngineProfile

既存の責務を維持します。

- 実行ファイル
- 起動引数
- GUI オプション
- GTP ログ設定

Player の表示名やログイン識別子を `GtpEngineProfile` へ追加しません。

### 対局設定

既存の `BlackPlayerKind` / `WhitePlayerKind` と黒白のエンジン選択インデックスを、次の参照へ段階的に置き換えます。

```text
BlackEntryProfileId
WhiteEntryProfileId
```

実行時は Player を解決し、Computer の場合だけ `EngineProfileId` から GTP クライアントを生成します。

## UI 計画

### 対局設定画面

黒白それぞれを次の 1 行へ統合します。

```text
BLACK PLAYER   [表示名 / 種別]                 [SELECT]
WHITE PLAYER   [表示名 / 種別]                 [SELECT]
```

`SELECT` で Player 一覧ダイアログを開きます。Human と Computer を同じ一覧に表示し、選択後の画面は Player の表示名を示します。

### Player 一覧

- Player カードに表示名、種別、識別子を表示する。
- Computer のカードには参照エンジンの表示名も補助表示する。
- `ADD HUMAN` / `ADD COMPUTER`、`EDIT`、`DELETE` を用意する。
- 対局に使用中の Player を削除する場合の扱いは、削除確認または参照の差し替えで保護する。

### Player 編集

- Human: 表示名と識別子を編集する。
- Computer: 表示名、識別子、参照エンジンを編集する。
- 表示名と Engine の間に、既定 Client Identity の表示欄と `SELECT CLIENT IDENTITY` を置く。
- `EDIT CLIENT IDENTITIES` では Client Identity を編集し、`USE` で既定 Client Identity を選ぶ。
- エンジン固有の設定は既存の Engine Settings 画面を開く。
- Identifier にはアプリ独自の文字種・文字数制限を置かない。空欄だけは許可しないか、保存時にユーザーへ分かる警告を出すかを実装時に決める。

## 既存機能との統合

### Random Seed Auto Change

現行の `COMPUTER1` / `COMPUTER2` 表示は、Player 統合後には黒 Player / 白 Player の選択結果から有効性を決めます。

- 選択 Player が Human: Seed 自動変更を Disabled にする。
- 選択 Player が Computer: Seed 自動変更を有効にする。
- 同じ Engine を参照する 2 Player でも、別 Player として選ばれているなら黒白それぞれの設定を表示する。
- ルートコメントには Provider / Black Player / White Player の実際に使った Seed を記録する。

### SGF と棋譜レビュー

- `PB` / `PW`: Player の `DisplayName` を保存する。
- ルートコメント: Provider Seed、黒白 Player Seed、必要なら Player の `Identifier` を人が読める形で記録する。
- 将来、SGF の拡張プロパティを使う場合でも、標準ビューアーで読める `PB` / `PW` を正とする。

### ログとファイル名

- 表示用には `DisplayName`、機械的な識別には `Identifier` を使い分ける。
- Identifier をファイル名へ使う箇所は、OS に依存するファイル名変換をその出力箇所で行う。
- Identifier 自体を保存時に変換・制限しない。

## 段階的な実装手順

1. [完了] `EntryProfile`、永続化 DTO、Entry Catalog を追加する。
2. [一部完了] 起動時に互換 Player を生成する。現在は初期 Player とエンジン対応 Player の生成・旧エンジン ID 補正まで実装済み。旧人間名と黒白エンジン選択の完全移行を追加する。
3. [完了（旧状態併存）] 対局セッションに黒白 Player ID を追加し、Player から Human / Computer とエンジンを解決するアダプターを実装する。
4. [一部完了] Player 名と SGF `PB` / `PW` は切替済み。GTP 起動と Random Seed 記録を Player の参照だけで完結させ、ルートコメントを Black/White Player 名へ更新する。
5. [完了] Player 一覧・編集・選択ダイアログを実装する。
6. [一部完了] Local Match と Ponnuki の設定画面を、新しい `PLAYER SELECT` UI へ置換する。Entry Profile の選択は実装済み。Player を成立させる Client Identity の明示選択は未実装。
7. [未着手] CGOS と大会ルールで必要な Player 選択・Identifier の送信規則を個別に追加する。
8. [未着手] 移行猶予後、旧 `BlackPlayerKind` / `WhitePlayerKind` とエンジン選択インデックスを削除する。
9. [未着手] CGOS 固有の接続先カタログを一般化する。現在の表示名は「接続先（CGOS）」とし、将来の ConnectionProfile には接続種別と EndpointKey を追加する。

## 完了条件

- [完了] Human と Computer を同じ Player 一覧から黒白へ選択できる。
- [未完了] 黒白それぞれについて、選択済み Entry Profile が利用できる Client Identity を選択し、両者を Player の構成要素として表示できる。
- [完了] Computer Player が別 Engine を参照できる。
- [完了] 表示名と Identifier が保存・再読込できる。
- [完了] Identifier はアプリ側で文字種・文字数を制限しない。
- [一部完了] 既存設定を持つ利用者が起動しても、対局できる Player が失われない。初期 Player とエンジン対応 Player は補うが、旧黒白選択の完全な永続移行が残る。
- [未完了] SGF、ログ、Random Seed の表示先が Player 情報と矛盾しない。SGF 名は完了、ログ・Random Seed の Player 名／Identifier 記録と CGOS 連携が残る。

## 最新実装ステータス（2026-08-12）

### 完了

- Human / Computer の Player 統合選択、GTP Engine の選択、Local Match / Ponnuki、SGF 表示名出力。
- ClientIdentityProfile / ClientIdentityCatalog、Player からの Client Identity 参照、CGOS Connection Profile の不変 ID 参照。
- CGOS 実行時の Client Identity 認証情報参照。
- CLIENT IDENTITIES ダイアログの表示、最大5件までの OnlineMatch (CGOS) / LocalMatch Client Identity 追加、削除、行選択、接続先選択。
- Client Identity の `DISPLAY`、`LOGIN NAME`、`LOGIN PASS` をクリック、貼り付け、IME、Enter/Escape、Tab で編集・保存できる。非編集中のパスワードは伏せ字にする。
- LocalMatch Client Identity では `LOGIN NAME` を `OUTPUT NAME` として表示し、パスワード欄を表示しない。
- `USE` により選択 Client Identity を Player の既定使用先にできる。Player 編集画面では既定 Client Identity を表示し、`SELECT CLIENT IDENTITY` / `EDIT CLIENT IDENTITIES` から選択・編集できる。

### 残作業

- [未着手] Player 選択画面を二段階化する。Entry Profile の選択後、その Entry Profile が現在のサービスで使える Client Identity を選び、黒白それぞれの Player を `Entry Profile + Client Identity` として確定する。
- [未着手] Player 選択画面は大きな左右二ペインとする。左半分に Entry Profile 一覧、右半分に左で選択中の Entry Profile が参照する Client Identity 一覧を表示し、同一ダイアログ内で両方を選択・確定する。
- [未着手] OnlineMatch (CGOS) では、ダイアログで確定した Client Identity の Handle と Password を接続開始画面の入力欄へコピーする。元画面での直接編集は今回の接続だけに使う一時ドラフトであり、Client Identity には保存しない。
- [未着手] LocalMatch では、ダイアログで確定した Client Identity の Handle を設定画面へコピーする。設定画面での直接編集は今回の対局・棋譜ファイル名だけに使う一時ドラフトであり、Client Identity には保存しない。Password は扱わない。
- [未着手] Player 関連 UI の操作要素は、常設の `SELECT` / `CHANGE` ボタンを原則置かない。選択リンクと直接編集欄は角丸の太いアンダーラインで表示し、マウスホバーまたは Tab フォーカス時だけ、クリック結果を `CHANGE`（選択ダイアログ）または `EDIT`（直接編集）として示す。START 等の主要アクションボタンは常設して目立たせる。
- [完了] Local Match の開始時に、黒白それぞれの既定 LocalMatch Client Identity の `NAMELY KEY`（旧 OUTPUT NAME）を固定し、手動保存・自動保存する棋譜ファイル名へ反映する。空欄または旧データでは Player の Identifier、表示名の順に補う。
- [完了] OnlineMatch (CGOS) で同じ接続先に複数 Client Identity がある場合、Player 内の並び順で最初の Client Identity を既定として使う。接続・対局開始画面には、その Client Identity の Display と HANDLE を `DEFAULT CLIENT IDENTITY` として表示する。
- [完了] OnlineMatch (CGOS) 接続先の選択 UI を、Client Identity 編集の現在値・前後切替から、接続先一覧を選ぶ一貫した UI へ整理する。
- [完了] Entry Catalog と Client Identity Catalog をまたぐ追加・削除・既定変更の保存を、ひとまとまりの操作として整理する。Client Identity を先に保存し、その後 Player の Client Identity 参照を保存する。

### 操作上の補足（2026-08-12）

- LocalMatch / OnlineMatch (CGOS) ともに、Entry Profile を選ぶと、そのサービス向けの既定 Client Identity を採用する。
- 画面には Client Identity の `HANDLE` を表示する。`HANDLE` をクリックすると、選択中 Player が持つ当該サービス用 Client Identity だけの一覧を開き、今回の対局・接続に限って切り替えられる。
- この一時切替は Player 内の Client Identity 順序を変更しない。恒久的な既定変更には Client Identity 編集画面の `USE` を使う。
- `HANDLE` は「機械に入力できる書式に従った、Player の Entry 名」。CGOS ではログイン名、LocalMatch では棋譜ファイル名に使う。

### 後続

- Client Identity の並べ替え・ページ送りは、最大5件の少数運用のため当面不要。
- LOGIN PASS の OS 保護ストア移行。
- CGOS／大会ルールへの Player 統合、旧 PlayerKind／エンジン選択状態の削除、ConnectionProfile の一般化は、本計画の従来どおりの後続課題。

## 将来の用語整理案（本計画の完了後）

現在の `EntryProfile` / `ClientIdentityProfile` / `GtpEngineProfile` は、互換性を保ちながら本計画を完了するための名称として維持する。本計画の完了後、責務を次のように再編・改名する。

| 将来の名前 | 責務 | 現在の主な対応物 |
| --- | --- | --- |
| `EntryProfile` | 対局に参加する一席。`HumanProfile` または `EngineProfile` のどちらかを参照する。自分同士の対局では二つの Entry を作れる。 | `EntryProfile` |
| `HumanProfile` | 人間一人と一対一のプロフィール。紹介・表示など、人そのものの情報を持つ。 | `EntryProfile` の Human 部分 |
| `EngineProfile` | 思考エンジン一つの起動方法。実行ファイル、作業ディレクトリ、引数、GUI/GTP 設定を持つ。 | `GtpEngineProfile` |
| `ServiceProfile` | CGOS、野良対局、大会各日、LocalMatch など、接続先サービス一つの接続情報。 | `CgosConnectionProfile` と LocalMatch の種別 |
| `ClientIdentity` | 一つの `EntryProfile` が一つの `ServiceProfile` で名乗る身元。 | `ClientIdentityProfile` |

`ClientIdentity` は次の項目を中心にする。`Handle` は、機械に入力できる書式に従った Player の Entry 名である。プロトコルに応じて CGOS ではログイン名、LocalMatch では出力ファイル名として使う。人に見せる `DisplayName` とは別の値であり、UI では原則 `HANDLE` と表示する。

```text
ClientIdentity
  EntryProfileId
  ServiceProfileId       // LocalMatch 等、接続先を持たない形はサービス種別で表す
  Handle
  SecretReference        // OS の安全な資格情報ストアを参照する。秘密そのものは JSON に保存しない。
```

この改名は保存 JSON の移行を伴うため、Player/Client Identity を利用する現在の UI・CGOS 接続・LocalMatch が一通り安定した後に、一回の移行として実施する。
