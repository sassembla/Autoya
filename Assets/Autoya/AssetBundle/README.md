#AssetBundles
AssetBundle management utilities.


## 概要
次のクラスがある。

* AssetBundleLoader
* PresetDownloader
* ListDownloader

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
NotCached -> CachedOnStorage <-> CachedOnMemory(AssetBundleLoaded)
```

Assetを取得する場合、Assetを含んでいるAssetBundleの状態が変わり、最終的にAssetが生成される。
基本的に、AssetBundleLoaderの内部のAssetBundleの状態を気にする必要はない。

現在ロードされているAssetBundleやAssetの情報については、[AssetBundleLoader#OnMemoryBundleNames](https://github.com/sassembla/Autoya/blob/master/Assets/Autoya/AssetBundle/AssetBundleLoader.cs#L554)
、[AssetBundleLoader#OnMemoryAssetNames](https://github.com/sassembla/Autoya/blob/master/Assets/Autoya/AssetBundle/AssetBundleLoader.cs#L559)
を使って取得できる。


#### Assetを取得する際のフロー

* Assetを指定した段階で、該当するAssetを含むAssetBundleがOnMemoryにロード済みであれば、そのAssetBundleからAssetを取得
* ロード済みでない場合はFileCacheを探索し、あればOnMemoryにロードし、そのAssetBundleからAssetを取得
* FileCacheに含まれていない場合、AssetBundleのダウンロードを開始し、完了時にOnMemoryにロードし、そのAssetBundleからAssetを取得
* ダウンロードに失敗した場合、エラーが返る
* 指定したAssetを含むAssetBundleがListに存在しない場合、エラーが返る


#### ゲーム中にListの更新を行う際の注意
例えばゲームプレイ中にListが更新された場合 = サーバ上などでゲームリソースの更新が行われた場合、最新のListをAssetBundleDownloaderに対して[AssetBundleLoader#UpdateList](https://github.com/sassembla/Autoya/blob/master/Assets/Autoya/AssetBundle/AssetBundleLoader.cs#L77)でセットし、最新版のリソースを取得して使用するように仕向けることができる。


ただし、次の条件を満たすAsset | AssetBundleに関しては、最新版をサーバから取得し使用するために、一度Unloadする必要がある。

* ListUpdate時、既にOnMemoryに乗っていて、使用中のAsset | AssetBundle


**もしUnloadしなかった場合、更新前の、OnMemoryにあるAsset | AssetBundleが継続して使用される。**

ListUpdateメソッドには「更新がかかったAsset | AssetBundleがOnMemoryにあるかどうかチェックし、通知する」ためのコールバック`updatedOnMemoryAssetNameAndBundleName` があるため、もし現在使用中のAssetに更新が掛かったら、下記のようなコードでUnloadして再読み込みをするとよい。

[UpdateList sample](https://github.com/sassembla/Autoya/blob/master/Assets/AutoyaTests/Tests/AssetBundles/AssetBundleLoaderTests.cs#L834)

```
// update loader's list.
loader.UpdateList(
	BundlePath(newVersionStr), 
	newList, 
	(updatedAssetNames, bundleName) => {
		// updated && on memory loaded assets are detected.
		// unload it from memory then get again later.
		loader.UnloadOnMemoryAssetBundle(bundleName);
	}
);
```

もちろん、更新が掛かったAssetをすぐに適応せず、適当に画面遷移する際にUnloadして再度読み込み時に最新版が使われるようにする、という手法もありえる。


## PresetDownloader

開発中。特定のAssetBundle x Nのグループを作成し、それらを事前に読み込んでおく機能。
ぶっちゃけ既存の機能でも実現可能。


## ListDownloader

開発中。ListをDLするか、ストレージから読み出すかの2択を制御する機能。
ぶっちゃけわざわざ作る必要性がなくて放置中。

