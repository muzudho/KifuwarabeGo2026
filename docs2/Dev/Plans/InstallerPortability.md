# インストーラー保守のOS別拡張構想

元は2026-08-24のランチャー更新計画（v4.1.0予定からv4.0.1へ前倒し）の将来OS拡張です。本文のLauncher名・v4.1.0は当時の設計表記で、現在の公開予定版ではありません。[Installerの現状](../CurrentState/Installer.md)を基準に再評価します。

## クロスプラットフォーム拡張方針

v4.1.0の正式対象はWindows x64ですが、Application層では「Windowsショートカット」ではなく「ランチャー起動エントリー」として扱います。`.lnk`、`.desktop`、macOS固有形式などのファイル構造を共通モデルへ持ち込みません。

```text
Application
  LauncherEntryRegistration
  LauncherEntryInspection
  LauncherEntryUpdateSession
  ILauncherEntryService
  ILauncherInstallationPlatform
             ▲
             │ OS非依存契約
     ┌───────┼────────┐
     │       │        │
 Windows   Linux    macOS
 .lnk      .desktop  Alias／アプリ入口（将来調査）
```

Applicationが使用する起動エントリーの共通情報は、次の範囲に限定します。

- 安定した登録ID
- 利用者向け表示名
- OS実装だけが解釈する不透明な場所識別子
- 検査状態
- 現在のリンク先を表す正規化済み表示値
- 更新可否と、更新できない理由
- 確認待ち、更新済み、スキップ、保留、失敗などの処理状態

共通契約に`.lnk`パス、Windows COM型、Linuxデスクトップエントリーのキー、macOS BookmarkやAliasの型を追加しません。OS固有の読込結果は各アダプター内で保持し、Applicationへは共通状態だけを返します。

### OS別アダプターの責務

| 責務 | Windows v4.1.0 | Linux将来版 | macOS将来版 |
|---|---|---|---|
| 利用者単位の配置場所 | LocalApplicationData | XDG Base Directory準拠の場所 | Application Support配下の場所 |
| 起動エントリー候補 | 通常の`.lnk` | `.desktop`候補 | Alias、アプリケーション入口などを調査して決定 |
| リンク先の読込・変更 | Shell Link API | Desktop Entry仕様に従う実装 | 採用形式のOS APIに従う実装 |
| 実行中判定 | Windowsプロセス実装 | Linuxプロセス実装 | macOSプロセス実装 |
| 実行ファイル名 | `.exe` | 拡張子なしを基本 | `.app`構成または採用する配布形式 |
| Release資産 | `win-x64` | 対象RIDごとの資産 | 対象RIDごとの資産 |

macOSの更新対象をAlias、`.app`、Dock項目のどれにするかは、実装着手前にOSの正式な配布・署名・公証方式と合わせて調査します。Windowsの`.lnk`設計をそのままmacOSへ模倣しません。Linuxでもデスクトップ環境ごとの差異があるため、最初はFreedesktop準拠の通常`.desktop`だけを候補とし、各デスクトップ固有のピン留めは別能力として扱います。

### 能力による画面構成

Presentationは`OperatingSystem.IsWindows()`などで画面分岐しません。構成時に注入されたプラットフォーム能力を参照します。

```text
LauncherMaintenanceCapabilities
  CanInstallLauncher
  CanDiscoverLauncherEntries
  CanRegisterLauncherEntry
  CanRewriteLauncherEntry
  SupportsAutomaticRollback
  MaximumRegisteredEntries
  UnsupportedReason
```

未実装OSではボタンを消して処理を暗黙に無効化するのではなく、「このOSではランチャー起動エントリーの自動更新は未対応です」と理由を表示します。パッケージの取得・検証だけ対応できるOSでは、配置まで実行して手動入口作成を案内できるよう、能力を一つの真偽値にまとめません。

最大登録件数5件はv4.1.0の製品仕様としてApplicationが所有します。OSアダプターが件数や確認順序を独自に変更しません。一件ずつ［はい］［いいえ］を尋ねる状態機械も共通であり、OS実装は指定された一件を検査・更新して結果を返すだけです。

### 配置場所とRelease資産の抽象化

Application内に`%LOCALAPPDATA%`、`win-x64`、`.exe`を文字列として固定しません。プラットフォーム実装から、次をまとめた記述子を受け取ります。

- 管理ルート
- `Current`、`Previous`、`Versions`、一時作業場所
- 対象RID
- Release資産名
- 起動ファイルの相対パス
- ファイル版またはパッケージ版の検証方法
- プロセス起動・生存確認方法

ZIPの安全展開、SHA-256、staging、状態遷移は共通処理とし、パス比較規則、実行可能形式、署名、公証、実行権限などOS差がある検査だけをアダプターへ委譲します。

### プロジェクト分割の発展形

v4.1.0では既存構成を増やしすぎず、Windows実装を`KifuwarabeGo2026.GameOasis.Gui.Windows`の構成点から注入できます。ただし、OS固有クラスは`LauncherMaintenance/Windows`など明確な名前空間とフォルダーへ隔離します。

二つ目のOS実装へ着手する時点で、次のいずれかへ物理分割します。

```text
KifuwarabeGo2026.GameOasis.Platform.Windows
KifuwarabeGo2026.GameOasis.Platform.Linux
KifuwarabeGo2026.GameOasis.Platform.MacOS
```

または既存のOS別GUI起動プロジェクトが十分に薄い場合は、各`GameOasis.Gui.{OS}`を構成ルートとして維持します。どちらの場合もApplication／Storageの共通ユースケースをコピーせず、OSアダプターだけを追加します。

参照方向は必ず次を守ります。

```text
OS別起動・アダプター ──> GUI Presentation ──> GameOasis.Application
          │                                      ▲
          └────────────> GameOasis.Storage ──────┘
```

ApplicationからWindows、Linux、macOSの各プロジェクトを参照しません。Storageの共通文書形式もOS固有APIを参照しません。

### OSアダプター契約試験

共通の契約試験一式を作り、各OSアダプターへ同じシナリオを適用できるようにします。

- 候補の列挙が読取専用である
- 未登録項目を勝手に変更しない
- 現在のリンク先を検査できる
- 承認された一件だけを更新する
- 引数など、そのOSで維持対象とした属性を保存する
- 中断または失敗時に元の起動エントリーを壊さない
- 管理ルート外のパッケージファイルを変更しない
- 空白、非ASCII文字、長いパスを扱う
- 未対応能力が明示的な理由を返す

Windowsでは一時`.lnk`を使う結合試験を追加します。Linux／macOSを追加するときは、共通契約試験にそのアダプターを参加させ、各OS固有の実行・署名・デスクトップ統合試験を追加します。

## 元の検討経緯

[分割前の計画・調査記録](../Archive/LauncherMaintenance.md)
