# StationeryUIの後続確認

[独立・公開は実施済み](../CurrentState/StationeryUi.md)です。着手前の未チェック欄を再実装のTODOには使いません。

1. StationeryUI側の[実装・引き継ぎ](https://github.com/muzudho/StationeryUI/blob/main/docs/implementation-plan.md)で最新の担当範囲を確認する。
2. 実IME入力、候補位置、DPI、フォーカス移動を実機で確認し、OS・IME・表示倍率を記録する。
3. NuGet.org公開の要否・名前・対象版を確定する。GitHub Releaseでの公開とは別工程として扱う。
4. タッチ、WindowsDX、Linux/macOS IME、OSアクセシビリティ等は、採用範囲と検証環境を決めてから進める。

このリポジトリーには利用側の影響だけを記録し、ライブラリー本体の進捗は正本へ集約します。

[当初の機能・設計条件と検証項目](../Archive/StationeryUiExtraction.md)
