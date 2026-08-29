# Smart App Controlによる再生成DLLブロック調査

調査日：2026年8月29日

## 結論

この端末で再生成した.NET DLLが`0x800711C7`で読み込めない原因は、Codex、.NET SDK、リポジトリーのアクセス権ではなく、Windows 11のSmart App Controlが強制モードで動作していることです。

再起動は必要な解決手順ではありません。再起動後に一部のDLLが実行できた事例はありますが、再生成した別ハッシュの未署名DLLは再び拒否されています。したがって「再起動すれば生成物が許可される」という再現可能な仕組みは確認できず、信頼判定またはキャッシュの変化による一時的な現象と判断します。

ローカル開発を継続する場合の現実的な恒久策は、次のいずれかです。

1. Smart App Controlを維持し、このリポジトリーのビルドと試験をSmart App Control非強制の開発用VMで行う。
2. 信頼されたコード署名サービスまたは証明書で、実行するすべてのEXEとDLLを署名する。
3. このPCを開発機として扱い、影響を理解したうえでSmart App Controlをオフにする。

本調査ではセキュリティ設定を変更していません。

## 端末で確認した事実

### Smart App Controlは強制モード

レジストリーの状態は次のとおりでした。

```text
HKLM\SYSTEM\CurrentControlSet\Control\CI\Policy
VerifiedAndReputablePolicyState = 0x1
```

Microsoftの説明では`1`は強制、`2`は評価、`0`はオフです。

Code Integrityイベントが報告したポリシーIDは次の値でした。

```text
{0283ac0f-fff1-49ae-ada1-8a933130cad6}
```

