# Lobby・Play Room分離の到達点

2026-09-09追記：以下は過去の段階ごとの完了記録です。[外部アプリから利用する境界の現状](ExternalPlayRoomIntegration.md)と[次の計画](../Plans/ExternalPlayRoomIntegration.md)を別途確認してください。標準入出力ホストの存在と、外部アプリから画面付きプレイルームを操作できることは別の完了条件です。

2026-08-30の末尾の再開記録を基準に整理。段階2は第11縦切りまで、段階3・4・5・6・7は完了。冒頭に残っていた「第1／第2縦切り」の状態表示は古い記録です。

Lobbyのページ状態・Presenter・意味入力は専用Assemblyへ抽出済み。互換GUIにはMonoGameシェル、構成点、旧セッションとのAdapterが残ります。[次の作業](../Plans/LobbyRenderer.md)。

## Lobby描画の現在の境界

2026-09-09：`ILobbyPageLayout`を介して旧`TitleScreen`の座標・共通Controlを利用する形にし、ページ内容を`LobbyGui.MonoGame.LobbyPageRenderer`へ抽出しました。`TitleScreenRenderer`はShellとの合成を担当します。新DLLから互換GUI・Play Roomへの参照がないこと、および描画座標と入力判定の一致をGUI移植性試験で検査しています。Releaseビルドと同試験はPASS、手動の表示確認は未実施です。

互換タイトルShell固有操作とLobbyページ内容は別Rendererとなり、両者は設定説明の意味的描画Callbackだけで接続されます。内容Rendererは `ApplicationSettingsScreen` を参照しません。

### 段階4の完了判定（2026年8月30日）

状態：完了。

段階4の明記された完了条件は「囲碁Play Room GUIがLobby GUIの内部型を参照せず、保存済み起動要求から開始できる」です。`Reference.PlayRoomGui.Go`と`Reference.PlayRoomGui.Go.MonoGame`はいずれも`LobbyGui`および互換`GameOasis.Gui`を参照しません。保存済み起動要求から盤面条件、参加者、GTP Player接続、Review棋譜を復元し、Lobby非依存の囲碁構成点から新しいPlay Roomセッションを準備する経路も検査済みです。さらに通常盤面の表示状態、Geometry、Presenter、Coordinator、MonoGame Renderer、盤材、石資源まで物理抽出できました。したがって段階4の完了条件を満たしたと判定します。

### 段階5の完了判定（2026年8月30日）

状態：完了。

通常囲碁とポン抜きはゲーム別のEngine本体、JSON Lines Host、マニフェストを持ち、共通`PlayRoomEngine.JsonLines` SDKでProtocol Sを公開しています。両EngineはGUI、Lobby、MonoGame、他ゲーム具象実装を参照せず、公式Go、公式Ponnuki、外部Counterが同じ適合性ランナーを通過します。したがって段階5の完了条件を満たしたと判定します。

### 作業段階6の第8縦切り（2026年8月30日）

Releaseスクリプトへ囲碁Play Room専用Windows Hostの`win-x64`発行を追加し、互換GUIのpublishルート直下へexe、DLL、deps.json、runtimeconfig.jsonと、Common、Go、Go.MonoGameの依存DLLを収録するようにしました。Lobbyの公開配置用Resolverが最初に探索する場所と一致するため、開発用兄弟ディレクトリーへ依存せず、同じ内容がGUI版ZIPへ入ります。版番号一括更新の対象にも専用Hostプロジェクトを追加しました。

専用Hostへ画面を開かない明示的な配布契約スモークモードを追加しました。通常モードの起動契約は変えず、保存済み要求の読込と検証、準備完了JSONのflush後に、正常終了または診断付き異常終了を決定論的に発生させられます。これによりCIやRelease作業でMonoGame画面の手動操作を必要とせず、実際の公開exeと依存物を検査できます。

`Test-PublishedGoPlayRoom.ps1`はGUI publishルート直下の専用Hostを子プロセス起動し、準備完了通知、正常終了、準備後異常終了、非対応Room要求の拒否、異常後の再起動を順に検査します。要求JSONはWindows PowerShell 5.1でもBOMを付けずに保存します。Releaseスクリプトは必須ファイル検査後、ZIP作成前にこのスモークを自動実行します。

ソリューション全体のReleaseビルドは警告0・エラー0です。GUI移植性回帰試験とWindows非対話試験はともに`PASS`しました。実際のGUI Release publishへ専用Hostを重ねて公開配置スモークを実行し、準備、正常終了、異常終了、拒否、再起動の全項目が`PASS`しました。

第8縦切りにより、専用囲碁Play Room Hostの実装、Lobbyからの別プロセス起動、障害復帰、GUI配布物への収録までが一周し、作業段階6を完了しました。段階7も同日の後続記録で完了しています。

### 実施結果（2026年8月30日）

通常囲碁Play Room Engineに残っていた仮移管名`LegacyMatch`を再評価しました。この実装はProtocol Sの`GoPlaySpaceSession`と重複する旧Hostではなく、互換GUIが利用する時計、対局結果、観戦イベントを含む現役の囲碁対局モデルです。このため削除ではなく、所有者を表す`Reference.PlayRoomEngine.Go.Match`名前空間と`Match`フォルダーへ改名し、旧名前空間は残しません。Player Engine用初期局面アダプター、互換GUI、Windows試験、移植性試験の参照も新所有名へ揃えました。

`Reference.PlayRoomEngine.Go`のREADMEへ、依存先、`Match`の所有責務、旧名を二重保持しないことを明記しました。型名に残る`PlaySpace`はProtocol Sの公開通信契約であり、内部役割名の残骸ではありません。保存済み契約と外部Hostの互換性を優先し、Protocol Sの版更新までは維持すると記録しました。

互換`GameOasis.Gui`の責務表を現配置へ更新しました。Lobby GUI／Engineと囲碁Play Room GUI／Engineの正本は専用プロジェクトにあり、互換GUIには公開実行名を保つMonoGameシェル、構成点、旧`GoAppSession`から公開起動要求へのAdapter、専用Host未移行のBoard Editor／Review／CGOS／Ponnuki経路が残ることを明示しました。これは重複する正本ではなく、既存画面と保存形式を維持する互換境界です。また、独立`EditEntryProfile`へ置換済みで`#if false`に残っていた旧Entry編集描画と未使用enumを削除しました。

移植性試験は、囲碁Engine Assemblyが旧`.LegacyMatch`名前空間を公開しないことを追加検査します。ソリューション全体のReleaseビルドは警告0・エラー0です。GUI移植性、Windows非対話、通常囲碁Protocol S、Lobby Engine、Play Room JSON Lines、通常囲碁・ポン抜き・外部CounterのProtocol S適合性試験はすべて`PASS`しました。

以上により、段階7で扱う旧仮名称、無効な重複スタブ、横断互換シェルの所有理由を整理し、作業段階7を完了しました。段階2の最新到達点は本書冒頭を参照してください。

## 元の検討経緯

[分割前の計画・調査記録](../Archive/LobbyPlayRoomSeparation.md)
