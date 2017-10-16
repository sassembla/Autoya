# AssetBundle API

---

## 概要

AssetBundle関連のAPI  
AssetBundleに関しての機能を提供する。  

+++

5つの機構がある。

* [List機構](#/2)
* [Preload機構](#/3)
* [LoadAsset機構](#/4)
* [独立したState](#/22)  
* [responseHeaderによるList/ABの自動更新](#/23)

---

## List feature

**Methods**

* [AssetBundle_DownloadAssetBundleListIfNeed](#/5)
* [AssetBundle_IsAssetBundleFeatureReady](#/6)
* [AssetBundle_AssetBundleList](#/7)
* [AssetBundle_DeleteAssetBundleListFromStorage](#/8)

**OverridePoints**

* [AssetBundleListDownloadUrl](#/9)
* [OnAssetBundleListGetRequest](#/11)
* [LoadAssetBundleListFromStorage](#/11)
* [StoreAssetBundleListToStorage](#/12)
 
+++

Appで現在DL可能な全てのAssetBundle(AB)の  
情報が入ったListを制御するAPI。

Listを取得することで、AB関連の全APIが  
使用可能になる。

---

## Preload feature
**Methods**

* [AssetBundle_Preload](#/13)
* [AssetBundle_PreloadByList](#/14)

**OverridePoints**

* [OnAssetBundlePreloadListGetRequest](#/15)

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

* [AssetBundle_LoadAsset[T]](#/16)
* [AssetBundle_UnloadOnMemoryAssetBundles](#/17)
* [AssetBundle_UnloadOnMemoryAssetBundle](#/18)
* [AssetBundle_UnloadOnMemoryAsset](#/19)
* [AssetBundle_DeleteAllStorageCache](#/20)

**OverridePoints**

* [OnAssetBundleGetRequest](#/21)

+++

ABからAssetを取り出す。  

もしABがDLされていなかった場合、  
DL -> 展開までを自動で行う。

もしABが更新されていれば、最新版をDLする。  

+++

そのほか、端末にDL済みのABの削除、  
メモリに展開されたABのunloadなどを行う。

---

AssetBundle_DownloadAssetBundleListIfNeed

---

AssetBundle_IsAssetBundleFeatureReady

---

AssetBundle_AssetBundleList

---

AssetBundle_DeleteAssetBundleListFromStorage

---

AssetBundleListDownloadUrl

---

OnAssetBundleListGetRequest

---

LoadAssetBundleListFromStorage

---

StoreAssetBundleListToStorage

---

AssetBundle_Preload

---

AssetBundle_PreloadByList

---

OnAssetBundlePreloadListGetRequest

---

AssetBundle_LoadAsset[T]

---

AssetBundle_UnloadOnMemoryAssetBundles

---

AssetBundle_UnloadOnMemoryAssetBundle

---

AssetBundle_UnloadOnMemoryAsset

---

AssetBundle_DeleteAllStorageCache

---

OnAssetBundleGetRequest


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

(遷移を書く)