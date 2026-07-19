# 【むずでょ個人用】CGOSとの通信について


## 接続確認

```powershell
# ビルド
dotnet build KifuwarabeGo2026.slnx

# 接続
dotnet run --project KifuwarabeGo2026.Communication.Cgos -- --help

# 黒番で接続
dotnet run --project KifuwarabeGo2026.Communication.Cgos -- --account black
```
