# Kifuwarabe Go 2026 v3.0.0

通常対局の `Go Play` に加えて、外部エンジンが囲碁の遊びや教材を提供できる `Go Apps` を始めたリリースです。最初のアプリとして、9路盤のポン抜きゲームを遊べます。

## 主な変更

- タイトル画面を `Go Play` と `Go Apps` に再編し、利用できる機能を一目で見渡せる構成へ変更
- ポン抜きゲームの紹介、Provider選択、黒白プレイヤー選択、対局、結果までの一連の画面を追加
- アゲハマ20個を先取すると、Providerの終局判定で結果画面へ遷移
- 初期局面を毎回変える乱数シードを自動生成し、GUIログへ記録。明示シードによる再現にも対応
- Providerの能力を再確認する［RECHECK PROVIDER］を追加
- タイトルバーへ `Kifuwarabe Go 2026 | v3.0.0` を表示
- タイトル背景へ、囲碁を表すワイヤーフレームの碁笥と碁石を追加
- 連続対局表示とタイトル画面で見つかった日本語の文字化けを修正

## EngineとGo Apps

同梱の `KifuwarabeGo2026.Engine` が、通常の思考エンジンとGo AppsのProviderを兼ねます。ポン抜きでは、Providerが初期局面の作成、着手の受理、アゲハマの集計、終局判定を担当します。

独自GTP拡張の正式な接頭辞を `kfw-` に統一しました。代表的なGo Appsコマンドは次のとおりです。

- `kfw-make-position ponnuki 1 9 20 [seed]`
- `kfw-listen-move <vertex|pass>`

旧 `gui_` 系とsnake_case系の独自コマンドは、移行用の互換エイリアスとして引き続き受理します。

## エンジン作者向け文書

- ポン抜きゲームのProviderエンジン実装ガイド
- ポン抜きゲームのプレイヤーエンジン実装ガイド

自作エンジンをProviderまたは黒白プレイヤーとして登録し、GUIから利用できます。

## テスト状況

- ソリューション全体のReleaseビルド
- 移植性スモークとWindowsスモーク
- GUI版とEngine版のWindows x64 publish
- 同梱CGOS通信コンポーネントとEngine GTP応答のスモークテスト
- 利用者によるポン抜きゲームの実画面確認

## 配布物

- GUI版: `KifuwarabeGo2026.Gui-v3.0.0-win-x64.zip`
- Engine版: `KifuwarabeGo2026.Engine-v3.0.0-win-x64.zip`

GUIとEngineの両方を使う場合は、2つともダウンロードしてください。

## SHA-256

- GUI版: `FA539A3015DB9A997E102FC4F9F754FB010D5AE318A8093643A5443007B7A828`
- Engine版: `ABB423FC0E783B4157C6D050182995B055E628272573C6FD324B110600AE089B`

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime
- フレームワーク依存配置
