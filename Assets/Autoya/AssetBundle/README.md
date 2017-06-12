#AssetBundles
AssetBundle management utilities.


## 概要
次のクラスがある。

* AssetBundleLoader
* AssetBundleListDownloader
* AssetBundlePreloader

## AssetBundleLoader

Asset名と型をリクエストすると、AssetBundleを取得 -> キャッシュ -> 展開 までを自動でやってくれる。

どのAssetBundleにどのAssetが含まれているか、crcやhash値はなにか、といった、ゲーム内で使用する包括的なAssetBundle情報のListを渡す必要がある。

Listは次のようなツリー構造を使用する。

```
list - target // target platform name.
		 \ version // human readable version desc.
		 \ assetBundles
		 		\ assetBundle
				 		\ bundleName // bundle name.
						\ assetNames // contained asset names. e,g, "Assets/Somewhere/texture.png"
						\ dependsBundleNames // the bundle names which this assetBundle depends on.
						\ crc // crc parameter. used for crc check.
						\ hash // hash parameter. used for exchange same asset from old one to new one.
```

実際の型定義はこちら。
[AssetBundleList.cs](https://github.com/sassembla/Autoya/blob/master/Assets/Autoya/AssetBundle/AssetBundleList.cs#L1)


#### 状態について
各AssetBundleについて、Loader内部で3段階の状態を持っている。

```
NotCached -> OnStorage <-> OnMemory(AssetBundleLoaded)
```

Assetを取得する場合、Assetを含んでいるAssetBundleの状態が変わり、最終的にOnMemory状態になったAssetBundleからAssetが生成される。
基本的に、AssetBundleLoaderの内部のAssetBundleの状態を気にする必要はないが、知っておくとメモリ状態を気にすることができるので、楽だと思う。

現在ロードされているAssetBundleやAssetの情報については、[AssetBundleLoader#OnMemoryBundleNames](https://github.com/sassembla/Autoya/blob/master/Assets/Autoya/AssetBundle/AssetBundleLoader.cs#L554)
、[AssetBundleLoader#OnMemoryAssetNames](https://github.com/sassembla/Autoya/blob/master/Assets/Autoya/AssetBundle/AssetBundleLoader.cs#L559)
を使って取得できる。

#### Loadについて
次のコードでAssetBundleに含まれるAssetのロードができる。
ロードされたAssetが含まれるAssetBundleは、OnMemory状態になりメモリ上にキャッシュされている。

```
loader.LoadAsset(
	"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png", 
	(string assetName, Texture2D texAsset) => {
		tex = texAsset;
		done = true;
	},
	(assetName, failEnum, reason, status) => {
		done = true;
		Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
	}
)
```

型の指定については、こう書くこともできる。

```
loader.LoadAsset<Texture2D>(
	"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png", 
	(assetName, texAsset) => {
		tex = texAsset;
		done = true;
	},
	(assetName, failEnum, reason, status) => {
		done = true;
		Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
	}
)
```

型情報はListには収録していないが、頑張ってくれ。というか使いたいデータの型なんだしわかるだろうきっと。

サンプルコードは[こちら](https://github.com/sassembla/Autoya/blob/master/Assets/AutoyaTests/Tests/AssetBundles/AssetBundleLoaderTests.cs#L111)。

#### Unloadについて
次のコードで、OnMemoryにロード済みのAssetBundleの解放ができる。

```
loader.UnloadOnMemoryAssetBundle(bundleName)
```

メモリ上にあるAssetBundleを破棄し、現在使用されているこのAssetBundle由来のAssetもすべて破棄される。


サンプルコードは[こちら](https://github.com/sassembla/Autoya/blob/master/Assets/AutoyaTests/Tests/AssetBundles/AssetBundleLoaderTests.cs#L1019)。


Asset名からそのAssetを含むAssetBundle名を知りたい場合、[AssetBundleLoader#GetContainedAssetBundleName](https://github.com/sassembla/Autoya/blob/master/Assets/Autoya/AssetBundle/AssetBundleLoader.cs#L564)を使用するといい。

ちなみにUnload時にAssetBundleに由来するAssetを残す方法は提供していない。


#### Assetを取得する際の内部フロー
次のようなフローで動作している。

* Asset名を指定した段階で、該当するAssetを含むAssetBundleがOnMemoryにロード済みであれば、そのAssetBundleからAssetを取得
* ロード済みでない場合はFileCacheを探索し、あればOnMemoryにロードし、そのAssetBundleからAssetを取得
* FileCacheに含まれていない場合、AssetBundleのダウンロードを開始し、完了時にOnMemoryにロードし、そのAssetBundleからAssetを取得
* ダウンロードに失敗した場合、エラーが返る
* 指定したAssetを含むAssetBundleがListに存在しない場合、エラーが返る


#### ゲーム中にListの更新を行う際の注意
##### 追記中

## AssetBundleListDownloader

特定のAssetBundle x Nの情報が入ったリストをWebや端末内から読み出す。
##### 追記中

## AssetBundlePreloader

特定のAssetBundle x NをWebから端末内にDLする。すでに保持している場合は何も起こらない。

DL時には進捗などが取得できる。
##### 追記中