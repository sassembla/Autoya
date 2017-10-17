# AssetBundle API

---

## 概要

AssetBundle関連のAPI  
AssetBundleに関しての機能を提供する。  

* [List機構](#/2)
* [Preload機構](#/3)
* [LoadAsset機構](#/4)
* [独立したState](#/25)  
* [responseHeaderによるList/ABの自動更新](#/26)

+++

**MethodsとOverridePoints**

MethodsはAutoya.経由で使えるメソッド、  
OverridePointsはデフォルト挙動を変更する  
ための変更点を記述する箇所になっている。

---

## List feature

**Methods**

* [AssetBundle_DownloadAssetBundleListIfNeed](#/5)
* [AssetBundle_IsAssetBundleFeatureReady](#/6)
* [AssetBundle_AssetBundleList](#/7)
* [AssetBundle_DiscardAssetBundleList](#/8)

+++

**OverridePoints**

* [OverridePoints/AssetBundleListDownloadUrl](#/9)
* [OverridePoints/OnAssetBundleListGetRequest](#/11)
* [OverridePoints/LoadAssetBundleListFromStorage](#/11)
* [OverridePoints/StoreAssetBundleListToStorage](#/12)
* [OverridePoints/DeleteAssetBundleListFromStorage](#/13)
* [OverridePoints/OnRequestNewAssetBundleList](#/14)
* [OverridePoints/ShouldUpdateToNewAssetBundleList](#/15)
 
+++

Appで現在DL可能な全てのAssetBundle(AB)の  
情報が入ったList(ABList)を制御するAPI。

Listを取得することで、AB関連の全APIが  
使用可能になる。

---

## Preload feature
**Methods**

* [AssetBundle_Preload](#/16)
* [AssetBundle_PreloadByList](#/17)

**OverridePoints**

* [OnAssetBundlePreloadListGetRequest](#/18)

+++

1~N個のABを使用前に一括DLするAPI。  

もしABが更新されていれば、最新版をDLする。  

+++

PreloadList(PL)インスタンスを生成して使うほか、  

PLを返す外部サービスを用意し、  
urlからPLを取得->Preloadさせることができる。

---

## Load Asset feature

**Methods**

* [AssetBundle_LoadAsset[T]](#/17)
* [AssetBundle_UnloadOnMemoryAssetBundles](#/20)
* [AssetBundle_UnloadOnMemoryAssetBundle](#/21)
* [AssetBundle_UnloadOnMemoryAsset](#/22)
* [AssetBundle_DeleteAllStorageCache](#/23)

**OverridePoints**

* [OnAssetBundleGetRequest](#/24)

+++

ABからAssetを取り出す。  

もしABがDLされていなかった場合、  
DL -> 展開までを自動で行う。

もしABが更新されていれば、最新版をDLする。  

+++

## ABのlifetime
この機構で扱われるABのライフタイムは以下。



+++

そのほか、端末にDL済みのABの削除、  
メモリに展開されたABのunloadなどを行う。

---

**AssetBundle_DownloadAssetBundleListIfNeed**

必要であればABListを特定のURLから取得、  
端末内に保持する。

+++

(このへんにmdDoc)

+++

![img](https://github.com/sassembla/Autoya/raw/doc/doc/images/api/assetbundle_list.png)

---

**AssetBundle_IsAssetBundleFeatureReady**

ABを使用できる状態であればtrueを返す。  
そうでなければfalseを返す。

+++

(このへんにmdDoc)

---

**AssetBundle_AssetBundleList**

現在保持しているABListを返す。  
もし保持していなければ空のABListを返す。  

(空のaBListは、.Exists()がfalseを返す。)


+++

(このへんにmdDoc)

---

**AssetBundle_DiscardAssetBundleList**

現在保持されているABListを削除する。  
保持されているABデータは一切変更されない。


+++

(このへんにmdDoc)

---

**OverridePoints/AssetBundleListDownloadUrl**

[AssetBundle_DownloadAssetBundleListIfNeed](#/5)  
に対してリスト取得用のURLを提供する。

+++

(このへんにmdDoc)

---

**OverridePoints/OnAssetBundleListGetRequest**

[AssetBundle_DownloadAssetBundleListIfNeed](#/5)  
で外部からリストを取得する際のパラメータを指定する。

+++

(このへんにmdDoc)

---

**OverridePoints/LoadAssetBundleListFromStorage**

[AssetBundle_DownloadAssetBundleListIfNeed](#/5)  
等に対して、保存してあるABListを提供する。

+++

(このへんにmdDoc)

---

**OverridePoints/StoreAssetBundleListToStorage**

ABListの更新時に呼ばれる。  
ABListを上書き保存する。

+++

(このへんにmdDoc)

---

**OverridePoints/DeleteAssetBundleListFromStorage**

ABListの削除時に呼ばれる。  
保存されているABListを削除する。


+++

(このへんにmdDoc)

---

**OverridePoints/OnRequestNewAssetBundleList**

+++

(このへんにmdDoc)

---

**OverridePoints/ShouldUpdateToNewAssetBundleList**

+++

(このへんにmdDoc)

---

**AssetBundle_Preload**

urlからPreloadListを取得し、  
記載されているABを端末へとキャッシュする。

+++

(このへんにmdDoc)

---

**AssetBundle_PreloadByList**

listパラメータに記載されているABを  
端末へとキャッシュする。

+++

(このへんにmdDoc)

---

**OverridePoints/OnAssetBundlePreloadListGetRequest**

listパラメータに記載されているABを  
CDNなどへリクエストする際のパラメータを指定する。

+++

(このへんにmdDoc)

---

**AssetBundle_LoadAsset[T]**

+++

(このへんにmdDoc)


---

**AssetBundle_UnloadOnMemoryAssetBundles**

+++

(このへんにmdDoc)

---

**AssetBundle_UnloadOnMemoryAssetBundle**

+++

(このへんにmdDoc)

---

**AssetBundle_UnloadOnMemoryAsset**

+++

(このへんにmdDoc)

---

**AssetBundle_DeleteAllStorageCache**

+++

(このへんにmdDoc)

---

**OverridePoints/OnAssetBundleGetRequest**

CDNなどからABを取得する際のパラメータを指定する。

+++

(このへんにmdDoc)

---

## 独立したstate

AB関連の機構には独自の状態設定がある。  

* ABList未取得状態
* ABList取得済み状態

端末内にAssetBundleListを保持すると、  
自動的に取得済み状態に変化する。  

取得済み状態では、ABからAssetを取り出したり、  
ABを事前にPreloadすることができる。

---


## responseHeaderによるList/ABの自動更新

任意の通信のresponseHeaderに次のkvを与えると、  
ABの機構に使用するListの更新を促せる。

	resversion="X.Y.Z"
	

+++

backgroundで特定ハンドラが着火し、  
リソース更新をどう行うか、App側で制御可能。

リストの更新後、更新されたABは、  
次回Load/Preload時に自動的に最新のものになる。

+++

![img](https://github.com/sassembla/Autoya/raw/doc/doc/images/api/assetbundle_update.png)

+++

[OverridePoints/OnRequestNewAssetBundleList](#/)

[OverridePoints/ShouldUpdateToNewAssetBundleList](#/)