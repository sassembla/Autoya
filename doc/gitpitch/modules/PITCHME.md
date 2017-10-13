# Autoya modules

---

## 概要

ここでは、Autoyaの機能が実装されている  
モジュール(Module, Mod)という要素について  
解説する。

---

## モジュールって何

Autoyaが内部で使用している、  
機能単位に分割された実装のこと。

それぞれ単体でも使用することができる。

---

## module一覧

* [AssetBundle](https://gitpitch.com/sassembla/autoya/doc?p=doc/gitpitch/modules/AssetBundle)
* [Connections](https://gitpitch.com/sassembla/autoya/doc?p=doc/gitpitch/modules/Connections)
* [Encrypt](https://gitpitch.com/sassembla/autoya/doc?p=doc/gitpitch/modules/Encrypt)
* [Information](https://gitpitch.com/sassembla/autoya/doc?p=doc/gitpitch/modules/Information)
* [Manifest](https://gitpitch.com/sassembla/autoya/doc?p=doc/gitpitch/modules/Manifest)
* [Persistence](https://gitpitch.com/sassembla/autoya/doc?p=doc/gitpitch/modules/Persistence)
* [Purchase](https://gitpitch.com/sassembla/autoya/doc?p=doc/gitpitch/modules/Purchase)
* [Representation](https://gitpitch.com/sassembla/autoya/doc?p=doc/gitpitch/modules/Representation)
* [Backyard](https://gitpitch.com/sassembla/autoya/doc?p=doc/gitpitch/modules/Backyard)

---

それぞれのモジュールの機能は  
名前やリンク先から察してもらうとして、  

Autoyaでは各Modを**Backyard**から使う形で  
フレームワークとしての機能を提供している。


+++

各モジュールは単体でも動作可能。  
Autoyaを介さずに各機能を単体で  
使用することも可能になっている。  

(そういう設計にした方がテストとか楽だった。)  

---

## Overview

![図](https://raw.githubusercontent.com/sassembla/Autoya/master/doc/images/Overview.png)

+++

実装されている機能へはAutoya.からアクセス可。  
IDE上でAutoya.以降に補完候補が出てこない場合、  
その機能はAutoyaにはおそらく存在しない。


---

## Autoya -> Modの実例

Autoyaを経由すると、複数のModを組み合わせた  
挙動が簡単に利用できる。

例として認証ありHttp通信の構成を挙げる。  
この機能はAuth機能とConnections Modの連携で  
実装されている。


+++


![図](https://raw.githubusercontent.com/sassembla/Autoya/master/doc/images/Authentication Bypass.png)

+++

Autoyaを介した通信は、認証系の機構を経由して  
authenticatedな変換が行われ、  
サーバへとHttpリクエストを行う。


+++

レスポンスに関しても、認証系の機構を経由して  
validationなどが行われ、tokenなどの処理と  
平行してApp側へのレスポンスを提供している。

---

## 各ModのSettingについて

Autoyaの設定ファイルは、  
Autoya/Settingsフォルダに纏められている。

主に各ModをAutoya経由で使用するための設定が  
初期値としてセットされている。


