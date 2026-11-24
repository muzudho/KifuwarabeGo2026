# Lobby描画境界の後続分離

2026-09-09追記：利用者の目的に対する主計画は[外部アプリからのプレイルーム利用](ExternalPlayRoomIntegration.md)です。本作業は補助となるDLL分離です。第12縦切りの描画Portと`LobbyGui.MonoGame`への内容Renderer抽出を実装し、ReleaseビルドとGUI移植性試験は成功しました。手動の表示・操作確認は残っています。

段階2の第12縦切りを進めた記録です。完了済みの段階3〜7を最初から繰り返しません。[現在の到達点](../CurrentState/LobbyPlayRoomSeparation.md)。

## 次の最小作業

描画抽出後の見た目・hover・BACK操作を手動確認します。通信分離の主作業は[画面付きプレイルームへの接続](ExternalPlayRoomIntegration.md)へ進みます。

一括改名・一括移動、既存実行ファイル名と保存形式の先行変更はしません。

## 検証

各段階で最低限、次を確認します。

* ソリューション全体Releaseビルド。
* LobbyEngine、GameOasis.Contracts、Conciergeの単体試験。
* PlayRoom JSON Linesの正常／異常終了試験。
* Protocol S適合性ランナーの通常囲碁、ポン抜き、外部Counter。
* GUI移植性試験とWindows非対話試験。
* ランチャーからLobbyを起動し、Lobbyから各Play Roomを開始・終了する手動試験。
* 発行物に必要なHost、マニフェスト、契約DLLが含まれること。

WindowsのSmart App Controlが未署名の再生成DLLを`0x800711C7`で拒否した場合は、[`Smart App Controlによる再生成DLLブロック調査.md`](../Troubleshooting/Smart%20App%20Controlによる再生成DLLブロック調査.md)に従い、コード失敗とOS拒否を分けて記録します。PC再起動を通常の試験手順にはしません。

## 実施原則

* 一度に1つの役割境界だけを物理移動する。
* 責務を分けてから改名する。
* `Game1`または`GoAppSession`を一括移動しない。
* 同一プロセスと別プロセスの両アダプターを同時に全面変更しない。
* 保存形式、実行ファイル互換、ランチャー更新経路を優先する。
* 利用者の指示なしにSmart App Controlを無効化しない。

## 元の検討経緯

[分割前の計画・調査記録](../Archive/LobbyPlayRoomSeparation.md)
