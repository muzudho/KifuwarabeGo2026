# 指定局面エンジン検証結果

最終更新: 2026-08-01

## 判定区分

- 実機確認済み: この作業環境で実行ファイルを起動し、GTP応答と指定局面設定を確認した。
- 自動検査済み: リポジトリのスモークテストで毎回検査する。
- 公式資料確認: 公式プロジェクトの説明またはコマンドリファレンスを確認したが、この環境では実行していない。
- 未確認: 推測を成功扱いしない。

## 結果一覧

| エンジン | version | OS | 実行結果 | 確認した方式 | Profile証拠区分 |
|---|---|---|---|---|---|
| Kifuwarabe Star Random GTP | 2.8.2 | Windows | 実機確認済み、自動検査済み | きふわらべ原子的指定局面 | `BundledEngineVerified` |
| KataGo | 未取得 | 未実行 | 公式資料確認のみ | 実行時能力検査で決定 | `OfficialDocumentationOnly` |
| Leela Zero | 未取得 | 未実行 | 公式資料確認のみ | `loadsgf` が公式READMEに記載。実行時能力検査が必要 | `OfficialDocumentationOnly` |
| GNU Go | 未取得 | 未実行 | 公式資料確認のみ | `fixed_handicap`、`set_free_handicap`、`loadsgf` が公式マニュアルに記載。実行時確認は未実施 | `OfficialDocumentationOnly` |
| その他 | 不明 | 未実行 | 未確認 | Generic Profileで能力検査 | `ConservativeFallback` |

ローカルのリポジトリ、PATH、ビルド出力を検索したが、KataGo、Leela Zero、GNU Goの実行可能ファイルは見つからなかった。外部3エンジンは未実機であり、成功扱いしない。

## 同梱きふわらべの検査経路

Windows非対話スモークは、コピーされた `KifuwarabeGo2026.Engine.exe` をGUI本番と同じ `GtpEngineClient` で外部プロセス起動し、次を実行する。

1. `name` と `version` を取得する。
2. `known_command` と `list_commands` で指定局面能力を検査する。
3. `name` からKifuwarabe Profileを自動選択する。
4. 黒白混在のInitialPositionRequestをConciergeへ渡す。
5. KifuwarabeAtomicSetupStrategyで原子的commitする。
6. `VerifiedSuccess` になったことを確認する。

エンジン内部の回帰検査では、標準置き碁相当の配置、黒白混在、呼吸点のない自由編集形、重複、盤外、開始手番なし、準備中の通常着手拒否、abort、失敗後の通常盤維持も確認する。

## 公式資料から分かる範囲

- KataGo公式リポジトリはGTPモードの起動方法、Windows/Linux向け配布、モデルと設定ファイルが必要なことを説明している。個々の指定局面コマンドは、実行時の能力検査を正とする。
- Leela Zero公式READMEはGTP v2、必須コマンド、tournament subset、`loadsgf` 対応を説明している。起動には `--gtp` とweightsが必要である。
- GNU Go公式GTPコマンドリファレンスは `fixed_handicap`、`set_free_handicap`、`loadsgf` の引数と失敗条件を記載している。

参照:

- https://github.com/lightvector/KataGo
- https://github.com/leela-zero/leela-zero
- https://www.gnu.org/software/gnugo/gnugo_19.html

## 外部エンジンを追試するときの記録

次の項目を追記する。

- エンジン名と `version` 応答
- 配布元とビルド種別
- OS
- 起動引数、設定ファイル、モデルまたはweights
- `known_command` と `list_commands` の結果
- 局面分類と試した方式
- 成功、未検証成功、拒否、不一致、通信失敗の別
- `logs/gtp.log` の該当範囲

Profileの `OfficialDocumentationOnly` は、実機検証できたversionについてだけ別の検証済み証拠へ変更する。エンジン名だけでは成功を保証しない。