Microsoftの[受信トレイApp Controlポリシー一覧](https://learn.microsoft.com/ja-jp/windows/security/application-security/application-control/app-control-for-business/operations/inbox-appcontrol-policies)では、このIDはSmart App ControlがオンのWindows 11で有効になる`VerifiedAndReputableDesktop`基本ポリシーです。対応するポリシーファイルも端末上に存在しました。

```text
C:\Windows\System32\CodeIntegrity\CiPolicies\Active\{0283ac0f-fff1-49ae-ada1-8a933130cad6}.cip
```

OSのビルド番号は`26200.9168`、表示バージョンは`25H2`でした。一部の旧APIは製品名を`Windows 10 Home`と返しましたが、ビルド番号、Smart App Controlポリシー、設定構造はいずれもWindows 11のものです。

### Code Integrityが.NETのDLL読込みを拒否

`Microsoft-Windows-CodeIntegrity/Operational`ログに、イベント3033と3077が同じActivity IDで記録されました。代表例は次のとおりです。

```text
Process:
  C:\Program Files\dotnet\dotnet.exe

Blocked file:
  KifuwarabeGo2026.Tests.GameOasis.Gui.Windows.dll

Reason:
  did not meet the Enterprise signing level requirements

Policy ID:
  {0283ac0f-fff1-49ae-ada1-8a933130cad6}
```

イベント3077はApp Control強制モードのブロックイベントです。Microsoftの[App Controlデバッグガイド](https://learn.microsoft.com/en-us/windows/security/application-security/application-control/app-control-for-business/operations/appcontrol-debugging-and-troubleshooting)も、3077のファイル名、親プロセス、要求署名レベル、検証署名レベルを使って拒否理由を調べるよう案内しています。

同じポリシーによる拒否は、Windows専用試験だけでなく、移植性試験、PlayRoom Host、GTP拡張DLLなど、再生成された複数の成果物で発生していました。特定プロジェクトのコード不具合ではありません。

### 対象DLLは未署名で、由来の信頼情報もない

調査対象DLLの状態は次のとおりでした。

```text
File:
  KifuwarabeGo2026.Tests.GameOasis.Gui.Windows.dll

SHA-256:
  1D1C0C1FF9BC4B727567B3C8B974536917819D1930E00B2B45BAE978693EFF2E

Authenticode:
  NotSigned

Alternate data streams:
  :$DATA only

Extended attributes:
  none
```

`Zone.Identifier`はなく、インターネットからダウンロードしたファイルとして拒否されたわけではありません。一方、Managed InstallerまたはIntelligent Security Graphの信頼を記録する`$KERNEL.SMARTLOCKER.ORIGINCLAIM`拡張属性もありませんでした。Microsoftは[Managed Installerの説明](https://learn.microsoft.com/en-us/windows/security/application-security/application-control/app-control-for-business/design/configure-authorized-apps-deployed-with-a-managed-installer)で、この由来情報がカーネル管理の拡張属性として記録されると説明しています。

## なぜビルドは成功して実行だけ失敗するのか

Smart App Controlはファイルの作成を禁止する機能ではなく、実行またはDLL読込み時にコードの信頼性を評価します。このため、`dotnet build`は警告0件・エラー0件で完了しても、その直後の`dotnet <test.dll>`がOSローダーで拒否されます。

Microsoftの[Smart App Control概要](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/overview)によると、強制モードではMicrosoftのアプリインテリジェンスで安全と認識されるか、信頼された証明書で署名されたバイナリだけが実行できます。未知の未署名コードは既定で拒否されます。

.NET DLLも例外ではありません。App Controlの[ポリシールール説明](https://learn.microsoft.com/ja-jp/windows/security/application-security/application-control/app-control-for-business/design/select-types-of-rules-to-create)では、動的コードセキュリティが.NETアプリケーションと動的に読み込まれるライブラリーへ適用されることが明記されています。

## なぜ再生成のたびに再発するのか

コンパイルするとDLLの内容とハッシュが変わります。未署名ファイルには継続的な発行者IDがないため、新しいハッシュは新しい未知のコードとして評価されます。

Microsoftの[Windowsアプリ開発者向けレピュテーション説明](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation)では、未署名ファイルは更新のたびに評価を一から構築し、以前の版の評価を引き継げないと説明しています。また、Smart App Controlの署名検査はインターネットから取得したファイルだけでなく、すべての実行可能ファイルへ適用されます。

このため、あるビルドのDLLが一時的に通っても、次のビルドは別物として再び拒否され得ます。

## 再起動についての訂正

以前は、再起動後に保留中の試験が通ったため、再起動を回避策として案内しました。しかし今回の調査では、Smart App Control強制ポリシーは再起動後も有効であり、次に再生成した未署名DLLは同じポリシーで拒否されました。

したがって、次を正式な判断とします。

* 再起動はSmart App Controlの恒久的な許可を作らない。
* 再起動後の成功は、特定ハッシュに対するクラウド評価、キャッシュ、または実行時状態が変わった可能性はあるが、今回の証拠だけでは一つに特定できない。
* 試験手順へ「DLL再生成後はPCを再起動する」を組み込んではならない。
* `CiTool --refresh`はポリシー更新を反映する管理コマンドであり、未知の未署名DLLを許可するコマンドではない。

Microsoftは[ISGの説明](https://learn.microsoft.com/en-us/windows/security/application-security/application-control/app-control-for-business/design/use-appcontrol-with-intelligent-security-graph)で、未許可バイナリのハッシュと署名情報をクラウド評価し、その判断が変わり得ること、端末が評価結果を保持することを説明しています。これは再起動前後で結果が変わった可能性を説明できますが、成功した個々のDLLにどの許可理由が適用されたかを示すイベントは取得できていないため、推定に留めます。

## 恒久策の比較

### 1. 開発用VMを使う

推奨度：高

Smart App ControlをホストPCで維持しながら、Smart App Controlが強制されていないWindows開発用VMでビルドと試験を行います。製品利用環境としての強制モード試験は、署名済みRelease候補だけをホストPCで行えます。

長所：ホストPCの保護を下げず、Debug／Releaseの反復試験を妨げません。

短所：VMの準備とディスク容量が必要です。MonoGameの画面試験ではGPUやフォントの差も確認します。

### 2. 成果物を信頼された証明書で署名する

推奨度：Release配布には高、日常の全プロジェクト署名には中

Microsoftの[Smart App Control署名ガイド](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/code-signing-for-smart-app-control)は、信頼されたプロバイダーのRSAコード署名証明書を使用するよう案内し、Microsoft Trusted Signingを推奨しています。

単に自己署名証明書をローカルへ入れるだけでは、Smart App Controlが要求する公開信頼を満たすとは限りません。また、このソリューションは多数のDLLを生成するため、テスト対象を含む依存DLLすべての署名、タイムスタンプ、署名後にファイルを変更しないビルド順序が必要です。

### 3. Smart App Controlをオフにする

推奨度：このPCを専用開発機にする場合のみ検討

Windows設定の`プライバシーとセキュリティ` → `Windows セキュリティ` → `アプリとブラウザー コントロール` → `Smart App Control`からオフにできます。

重要：Microsoftの[Smart App Controlテストガイド](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/test-your-app-with-smart-app-control)によると、設定画面でオンからオフへ変更する操作は一方向です。通常、再びオンへ戻すにはWindowsのリセットまたは再インストールが必要です。調査だけを目的に安易にオフへ変更してはいけません。

### 採用しない案

* 毎回PCを再起動する：恒久的な信頼にならず、再現性がありません。
* DLLを別フォルダーへコピーする：Smart App Controlはパスだけでなく署名と評価を確認するため、根本解決になりません。
* `Zone.Identifier`を削除する：対象DLLには最初から存在しません。
* リポジトリーへOSポリシー回避コードを入れる：製品コードの責務ではなく、保護を迂回する設計になります。
* Microsoftの`.cip`ファイルを直接置換する：公式資料に開発者向け監査試験手順はありますが、管理者権限、BitLocker、回復環境を伴い、日常開発の解決策として危険です。

## 今後の運用

1. `0x800711C7`が出たら、テスト失敗と混同せずCode Integrityイベント3077の対象ファイルを記録する。
2. 全ソリューションのコンパイル結果と、起動できた試験、OSに拒否された試験を分けて報告する。
3. OS拒否時に再起動を自動的な次手としない。
4. 開発用VM、信頼された署名、Smart App Control無効化のどれを採用するか利用者が決定するまで、OS設定を変更しない。
5. Release配布前には、署名済み発行物をSmart App Control強制環境で起動確認する。

## 再確認用の読み取りコマンド

次のコマンドは状態を変更しません。

```powershell
reg.exe query "HKLM\SYSTEM\CurrentControlSet\Control\CI\Policy" /v VerifiedAndReputablePolicyState

Get-WinEvent -FilterHashtable @{
    LogName = 'Microsoft-Windows-CodeIntegrity/Operational'
    Id = 3033, 3076, 3077, 3089
    StartTime = (Get-Date).AddHours(-1)
} | Select-Object TimeCreated, Id, ActivityId, Message

Get-AuthenticodeSignature -FilePath '<対象DLL>'
Get-FileHash -Algorithm SHA256 -LiteralPath '<対象DLL>'
Get-Item -LiteralPath '<対象DLL>' -Stream *
fsutil file queryEA '<対象DLL>'
```

管理者端末では`CiTool.exe -lp`でもポリシー一覧を確認できます。この調査セッションでは権限不足で`0x80070005`となりましたが、ポリシーID、レジストリー状態、ポリシーファイル、Code IntegrityイベントからSmart App Control強制モードは独立に確定できました。
