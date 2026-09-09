# FormalAdapter移行後の最終検証

2026-08-30の引き継ぎに残る判定です。別作業でWindows試験がPASSした記録もあるため、現在の環境と対象変更を先に照合します。この文書整理では再実行していません。

## 実装再開地点

```text
現在の状態：作業段階0～6・8・9完了。作業段階7は実装完了、Windows非対話最終回帰試験だけSmart App Control対応方針の決定待ち
次の最小作業：開発用VM、信頼されたコード署名、Smart App Control無効化のいずれかを選び、Windows非対話試験を再実行する
次の実装候補：なし。機能移行と移行後境界の固定は完了。Smart App Control対応方針の決定後、第7段階のWindows最終回帰試験を行う
移行先：KifuwarabeGo2026.FormalAdapter.Gtp、FormalAdapter.Gtp.PlayerEngine、利用側、試験、発行スクリプト、開発者文書
禁止事項：GTPプロジェクト全体の一括改名、CGOS Hostの一括分解、SgfGameRecordConverterの型ごとの単純移動を同時に行わない
```

## 元の検討経緯

[分割前の計画・調査記録](../Archive/FormalAdapterMigration.md)
