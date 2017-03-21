# Information

## 何
Unity GUIをmarkdownから構築するツール。

## 基本仕様
CommonMark strict に準ずる。(githubと同様)  
[https://markdown-it.github.io](https://markdown-it.github.io) で、CommonMark strict のチェックを入れるとそんな感んじ。


## 画像
サイズ指定は<IMG サイズ指定>でしかできない(これはmarkdownがそうなってる。)  
サイズ指定しない記法で  
!+[title]+(./somewhere/a.jpg)  
とかやった場合、横幅をコンテンツサイズに合わせた状態で表示される。

	
## テーブル
htmlの記法で書けば動く。
markdown記法では動かない。(CommonMarkでもmarkdown記法は動かないけどまあそのせいだけじゃなくてサポートが追いついてない。	
