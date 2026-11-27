# Smart App Control 対策の保管

2026-11-27、開発環境は SAC をオフにする運用となったため、以前の対策をここへ退避しました。このフォルダーのコードは通常のビルドから呼び出されません。

- `SmartAppControl.targets`: 旧 `Directory.Build.targets` の開発用署名設定。自動読込されない名前で保管しています。
- `New-DevelopmentCodeSigningCertificate.ps1`: 開発用署名証明書の作成と信頼登録。
- `Sign-SmartAppControlDevelopmentOutput.ps1`: EXE / DLL の署名。
- `Save-SmartAppControlDiagnostic.ps1`: 署名状態や起動結果の診断記録。

署名を再利用する場合は、証明書を確認し、対象プロジェクトから `SmartAppControl.targets` を明示的に Import し、`SmartAppControlSigningEnabled=true` を指定する必要があります。

退避時に OS の SAC 設定、証明書ストア、保存済み環境変数は変更していません。過去の調査記録は `docs/Dev/Troubleshooting/SmartAppControl/` に残しています。
