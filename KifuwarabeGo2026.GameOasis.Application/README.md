# KifuwarabeGo2026.GameOasis.Application

Game Oasisで常用するカタログの追加、削除、並べ替え、整合性検査など、利用事例の意味を所有します。

物理ファイル、JSON、保存ディレクトリーなどの永続化手段は所有しません。永続化は抽象境界を介して`KifuwarabeGo2026.GameOasis.Storage`へ委譲します。

現在はGTPエンジン、エントリー、クライアント識別情報のプロファイル規則と、各カタログのロード・保存ユースケースを所有します。
