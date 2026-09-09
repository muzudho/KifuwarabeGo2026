# Game Oasisの将来構想とプロセス名

将来の拡張と独立プロセス化の命名案です。表のexe名は採用済みの配布ファイル名ではありません。現在の正規Installer名は `KifuwarabeGo2026.Installer.exe` です。

## 将来の拡張

この分離により、GUIへゲーム運営やルール処理を集中させず、ゲームコンシェルジュとプレイスペースエンジンを独立して差し替えられます。  
また、人間またはコンピューターエンジンのプレイヤーとゲームマスターを、それぞれ独立した参加者として接続できます。  
さらに、プレイスペースエンジン内部のゲームルール、ルールセット、ゲーム形式を組み替えられます。

通常の囲碁だけでなく、変則囲碁、囲碁の四択クイズ、連珠、リバーシ、チェスなどへの拡張を想定できます。

### 製品名の二つの意味

`Kifuwarabe Go` は、次の二つの側面を持つ製品名とします。

- 囲碁（Go）を遊ぶための囲碁プログラム
- 複数のゲーム、参加者、エンジンが集まるゲームプラットフォーム `Kifuwarabe Game Oasis`

`Go` は囲碁を表すと同時に、`Game Oasis` を縮めた愛称でもあります。

ゲームプラットフォームとしての説明には「ゲームオアシス」または `Kifuwarabe Game Oasis` を使用します。

### 将来の実行プロセス名

将来、Installer、Lobby、Concierge、Play Room、PlaySpaceを別プロセスへ分離する場合、Windowsのタスクマネージャーには複数の実行ファイルが並びます。

`KifuwarabeGo` 自体が `Kifuwarabe Game Oasis` の略称を兼ねるため、`KifuwarabeGo2026GameOasis.exe` のように `GameOasis` を重ねた名前は使用しません。実行ファイル名は、製品名の接頭辞と内部的な責務名を組み合わせます。

| 実行ファイル名 | 内部的な役割 | 利用者向けファイル説明の例 |
|---|---|---|
| `KifuwarabeGo2026Installer.exe` | インストール、更新、各プロセスの起動 | Kifuwarabe Go 2026 Installer |
| `KifuwarabeGo2026Lobby.exe` | ゲーム選択、対局準備、Play Roomへの入口 | Kifuwarabe Go 2026 Lobby |
| `KifuwarabeGo2026Concierge.exe` | セッション、参加者、各プロトコル、プロセスの仲介 | Kifuwarabe Go 2026 Game Coordinator |
| `KifuwarabeGo2026PlayRoom.exe` | 対局、盤面編集、レビューなど盤を使うGUI | Kifuwarabe Go 2026 Play Room |
| `KifuwarabeGo2026PlaySpace.Go.exe` | 通常囲碁のルール、状態、行動適用、終局判定 | Kifuwarabe Go 2026 Go PlaySpace |

実行ファイル名はログ、設定、プロセス起動、障害調査で役割を識別するための安定した技術名です。Windowsのタスクマネージャーやファイルのプロパティでは、バージョンリソースのファイル説明を利用者向け名称として表示できます。このため、内部名`Concierge`を実行ファイル名に残しながら、利用者には`Game Coordinator`と説明できます。

`KifuwarabeGo2026PlayRoomConcierge.exe`は使用しません。ConciergeはPlay Roomの内部部品ではなく、Lobby、Play Room、Player、Game Master、PlaySpaceのすべてを仲介する中心プロセスだからです。

```text
KifuwarabeGo2026Lobby.exe ── Protocol G ──┐
KifuwarabeGo2026PlayRoom.exe ─ Protocol G ┤
Player ──────────────────────── Protocol P ┼─ KifuwarabeGo2026Concierge.exe
Game Master ─────────────────── Protocol M ┘                 │
                                                             │ Protocol S
                                                             ▼
                                      KifuwarabeGo2026PlaySpace.Go.exe
```

これらは将来のプロセス分離における命名方針です。現行の単一GUI実行ファイルを直ちに改名または分割することは定めません。分離時には、既存の起動名、Installer、設定、ログ、リリース資産との互換移行を別途計画します。

## 元の検討経緯

[分割前の計画・調査記録](../Archive/GameOasisTerminologyProposals.md)
