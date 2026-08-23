# KifuwarabeGo2026.GameOasis.Storage

Game Oasisの常用データを物理的に保存・読込する実装を所有します。

登録可否、重複、参照整合性、表示順などの業務判断は所有しません。それらは`KifuwarabeGo2026.GameOasis.Application`の責務です。

既定構成では、GTPエンジン、エントリー、クライアント識別情報の互換保存場所とUTF-8文書ストアを提供します。
