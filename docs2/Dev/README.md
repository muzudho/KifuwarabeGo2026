# 開発者向けドキュメント

[文書全体の入口](../README.md) / [開発環境・ビルド](../../README.developer.md) / [開発日誌](Log/README.md)

## 現状と将来

| 知りたいこと | 入口 |
|---|---|
| 今どこまでできているか | [CurrentState / 状況の概要](CurrentState/Overview.md) |
| 現在の構成・仕様・検証結果 | [CurrentState](CurrentState/README.md) |
| これから作るもの・残る確認 | [Plans](Plans/README.md) |
| 以前の設計・調査・引き継ぎ | [Archive](Archive/README.md) |

文書はファイル名ではなく内容で分類します。計画書に実装結果が増えたら `CurrentState` へ、現状説明に将来案が増えたら `Plans` へ分け、相互リンクを付けます。廃止・置換された案は `Archive` へ残します。移動・新規作成する文書には英語のパスを使います。

現状には確認日・対象版・未確認範囲を記し、整理日を検証日とは扱いません。手順書、SDK、参考資料、日誌はそれぞれの目的別の置き場を使います。

## SDK・実装ガイド

- [PlaySpace外部実装SDK](SDK/PlaySpace外部実装SDK.md)
- [プレイヤーエンジン](SDK/プレイヤーエンジン実装ガイド.md)
- [プレイルームGUI](SDK/プレイルームGUI実装ガイド.md)
- [プレイルームエンジン](SDK/プレイルームエンジン実装ガイド.md)
- [ゲームマスターエンジン](SDK/ゲームマスターエンジン実装ガイド.md)
- [既存GTP系のエンジン開発](エンジン開発/README.md)
- [SDK整備計画](Plans/SdkDevelopment.md)

## 運用・参考資料・記録

- [文書の読者分離ルール](運用/文書の読者分離ルール.md)
- [リリース手順](リリース手順.md)
- [Windows GUI手動スモークテスト](Windows%20GUI手動スモークテスト手順.md)
- [トラブルシューティング](Troubleshooting/)
- [参考資料](資料/)
- [コンピューター囲碁用語メモ](コンピューター囲碁用語メモ.txt)
- [CGOS SGFフィールドの由来](CGOS_SGFフィールド由来メモ.txt)
- [完了した作業](完了/)
- [リリースノート](リリースノート/)
- [開発日誌](Log/README.md)
- [個人用メモ](../【むずでょ個人用】/)
