# 【むずでょ個人用】Codexの環境構築Bパターン


## クラウドコード

クラウドのホームページへ行き、ダウンロード。  

Visual Studio 2026 のメインメニューの［拡張機能］から、 `Claude Code Extension for Visual Studio` という拡張を選択。  
Visual Studio 2026 を閉じる。インストールが開始される。  
Visual Studio 2026 を開く。  
［表示　＞　その他のウィンドウ　＞　Claude Code Extension］をクリック。  

最初は Claude Code Extension ではなく、 CMD という名前のウィンドウが開く。  


## ローカルにコーデックスをインストール

npm を使うので Node.JS をインストール。  
https://nodejs.org/ja
Windows にインストールするなら、 msi を選ぶと楽。  
Node.JS の中に npm が含まれている。  

Visual Studio のターミナルではなく、Windows 検索で cmd と入力して、コマンドプロンプトを開いて以下を入力。  

```shell
# CMD を Claude Code にするため。
npm install -g @anthropic-ai/claude-code

# Claude Code で Codex を使えるようにするため。
npm install -g @openai/codex
```

Visual Sudio を開いているのなら、それを再起動。  

画面にドッキングさせる。  
歯車アイコンボタンをクリック。Configure Visible Code Agents をクリック。  
もう１回歯車アイコンボタンをクリック。Codex をチェック。
AIの顔アイコンをクリック、例えば GPT-5.6 Terra を選択。  


## PC に Codex をインストールする。

// ChatGPT のホームページから、Windows 用の Codex をダウンロードする。  

「Visual Studio 2026 Insider にクロードコードのチャットをインストールしたんで、Codex もそこで使いたいぜ（＾～＾）」
