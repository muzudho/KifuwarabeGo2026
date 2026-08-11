# PLAYER 統合実装計画

最終更新: 2026-08-11

## 目的

現在の対局設定は、黒白それぞれについて次の 2 段階です。

1. `HUMAN` または `COMPUTER` を選ぶ。
2. `COMPUTER` のときだけ GTP エンジンを選ぶ。

これを、対局者を共通に扱う `PLAYER` の選択へ統合します。人間とコンピューターを同じ一覧から選べるようにし、表示名と外部連携用の識別子をあらかじめ登録できるようにします。

## 方針

- `PlayerProfile` は「誰が打つか」を表す。
- `GtpEngineProfile` は「どの実行可能な碁エンジンをどう起動するか」を表す。
- コンピューター Player は、1 個の `GtpEngineProfile` を参照する。
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

1. `PlayerProfile`、永続化 DTO、Player Catalog を追加する。
2. 起動時に既存の人間名・黒白エンジン選択から互換 Player を生成する移行処理を追加する。
3. 対局セッションに黒白 Player ID を追加し、Player から Human / Computer とエンジンを解決するアダプターを実装する。
4. GTP 起動、Player 名、SGF `PB` / `PW`、Random Seed 記録を Player 経由へ切り替える。
5. Player 一覧・編集・選択ダイアログを実装する。
6. Local Match と Ponnuki の設定画面を、新しい `PLAYER SELECT` UI へ置換する。
7. CGOS と大会ルールで必要な Player 選択・Identifier の送信規則を個別に追加する。
8. 旧 `BlackPlayerKind` / `WhitePlayerKind` とエンジン選択インデックスを、移行猶予後に削除する。

## 完了条件

- Human と Computer を同じ Player 一覧から黒白へ選択できる。
- Computer Player が別 Engine を参照できる。
- 表示名と Identifier が保存・再読込できる。
- Identifier はアプリ側で文字種・文字数を制限しない。
- 既存設定を持つ利用者が起動しても、対局できる Player が失われない。
- SGF、ログ、Random Seed の表示先が Player 情報と矛盾しない。
