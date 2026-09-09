# GTP独自拡張の命名方針

採用した名前空間の説明です。コマンド例には旧互換仕様を含み、最新の全コマンド一覧ではありません。[現行アプリ・オプション仕様](AppIdsAndEngineOptions.md)。

## KifuwarabeGo2026での決定

`goapps-`は用途を説明する名前ですが、一般名詞に近いため、別のプロジェクトが異なる意味で使用する可能性があります。この名称はコミット前の仮称として廃止しました。

`kifuwarabe-`は、この拡張仕様の発祥と管理主体が明確で、他の独自拡張との衝突も避けやすい名前です。一方、すべてのコマンドへ付ける接頭辞としては長いため、Kifuwarabeの先頭三音「Ki Fu Wa」を表す`kfw-`へ短縮します。

正規接頭辞は`kfw-`とします。第三者がこの接頭辞のコマンドを実装しても問題ありません。GoGuiの仕様を他のエンジンが`gogui-`という名前のまま実装することと同じです。

```text
kfw-options
kfw-get-option
kfw-set-option
kfw-list-apps
kfw-list-app-versions
kfw-begin-app
```

コマンド全体は、KataGoの現行拡張に近い一貫したkebab-caseとします。

旧`gui_`コマンドは互換エイリアスとして受け付けます。コミット前の仮称だった`goapps-`は互換対象に含めません。

## 参照資料

- GTP Version 2仕様: https://www.lysator.liu.se/~gunnar/gtp/gtp2-spec-draft2/gtp2-spec.html
- GNU Go GTP文書: https://www.gnu.org/software/gnugo/gnugo_19.html
- KataGo公式リポジトリ: https://github.com/lightvector/KataGo
- GoGui Analyze Commands: https://www.kayufu.com/gogui/analyze.html

## 元の検討経緯

[分割前の計画・調査記録](../Archive/GtpCommandNamingResearch.md)
