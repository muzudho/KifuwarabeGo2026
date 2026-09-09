# Codex MCP 起動警告のトラブルシューティング

## 現象

Claude Code Extension から Codex を起動した際、次の警告が表示された。

```text
⚠ MCP startup interrupted. The following servers were not initialized: codex_apps,
  cp-reactivememory-mcp-server, node_repl, openaiDeveloperDocs
```

`windows.sandbox` を `unelevated` から `elevated` に切り替えた後にも、同じ警告が再発した。

## 原因

ユーザー共通の Codex 設定ファイル `C:\Users\muzud\.codex\config.toml` に、Codex Desktop のブラウザー／Computer Use 連携用 MCP サーバーである `node_repl` が登録されていた。

`node_repl` は Desktop 起動中に作られる名前付きパイプへの接続を必要とする。Claude Code Extension から起動する Codex CLI ではその接続先が存在しないため、`node_repl.exe` が初期化できず、MCP 起動処理全体が中断された。

`elevated` への切替操作では、共有設定に `node_repl` が不完全な状態で自動再登録された。実行ファイルと一部の環境変数だけが登録され、Desktop 連携用の名前付きパイプ設定が存在しなかった。

このため、警告に `cp-reactivememory-mcp-server` と `openaiDeveloperDocs` も含まれていたが、これらが直接の原因ではない。

## 確認方法

PowerShell で登録済みの MCP サーバーを確認する。

```powershell
codex mcp list
```

`node_repl` が表示される場合、共有設定に登録されている。

設定ファイル中の登録箇所は次の形式である。

```toml
[mcp_servers.node_repl]
command = '...\\node_repl.exe'

[mcp_servers.node_repl.env]
# NODE_REPL_* など
```

Desktop 連携用パイプの有無は次で確認できる。

```powershell
Get-ChildItem '\\.\pipe\' | Where-Object Name -like 'codex-computer-use-*'
```

Codex Desktop を使わずに Claude Code Extension から Codex を起動している場合、通常は該当パイプが存在しない。

## 対処

1. `C:\Users\muzud\.codex\config.toml` を日時付きの `.bak` ファイルへ退避する。
2. `config.toml` から次の節全体を削除する。
   - `[mcp_servers.node_repl]`
   - `[mcp_servers.node_repl.env]`
3. `windows.sandbox = "elevated"` または `windows.sandbox = "unelevated"` の希望する設定は残す。
4. 新しい Codex チャットを開始する。
5. `codex mcp list` に `node_repl` が含まれないことを確認する。

削除後に残る MCP の例は次のとおり。

- `cp-reactivememory-mcp-server`
- `openaiDeveloperDocs`

## 再発時の注意

sandbox の切替などで `config.toml` が更新されると、`node_repl` が再登録される場合がある。その際は `node_repl` の節だけを再度退避・削除する。

`node_repl` は Codex Desktop 用のセッション依存連携であり、Claude Code Extension 用の共有 MCP 設定として手動追加しない。