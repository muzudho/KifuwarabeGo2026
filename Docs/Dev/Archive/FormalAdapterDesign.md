# カジュアル・コアとフォーマル・アダプター

> 過去の計画・調査記録。本文の「現在」「次回」、旧名称、チェック欄は記録当時のものです。
> 現状と後続作業は [カジュアル・コアとFormalAdapterの境界](../CurrentState/FormalAdapterBoundaries.md) / [FormalAdapterの継続開発方針](../Plans/FormalAdapterDevelopment.md) を参照してください。


## 目的

この節は[内容別の文書](../CurrentState/FormalAdapterBoundaries.md)へ移しました。

## 三つの実装区分

この節は[内容別の文書](../CurrentState/FormalAdapterBoundaries.md)へ移しました。

## `FormalAdapter` ProjectFamily

きふわらべ公式のフォーマル・アダプターは、次のProjectFamilyへ整理します。

```text
KifuwarabeGo2026.FormalAdapter.{FormalSpecification}.{SubNamespace}
```

候補例：

```text
KifuwarabeGo2026.FormalAdapter.Gtp
KifuwarabeGo2026.FormalAdapter.Cgos
KifuwarabeGo2026.FormalAdapter.Usi
KifuwarabeGo2026.FormalAdapter.Csa
KifuwarabeGo2026.FormalAdapter.Sgf
KifuwarabeGo2026.FormalAdapter.Kif
```

一つの仕様が複数の役割へ接続され、分割する必要が生じた場合は、役割を下位名に含めます。

```text
KifuwarabeGo2026.FormalAdapter.Gtp.PlayerEngine
KifuwarabeGo2026.FormalAdapter.Cgos.GameMasterEngine
KifuwarabeGo2026.FormalAdapter.Sgf.GameRecord
```

物理プロジェクトを必ずこの粒度で分けるという意味ではありません。責務、依存関係、配布単位が小さい間は一つのプロジェクトにまとめ、必要になった時点で同じProjectFamilyの配下へ分割します。

既存の`KifuwarabeGo2026.Reference.Communication.*`などは、実際に責務と依存関係を確認するまで機械的に改名しません。新しい配置へ段階的に移し、既存の実行ファイル名、設定、ランチャー、外部利用者との互換経路を維持します。

2026年8月29日、次のビルド可能なプレースホルダーをソリューションへ追加しました。

```text
KifuwarabeGo2026.FormalAdapter.Gtp
KifuwarabeGo2026.FormalAdapter.Cgos
KifuwarabeGo2026.FormalAdapter.Usi
KifuwarabeGo2026.FormalAdapter.Csa
KifuwarabeGo2026.FormalAdapter.Sgf
KifuwarabeGo2026.FormalAdapter.Kif
```

プレースホルダーは将来の所属先を先に示すためのもので、旧実装を参照するだけの中継アセンブリにはしません。実装を移すまでは空のProjectFamily入口として維持します。

## 継続的な移行方針

この節は[内容別の文書](../Plans/FormalAdapterDevelopment.md)へ移しました。

## フォーマル・アダプターの責務

この節は[内容別の文書](../CurrentState/FormalAdapterBoundaries.md)へ移しました。

## 実行可能なリファレンスとして残すもの

この節は[内容別の文書](../Plans/FormalAdapterDevelopment.md)へ移しました。

## 依存方向

この節は[内容別の文書](../CurrentState/FormalAdapterBoundaries.md)へ移しました。

## 完了の判断

この節は[内容別の文書](../Plans/FormalAdapterDevelopment.md)へ移しました。
