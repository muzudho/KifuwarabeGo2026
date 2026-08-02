# Codex MCP 起動警告の追加調査メモ

更新日: 2026-08-02

## 現象

Claude Code Extension から Codex CLI v0.146.0 を起動すると、次の警告が表示される。

```text
⚠ MCP startup interrupted. The following servers were not initialized: codex_apps,
  cp-reactivememory-mcp-server, openaiDeveloperDocs
```

Codex セッションと Visual Studio を再起動しても再現する。

## 現在の Codex 設定

対象設定ファイルは `C:\Users\muzud\.codex\config.toml`。

- `node_repl` は登録されていない。
- `cp-reactivememory-mcp-server` は `dnx CP.ReactiveMemory.Mcp.Server@1.* --yes` として登録されている。
- `openaiDeveloperDocs` は `https://developers.openai.com/mcp` として登録されている。
- `windows.sandbox` は `elevated`。
- `features.js_repl` は `false`。
- `codex mcp list` では ReactiveMemory と Developer Docs がともに `enabled` と表示される。

## 実施済みの確認

### codex_apps

- `codex_apps` は `config.toml` に静的定義されていない。
- `codex features list` で `apps` は `stable / true`。
- `codex -c features.apps=false` で一時的に Apps を無効化すると、警告一覧から `codex_apps` は消える。
- したがって `codex_apps` は Codex CLI の Apps 機能が実行時に追加する内部 MCP である。

### ReactiveMemory MCP

- Apps を一時無効化した状態では、`cp-reactivememory-mcp-server` の起動直後に警告が表示され、`openaiDeveloperDocs` も未初期化として連鎖表示される。
- `dnx CP.ReactiveMemory.Mcp.Server@1.* --yes -v diag` で直接起動すると、パッケージ `1.1.2` を検出して MCP サーバーを起動できる。
- JSON-RPC の `initialize` 要求を標準入力へ送ったところ、サーバー `reactivememory-mcp-server 1.1.0.0` の initialize ハンドラーは正常完了した。
- そのため .NET、dnx、NuGet パッケージ、およびサーバー自体の即時起動は正常と確認できた。

### OpenAI Developer Docs MCP

- Apps と ReactiveMemory を一時無効化して単独起動すると、Codex は `Booting MCP server: openaiDeveloperDocs` のまま 30 秒以上待機した。
- `https://developers.openai.com/mcp` への HTTPS 接続は成功し、GET に対して `405 Method Not Allowed` が返った。これは MCP エンドポイントが POST を要求するための正常な到達応答である。
- 正しい JSON-RPC POST による初期化応答は未確認。PowerShell からの curl 引数エスケープで失敗したため、再実施が必要。

## 暫定結論

`node_repl` は今回の原因ではない。

`codex_apps` は警告に含まれるが、Apps を無効化しても ReactiveMemory と Developer Docs の初期化は完了しない。各 MCP を直接起動または HTTPS 接続すると基本的な可用性は確認できるため、Codex CLI v0.146.0 の MCP クライアント起動、起動タイムアウト、環境継承、またはプロキシ処理に原因がある可能性が高い。

なお、バックグラウンドで開始した対話型 Codex を停止すると、未初期化の MCP が警告として表示される。このため、停止時の表示だけで個別 MCP の起動失敗を断定しないこと。

## 次に確認すること

1. `openaiDeveloperDocs` に対する JSON-RPC `initialize` の POST が成功するか確認する。
2. Codex CLI の MCP 起動タイムアウト、プロキシ、環境変数継承に関する設定・診断オプションを確認する。
3. `cp-reactivememory-mcp-server` の Codex 経由起動について、標準エラー出力または詳細ログを取得する。
4. 回避策として Apps、ReactiveMemory、Developer Docs を個別に無効化する場合は、必要な機能を利用しているか確認してから `config.toml` を日時付きバックアップしたうえで変更する。
