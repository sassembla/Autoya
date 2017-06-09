using System;
using System.Collections.Generic;
using AutoyaFramework;
using Miyamasu;
using UnityEngine;



/**
	tests for Autoya Download list of whole AssetBundles.
*/
public class AssetBundleListDownloaderTests : MiyamasuTestRunner {
	[MTest] public void GetAssetBundleList () {
		Debug.LogError("AssetBundleListの取得について");
	}

	[MTest] public void ListUpdated () {
		Debug.LogError("リストがアップデートされた際、AssetBundles系の内容を更新する必要がある。この辺は単にダウンロードした時になんとかなればいいのか。");
	}
}
