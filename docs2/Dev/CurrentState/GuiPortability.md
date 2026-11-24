# GUI移植の出発点

2026-08-01の記録。正式対応と移植用構成の区別を示します。後続手順は[移植計画](../Plans/GuiPorting.md)。

## 前提

- 現在の正式対応OSと配布物はWindowsである。
- 共通GUIは `KifuwarabeGo2026.GameOasis.Gui`、Windows実装は `KifuwarabeGo2026.GameOasis.Gui.Windows` に分離済みである。
- `KifuwarabeGo2026.GameOasis.Gui.PortabilitySmoke` は移植用の最小構成例として利用できる。

## 元の検討経緯

[分割前の計画・調査記録](../Archive/GuiPortingPlan.md)
