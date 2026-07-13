# KifuwarabeGo2026.Communication.Cgos

CGOS の練習サーバーへ接続し、CGOS の行ベースプロトコルを既存の GTP エンジン `KifuwarabeGo2026.Engine` へ中継する通信クライアントです。

既定の接続先:

- Host: `uec-go.com`
- Port: `6809`
- Black account: `KifuwarabeB`
- White account: `KifuwarabeW`

黒番アカウントで接続:

```powershell
dotnet run --project KifuwarabeGo2026.Communication.Cgos -- --account black
```

白番アカウントで接続:

```powershell
dotnet run --project KifuwarabeGo2026.Communication.Cgos -- --account white
```

黒番・白番の両方で接続:

```powershell
dotnet run --project KifuwarabeGo2026.Communication.Cgos -- --both
```

ログアウト:

接続中の端末で `Ctrl+C` を押してください。クライアントは CGOS へ `quit` を送ってから切断し、起動中の GTP エンジンも終了します。

publish 済みエンジンを使う場合:

```powershell
dotnet run --project KifuwarabeGo2026.Communication.Cgos -- --account black --engine-command ".\KifuwarabeGo2026.Engine\bin\Release\net8.0\win-x64\publish\KifuwarabeGo2026.Engine.exe"
```

ログは既定で `Logs\Cgos` に出力します。GUI から起動した通信プロセスの標準エラー出力は `standard-error-*.log` に、GTP エンジンの標準エラー出力は `gtp-*.log` に、それぞれ `# [StandardError] ` 付きで保存します。パスワードはログに出しません。
