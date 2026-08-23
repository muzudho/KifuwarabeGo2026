# GTP独自拡張コマンド命名調査

## 目的

KifuwarabeGo2026が追加するGTP独自拡張について、標準GTPや他の実装と衝突しにくく、第三者も実装しやすいコマンド名を決めるための調査結果を残します。

## GTP Version 2仕様

GTPの標準コマンドには、次のようなsnake_caseが多く使われています。

```text
protocol_version
known_command
list_commands
clear_board
final_status_list
```

ただし、仕様上の`command_name`は、空白を含まない印字可能な文字列です。snake_caseそのものが構文上の必須条件ではありません。

独自拡張については、標準コマンドとの将来的な衝突を避けるため、次の形式が推奨されています。

```text
XXX-YYYYY
```

- `XXX`: エンジン、GUI、または拡張仕様を識別できる固有の接頭辞
- `YYYYY`: コマンドの内容

仕様に示されている例は`gg-genmove`です。

## 実装例

### KataGo

KataGoは、実装名を接頭辞にしたkebab-caseを使用しています。

```text
kata-analyze
kata-set-rules
kata-get-rules
kata-set-param
kata-get-param
```

### GoGui

GoGuiは、GUI名の接頭辞とsnake_caseを組み合わせた混合形式を使用しています。

```text
gogui-analyze_commands
```

これは、`gogui-`が拡張の所有者を表し、`analyze_commands`がGTP風のコマンド名を表す構造です。

### GNU Go

GNU Goには`gg-undo`のように固有接頭辞を持つコマンドがあります。一方、歴史的には`countlib`など、標準コマンドと同じ見た目の独自拡張も多数あります。新しい独自拡張では、衝突を避けるため固有接頭辞を付ける方が安全です。

### Leela Zero

Leela Zeroでは、実装名を短縮した接頭辞を持つコマンドが使われています。

```text
lz-analyze
```

## 調査結果

- 標準GTPではsnake_caseが慣例です。
- 独自拡張では、実装、GUI、プロジェクト、または拡張仕様を識別する固有接頭辞をハイフンで付ける例が一般的です。
- 接頭辞はGUI名に限定されません。`kata-`、`gg-`、`lz-`のようにエンジンやプロジェクト名も使われます。
- 接頭辞より後をkebab-caseにする例と、snake_caseにする例の両方があります。
- `gogui-analyze_commands`のようなkebab-snake_case混合形式も実在します。

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
