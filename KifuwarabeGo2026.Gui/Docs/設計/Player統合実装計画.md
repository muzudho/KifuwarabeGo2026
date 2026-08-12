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
| Player データと永続化 | 完了 | `PlayerProfile` と `PlayerCatalog` を追加済み。`player-list.json` へ ID、表示名、識別子、種別、参照エンジン ID を保存・再読込する。 |
| 既存設定からの初期移行 | 一部完了 | Player 未登録時に Black/White の Human と、各 GTP エンジンに対応する Computer Player を生成する。旧エンジン ID の再生成による自動 Player の参照切れも表示名ベースで補正する。旧 UI の人間名・黒白エンジン選択を完全に引き継ぐ永続移行は未実装。 |
| 対局セッション | 完了（旧状態併存） | `BlackPlayerProfileId` / `WhitePlayerProfileId` を保持し、選択 Player から Human/Computer と参照エンジンを解決する。互換のため旧 `BlackPlayerKind` / `WhitePlayerKind` とエンジン選択インデックスも同時に更新している。 |
| Local Match / Ponnuki の選択 UI | 完了 | 黒白とも同じ Player 一覧から選択する `PLAYER SELECT` UI を使用する。 |
| Player 一覧・編集 | 完了 | Human/Computer の追加、編集、削除保護、並べ替え、ページ送りを実装。表示名・Identifier はクリック編集、貼り付け、IME、Enter/Escape、Tab に対応する。Computer は参照エンジンを切替え、`EDIT PROFILE` でエンジン設定を開ける。 |
| SGF の対局者名 | 完了 | ローカル棋譜の `PB` / `PW` には選択 Player の `DisplayName` を保存する。 |
| Random Seed | 一部完了 | Human/Computer 判定に追従して自動変更を有効・無効化する。ただし表示とルートコメントはまだ `Player1` / `Player2` で、Player の表示名・Identifier は記録していない。 |
| CGOS と大会ルール | 未着手 | CGOS は GTP エンジン選択と個別ログイン認証のままで、Player の `Identifier` を送信していない。大会ルールにも Player 選択は未接続。 |
| 旧状態の削除 | 未着手 | `BlackPlayerKind` / `WhitePlayerKind` と黒白の GTP エンジン選択インデックスは、互換アダプターとして残っている。 |

## 方針

- `PlayerProfile` は「誰が打つか」を表す。
- `GtpEngineProfile` は「どの実行可能な碁エンジンをどう起動するか」を表す。
- `TargetProfile` は「Player がどの用途・接続先へ、どの Login Name で参加するか」を表す。
- コンピューター Player は、1 個の `GtpEngineProfile` を参照する。
- Player は、使用可能な複数の `TargetProfile` を一方向に参照する。Engine Profile と Target Profile は独立したカタログであり、Player を逆参照しない。
- `TargetProfile.ConnectionProfileId` は接続先（現時点では CGOS）の不変 ID を参照する。正規化済み接続文字列は重複検出・候補選択用であり、参照キーにはしない。
- 同じエンジンを参照しても、表示名・識別子の異なる複数 Player を作れる。
- 対局設定は黒白とも `PlayerProfile` の ID を持つ。
- SGF の `PB` / `PW` には Player の表示名を保存する。
- 識別子はログ、ファイル名、サーバー連携などで使うために保存する。ただし、相手先ごとの制約が異なるため、このアプリは文字種・文字数を独自に制限しない。

## データモデル

### PlayerProfile

新設する Player の登録情報です。

| 項目 | 内容 |
| --- | --- |
| `Id` | アプリ内部で不変の一意 ID。UUID などを使う。 |
| `DisplayName` | 画面と SGF の `PB` / `PW` に表示する名前。 |
| `Identifier` | ログイン名・ログ名・ファイル名など外部用途の識別子。文字種・文字数は検査しない。 |
| `Kind` | `Human` または `Computer`。 |
| `EngineProfileId` | `Computer` のときに参照する GTP エンジンプロファイル ID。Human は空。 |
| `TargetProfileIds` | Player が利用可能な Target の ID 一覧。先頭を既定の使用先とする。 |

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
BlackPlayerProfileId
WhitePlayerProfileId
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
- 表示名と Engine の間に、既定 Target の表示欄と `SELECT TARGET` を置く。
- `EDIT TARGETS` では Target を編集し、`USE` で既定 Target を選ぶ。
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

1. [完了] `PlayerProfile`、永続化 DTO、Player Catalog を追加する。
2. [一部完了] 起動時に互換 Player を生成する。現在は初期 Player とエンジン対応 Player の生成・旧エンジン ID 補正まで実装済み。旧人間名と黒白エンジン選択の完全移行を追加する。
3. [完了（旧状態併存）] 対局セッションに黒白 Player ID を追加し、Player から Human / Computer とエンジンを解決するアダプターを実装する。
4. [一部完了] Player 名と SGF `PB` / `PW` は切替済み。GTP 起動と Random Seed 記録を Player の参照だけで完結させ、ルートコメントを Black/White Player 名へ更新する。
5. [完了] Player 一覧・編集・選択ダイアログを実装する。
6. [完了] Local Match と Ponnuki の設定画面を、新しい `PLAYER SELECT` UI へ置換する。
7. [未着手] CGOS と大会ルールで必要な Player 選択・Identifier の送信規則を個別に追加する。
8. [未着手] 移行猶予後、旧 `BlackPlayerKind` / `WhitePlayerKind` とエンジン選択インデックスを削除する。
9. [未着手] CGOS 固有の接続先カタログを一般化する。現在の表示名は「接続先（CGOS）」とし、将来の ConnectionProfile には接続種別と EndpointKey を追加する。

