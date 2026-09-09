# SGF文書モデルの実装状況

2026-08-29の実施記録では、文書モデル、パーサー、ライター、Go変換、GUI読込・保存接続まで完了しています。変化図のGUI編集は未着手です。

文書モデルの損失なし往復と、GUI互換 `GoGameRecord` への主手順だけの投影は区別します。[後続計画](../Plans/SgfEditing.md)。

## 責務の境界

- `MatchActionRecord` は、対局中に受理された操作の事実を保持する。
- SGF文書モデルは、ゲーム木、コメント、勝率、評価値、読み筋、独自情報を保持する。
- 現行 `GoGameRecord` は、移行中のGUI互換モデルとして残す。

## 文書構造

```text
SgfDocument
└─ SgfGameTree
   └─ SgfNode
      └─ SgfProperty
```

## 出典

- [Match分離実装計画](../完了/20260731_Matchプロジェクト分離実装計画.md)
- [きふわらべ式SGF形式仕様](SgfAnalysisFormat.md)

## 元の検討経緯

[分割前の計画・調査記録](../Archive/SgfDocumentPlan.md)
