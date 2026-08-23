# Kifuwarabe Go 2026 v3.1.0

自作GTPエンジンを［きふわらべの碁２０２６］へ接続したい作者が、外部の仕様書を探さず実装へ着手できる公開リファレンスと、Go Appsの用途別エンジンオプションプロトコルを追加したリリースです。

## 囲碁エンジン作者向け公開リファレンス

- リポジトリのトップREADMEで、［囲碁エンジンを作りたい人向け］を本体開発参加者向け案内より上へ配置
- `PublicDocs/GoApps/Play`へ、最小GTPエンジン、コマンド、対局シーケンス、エンジンオプションの自己完結した仕様を追加
- `PublicDocs/GoApps/Ponnuki`へ、ProviderとPlayerを分けた実装リファレンスを追加
- Playとポン抜きの各入口へ、トップREADMEから1クリックで移動できる目次を追加
- アプリIDを実装者やエンジンの所有名ではなく、相互運用する種類名として扱う共通規約を明文化

## 新しいエンジンオプションプロトコル

Go Appと`player`／`provider`の役割を指定する、次の正規コマンドを追加しました。

```text
kfw-describe-options <app-id> <role>
kfw-get-options <app-id> <role>
kfw-patch-options <app-id> <role> <json>
kfw-invoke-option <app-id> <role> <option-id>
```

- 型を`boolean`、`integer`、`enum`、`string`、`file`、`action`へ整理し、JSON本来の真偽値、整数、文字列を使用
- `kfw-patch-options`は差分更新とし、全項目が有効な場合だけ一括反映。一項目でも失敗した場合は何も変更せずJSONエラーを返却
- 副作用を持つ`action`をパッチから分離し、`kfw-invoke-option`で独立実行
- `immediate`、`next-game`、`restart`の適用タイミングをスキーマへ追加
- アプリ非対応と、対応アプリにオプションがない状態を別の応答として区別
- GUI、CGOS、Play Player、Ponnuki Player、Ponnuki Providerで新形式を優先使用
- 旧`kfw-options`、`kfw-get-option`、`kfw-set-option`は移行互換として継続し、新形式に非対応のエンジンへ自動フォールバック

## SGF互換性と開発支援

- 旧解析プロパティ`KFA`を現行の`KFW`へ変換して保存する互換処理を追加
- CodexのMCP起動警告について、原因調査と復旧手順をトラブルシューティング文書へ追加
- WindowsSmokeへ、型付きJSON、原子的ロールバック、action分離、新旧互換、GUI外部プロセス適用の検査を追加
- PortabilitySmokeへ、旧`KFA`から`KFW`への変換検査を追加

## テスト状況

- ソリューション全体のReleaseビルド
- 移植性スモークとWindowsスモーク
- GUI版とEngine版のWindows x64 publish
- GUI、Core、Shared、Match、GtpExtensions、CGOS、Engineのファイルバージョン確認
- 同梱CGOS通信コンポーネントとEngine GTP応答のスモークテスト

## 配布物

- GUI版: `KifuwarabeGo2026.GameOasis.Gui-v3.1.0-win-x64.zip`
- Engine版: `KifuwarabeGo2026.Engine-v3.1.0-win-x64.zip`

GUIとEngineの両方を使う場合は、2つともダウンロードしてください。

## SHA-256

- GUI版: `4907298D86D3C512358C8A47BFA842C19946D295851A369CE53FC3D1EB3B79E4`
- Engine版: `162BF0FF6A552B1CB53844ACC4A921210BF5304DF3484FFF2A8F90220FEC2AB6`

## 動作環境

- Windows x64
- .NET 8 Desktop Runtime
- フレームワーク依存配置