## 完了条件

- [完了] Human と Computer を同じ Player 一覧から黒白へ選択できる。
- [完了] Computer Player が別 Engine を参照できる。
- [完了] 表示名と Identifier が保存・再読込できる。
- [完了] Identifier はアプリ側で文字種・文字数を制限しない。
- [一部完了] 既存設定を持つ利用者が起動しても、対局できる Player が失われない。初期 Player とエンジン対応 Player は補うが、旧黒白選択の完全な永続移行が残る。
- [未完了] SGF、ログ、Random Seed の表示先が Player 情報と矛盾しない。SGF 名は完了、ログ・Random Seed の Player 名／Identifier 記録と CGOS 連携が残る。

## 最新実装ステータス（2026-08-12）

### 完了

- Human / Computer の Player 統合選択、GTP Engine の選択、Local Match / Ponnuki、SGF 表示名出力。
- TargetProfile / TargetCatalog、Player からの Target 参照、CGOS Connection Profile の不変 ID 参照。
- CGOS 実行時の Target 認証情報参照。
- TARGETS ダイアログの表示、最大5件までの CGOS / LocalMatch Target 追加、削除、行選択、CGOS 接続先の前後切替。
- Target の `DISPLAY`、`LOGIN NAME`、`LOGIN PASS` をクリック、貼り付け、IME、Enter/Escape、Tab で編集・保存できる。非編集中のパスワードは伏せ字にする。
- LocalMatch Target では `LOGIN NAME` を `OUTPUT NAME` として表示し、パスワード欄を表示しない。
- `USE` により選択 Target を Player の既定使用先にできる。Player 編集画面では既定 Target を表示し、`SELECT TARGET` / `EDIT TARGETS` から選択・編集できる。

### 残作業

- Local Match の開始設定で、黒白それぞれの LocalMatch Target を明示選択し、実際の出力ファイル名へ `OUTPUT NAME` を反映する。
- CGOS で同じ接続先に複数 Target がある場合、既定 Target の選択規則を接続・対局開始の画面へ明示表示する。
- CGOS 接続先の選択 UI を、Target 編集の現在値・前後切替から、接続先一覧を選ぶ一貫した UI へ整理する。
- Player Catalog と Target Catalog をまたぐ追加・削除・既定変更の保存を、ひとまとまりの操作として整理する。

### 後続

- Target の並べ替え・ページ送りは、最大5件の少数運用のため当面不要。
- LOGIN PASS の OS 保護ストア移行。
- CGOS／大会ルールへの Player 統合、旧 PlayerKind／エンジン選択状態の削除、ConnectionProfile の一般化は、本計画の従来どおりの後続課題。

## 将来の用語整理案（本計画の完了後）

現在の `PlayerProfile` / `TargetProfile` / `GtpEngineProfile` は、互換性を保ちながら本計画を完了するための名称として維持する。本計画の完了後、責務を次のように再編・改名する。

| 将来の名前 | 責務 | 現在の主な対応物 |
| --- | --- | --- |
| `EntryProfile` | 対局に参加する一席。`HumanProfile` または `EngineProfile` のどちらかを参照する。自分同士の対局では二つの Entry を作れる。 | `PlayerProfile` |
| `HumanProfile` | 人間一人と一対一のプロフィール。紹介・表示など、人そのものの情報を持つ。 | `PlayerProfile` の Human 部分 |
| `EngineProfile` | 思考エンジン一つの起動方法。実行ファイル、作業ディレクトリ、引数、GUI/GTP 設定を持つ。 | `GtpEngineProfile` |
| `ServiceProfile` | CGOS、野良対局、大会各日、LocalMatch など、接続先サービス一つの接続情報。 | `CgosConnectionProfile` と LocalMatch の種別 |
| `ClientIdentity` | 一つの `EntryProfile` が一つの `ServiceProfile` で名乗る身元。 | `TargetProfile` |

`ClientIdentity` は次の項目を中心にする。`PresentedName` はプロトコルに応じて CGOS ではログイン名、LocalMatch では出力ファイル名として使う。UI 表示だけはサービスに応じて `LOGIN NAME` / `OUTPUT NAME` とする。

```text
ClientIdentity
  EntryProfileId
  ServiceProfileId       // LocalMatch 等、接続先を持たない形はサービス種別で表す
  PresentedName
  SecretReference        // OS の安全な資格情報ストアを参照する。秘密そのものは JSON に保存しない。
```

この改名は保存 JSON の移行を伴うため、Player/Target を利用する現在の UI・CGOS 接続・LocalMatch が一通り安定した後に、一回の移行として実施する。
