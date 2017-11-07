# AssetBundle Module

---

## 概要

AssetBundleを扱うモジュール。  
AssetBundleに関しての機能を提供する。  

---

## できること

* AssetBundle List management
* load Asset from AB asynchronously
* on demand download
* multiple AssetBundle download
* preload AssetBundle


---


## AssetBundle List management

Appで使用する全AssetBundleをリストで  
管理することができる。

+++

運用中にゲームで使用するAssetが  
追加/更新されるのを前提に、それらをリスト化し  
サーバからコントロールすることができる。

+++

[AssetBundleList type](https://github.com/sassembla/Autoya/blob/master/Assets/Autoya/AssetBundle/AssetBundleList.cs#L1)

```
list
	└ target // human readable target platform name.
	└ version // human readable version desc.
	└ assetBundles // assetBundleInfo[]
		└ assetBundleInfo
			└ bundleName // bundle name.
			└ assetNames // contained asset names. e,g, "Assets/Somewhere/texture.png"
			└ dependsBundleNames // the bundle names which this assetBundle depends on.
			└ crc // crc parameter. used for crc check.
			└ hash // hash parameter. used for exchange same asset from old one to new one.
			└ size // size of uncompressed AssetBundle.
```


+++

リストの生成はUnity標準のmanifestから行う。

生成機構はEditor > Window > Autoya から  
使用可能。(そのうち詳しく書く)

[AssetGraph](https://github.com/unity3d-jp/AssetGraph)からも生成できる(plan)。

+++

EditorからABとABListを生成して、  
Assets -> AssetBundles + AssetBundleList

そのリストをユーザーに配ることで、  
ユーザーの手元のリソースの更新を行う。

---

## load Asset from AB asynchronously

基本非同期でAssetをABから展開する。

```
yield return loader.LoadAsset(
	"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png", 
	(string assetName, Texture2D texAsset) => {
		tex = texAsset;
	},
	(assetName, failEnum, reason, status) => {
		Debug.Log("fail to open, failEnum:" + failEnum + " reason:" + reason);
	}
)
```

+++

取り出すAssetの型指定は、こうも書ける。

```
yield return loader.LoadAsset<Texture2D>(
	"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png", 
	(assetName, texAsset) => {
		tex = texAsset;
	},
	(assetName, failEnum, reason, status) => {
		Debug.Log("fail to open, failEnum:" + failEnum + " reason:" + reason);
	}
)
```

+++

### Unloadについて
OnMemoryにロード済みのABの解放ができる。

```
loader.UnloadOnMemoryAssetBundle(bundleName)
```

メモリ上にあるABを破棄し、  
使用されているこのAB由来のAssetも破棄される。

+++

### Asset名からそのAssetを含むAB名を知りたい時

[AssetBundleLoader#GetContainedAssetBundleName](https://github.com/sassembla/Autoya/blob/master/Assets/Autoya/AssetBundle/AssetBundleLoader.cs#L562)を使用するといい。



---

## on demand download

特定のABに含まれるAssetをリクエストした際、  
そのABがダウンロード前であれば、  
ABをDLした後でAssetを展開する。

+++

あるAssetを使う際、Assetを含むABが  
DL済みでなければ、DL後に展開される。

Asset : AB -> ABをDL -> Asset展開

一度DLされたABは端末側にキャッシュされる。

+++

キャッシュされたABに含まれるAssetは、  
DL処理なしで展開される。

Asset : AB[Cached] -> Asset展開

+++

### AssetBundleの状態

各ABは、Loader内部で3段階の状態を持っている。

```
NotCached -> OnStorage <-> OnMemory(AssetBundleLoaded)
```

+++

```
NotCached -> OnStorage <-> OnMemory(AssetBundleLoaded)
```
* NotCached: 端末にABがない
* OnStorate: ABキャッシュがあるが未展開
* OnMemory: ABキャッシュがメモリに展開済み
+++

Assetを取得する際、含有するABの状態が変化し、  
OnMemoryになったABからAssetが生成される。

Loader内部のABの状態を気にする必要はないが、  
知っておくとメモリ状態を把握することが可能。

+++

現在ロードされているABやAssetの情報は、  
[AssetBundleLoader#OnMemoryBundleNames](https://github.com/sassembla/Autoya/blob/master/Assets/Autoya/AssetBundle/AssetBundleLoader.cs#L552)  
[AssetBundleLoader#OnMemoryAssetNames](https://github.com/sassembla/Autoya/blob/master/Assets/Autoya/AssetBundle/AssetBundleLoader.cs#L557)  
などのAPIを使って取得できる。

---

## multiple AssetBundle download

複数のAB/一つのABに含まれるAssetを同時に  
何個でもDLしたり展開することができる。  

メモリ上の無駄は発生しない。

+++

例えばAB1に含まれるAsset a, bについて、  
同時に展開するような処理を行っても、  
DLされるAB1は1つ、展開されるa,bは1つずつ。

もしAB1がキャッシュされていた場合は  
a,bを同時に展開する。

---

## preload AssetBundle

複数のABを一括でDL、キャッシュする機構。  
この機構でDLしたABはメモリに展開されない。

+++

主にon demandでAssetをDLさせたくない、   
Asset使用前に部分的にDLを済ませたい、  
という場合に便利。

キャッシュ済み、未更新なABは無視される。
 
取得済み、更新有りのABを一括で最新版へと  
更新させるためにも使用できる。

+++

[PreloadList](https://github.com/sassembla/Autoya/blob/master/Assets/Autoya/AssetBundle/AssetBundlePreloader.cs#L21)型のデータから  
複数のABを纏めてDLする機能を提供する。

```
preloadList
	└ name // human readable name of list.
	└ bundleNames // string[]
   		└ bundleName // preload target bundle name.
```

+++

PreloadListを自前で生成、  
AB名を入れて纏めてDLできる。

```
var preloadBundleNames = new string[]{.....};
var preloadList = new PreloadList("PreloadWithPreloadList", preloadBundleNames);
		
yield return assetBundlePreloader.Preload(
	loader,
	preloadList,
	onBeforePreload,
	progress => {
		// show progress here.
	},
	() => {
		// do nothng.
	},
	(code, reason, autoyaStatus) => {
		Debug.LogError("failed to download, code:" + code + " reason:" + reason);
	},
	(preloadFailedAssetBundleName, code, reason, autoyaStatus) => {
		Debug.LogError("failed to download, name:" + preloadFailedAssetBundleName + " code:" + code );
	}
);
```

+++

HTTPを介してサーバ側からPreload Listを取得、  
適宜必要なABをpreloadさせる運用も可能。

```
yield return assetBundlePreloader.Preload(
	loader,
	"https://somewhere/1.0.0/sample.preloadList.json", 
	onBeforePreload,
	progress => {
		Debug.Log("progress:" + progress);
	},
	() => {
		// all preloadListed assetBundles are cached!
	},
	(code, reason, autoyaStatus) => {
		Debug.LogError("failed to download, code:" + code + " reason:" + reason);
	},
	(preloadFailedAssetBundleName, code, reason, autoyaStatus) => {
		Debug.LogError("failed to download, name:" + preloadFailedAssetBundleName + " code:" + code );
	}
);
```

+++

### onBeforePreload引数

Preloadを継続するかキャンセルするか、  
判断を盛り込むことができる。

```
(willLoadBundleNames, proceed, cancel) => {
    proceed();// or cancel();
}
```

この関数は「取得するABの情報が纏まった」  
タイミングで呼び出され、これからDLされる  
AB名一覧にアクセスすることができる。

DL前に取得ABの総重量を計算する等に便利。