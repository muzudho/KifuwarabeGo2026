# StickyNote表示位置ストラテジー導入計画

## 目的

StickyNoteの表示可否と表示位置を、画面ごとに散在する条件分岐で決めないようにする。
画面文脈を表すパンくずリストに対応したストラテジーを一箇所で解決し、サブ画面追加時の表示漏れ・非表示漏れを防ぐ。

## 設計方針

- 画面定義（または画面を一意に識別できるパンくずキー）ごとに、`StickyNote` の表示方針を対応付ける。
- 表示方針はストラテジーとして実装し、各StickyNoteの表示可否、位置・サイズ、対象への接続線の端点を決定する。
- `StickyNote` の描画側は、現在の画面文脈から解決したストラテジーの結果だけを描画する。個別画面やダイアログの状態を見て独自に非表示判定しない。
- 対応するストラテジーが未登録の場合は、必ず非表示にする（安全側の既定値）。
- パンくずの表示文字列そのものを分岐キーにせず、安定した画面IDまたはパンくず項目IDを使う。画面定義にはパンくずとストラテジーの両方を関連付ける。

## 構成案

```text
ScreenDefinition
  - BreadcrumbId
  - StickyNotePlacementStrategy

StickyNotePlacementStrategy
  - TryGetPlacement(context, out placement)

placement
  - 表示するか
  - bounds
  - connectorStart / connectorEnd
```

ストラテジーは、必要な対象矩形・画面サイズ・表示中のUI状態だけを`context`から受け取る。描画処理や画面遷移処理は持たせない。

## 導入手順

1. 現在のStickyNote表示箇所と、サブ画面で非表示にしている条件分岐を洗い出す。
2. 画面ID／パンくず項目IDと、各画面で有効にするストラテジーの対応表を定義する。
3. 既存の代表画面用に配置ストラテジーを実装し、未登録時は`HiddenStickyNotePlacementStrategy`を返す。
4. StickyNote描画の入口を一箇所に集約し、個別のサブ画面判定を撤去する。
5. 全パンくず経路を確認し、各経路で「意図した付箋が表示される」または「明示的に非表示」であることを確認する。

## 完了条件

- 新しい画面を追加しただけではStickyNoteが誤って表示されない。
- StickyNoteを表示する画面では、表示位置の決定元を対応するストラテジーへ一本化できている。
- サブ画面を含む全画面で、表示・非表示の意図を対応表から追跡できる。

## 実装状況（2026-08-12）

- `StickyNoteScreenId` と `StickyNoteKind` を導入し、画面文脈と案内を安定したIDで表せるようにした。
- `StickyNotePlacementStrategies` が `(画面ID, 案内ID)` ごとの配置ストラテジーを解決する。未登録の組み合わせは非表示となる。
- タイトル、Entry Profile編集、Client Identity編集、一時HANDLE選択、GTPエンジン選択、対局ルール選択の既存StickyNoteをこの入口へ移行した。
- Entry Profile編集の子画面を開いている間に付箋を隠す個別条件分岐を撤去した。子画面の画面IDには親画面用ストラテジーが登録されていないため、自動的に非表示となる。
