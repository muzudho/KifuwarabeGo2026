# 【むずでょ個人用】GitHubへリリースする方法


## Visual Studio Code のインストール

git のインストールで選択したい場合、入れておく。  

https://code.visualstudio.com/Download  


## git のインストール

GitHubの認証で使うから、先にインストールしておく。  

https://git-scm.com/install/windows  


## winget のインストール



## GitHub CLI のインストール

winget を使って、ローカルPCへ、 GitHub CLI を  
インストールするのが初手だが、winget がなかったので、  
GitHub 公式に msi 形式のインストーラーがあってそっちを使った。  


## ブラウザー側の GitHub の認証

```shell
gh auth login --hostname github.com --git-protocol https --web
```

出てくる手順に従う。URL は自分でブラウザーに貼らず、Enter キーをクリック。  
Powershell は途中で自分では閉じないこと。  


## ターミナル側の GitHub の認証

```shell
gh auth login -h github.com
```

こうか？  

```shell
gh auth logout -h github.com -u muzudho
gh auth login -h github.com
```

ログインしているか確認：  

```shell
gh auth status
```

```log
github.com
  ✓ Logged in to github.com account muzudho (keyring)
  - Active account: true
  - Git operations protocol: https
  - Token: gho_************************************
  - Token scopes: 'gist', 'read:org', 'repo', 'workflow'
```
