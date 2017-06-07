using System;
using System.Collections;
using System.Collections.Generic;
using AutoyaFramework;
using AutoyaFramework.AssetBundles;
using Miyamasu;
using UnityEngine;



/**
	tests for Autoya Download prloadList of partial AssetBundles.

	調整中。うーん必要なのか？
	・複数のAssetBundleの名前をまとめたリストを扱う
	・リクエストをアプリ側からすることができ、リストを取得し、そのリストに含まれるものをDLする
	・DLが完了したら通知

	・preloadList自体はAssetBundleListから生成する(version値と対象になるAssetBundleName x Nを使う)
*/
public class AssetBundlePreloaderTests : MiyamasuTestRunner {
	
	private AssetBundlePreloader assetBundlePreloader;
	[MSetup] public void Setup () {
		assetBundlePreloader = new AssetBundlePreloader();
	}

	[MTeardownAttribute] public void Teardown () {

	}

	private AssetBundleList ExampleList () {
		var dummyList = new AssetBundleList(
			"Mac",
			"1.0.0", 
			new AssetBundleInfo[]{
				// pngが一枚入ったAssetBundle
				new AssetBundleInfo(
					"bundlename", 
					new string[]{"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName.png"}, 
					new string[0], 
					621985162,
					"578b73927bc11f6e80072caa17983776",
					100
				),
				// 他のAssetBundleへの依存があるAssetBundle
				new AssetBundleInfo(
					"dependsbundlename", 
					new string[]{"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName1.prefab"}, 
					new string[]{"bundlename"}, 
					2389728195,
					"1a3bdb638b301fd91fc5569e016604ad",
					100
				),
				// もう一つ、他のAssetBundleへの依存があるAssetBundle
				new AssetBundleInfo(
					"dependsbundlename2", 
					new string[]{"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName2.prefab"}, 
					new string[]{"bundlename"}, 
					1194278944,
					"b24db843879f6f82d9bee95e15559003",
					100
				),
				// nestedprefab -> dependsbundlename -> bundlename
				new AssetBundleInfo(
					"nestedprefab", 
					new string[]{"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/nestedPrefab.prefab"}, 
					new string[]{"dependsbundlename"}, 
					779842307,
					"30b17595dd7be703c2b04a6e4c3830ff",
					100
				),
				
			}
		);

		return dummyList;
	}

	[MTest] public void GetPreloadList () {
		var done = false;

		var assetBundleList = ExampleList();
		var loader = new AssetBundleLoader(AssetBundleLoaderTests.baseUrl, assetBundleList);

		RunEnumeratorOnMainThread(
			assetBundlePreloader.Preload(
				loader,
				AssetBundleLoaderTests.baseUrl + "sample.preloadList.json", 
				progress => {
					Debug.Log("progress:" + progress);
				},
				() => {
					Debug.Log("done!");
					done = true;
				},
				(code, reason, autoyaStatus) => {
					Debug.LogError("failed to download, code:" + code + " reason:" + reason);
				},
				(preloadFailedAssetBundleName, error, autoyaStatus) => {
					Debug.LogError("failed to download, name:" + preloadFailedAssetBundleName + " error:" + error );
				},
				5
			)
		);

		WaitUntil(() => done, 5, "not yet done.");
		// 追加で、preloadListに書いてあるものがdlできてればそれでOK.
		// 手元にhttpサーバ欲しいな。dropboxは怖いので避けたい。
	}
	
	[MTest] public void GetPreloadListButWholeABListIsChanged () {
		// なにかしらエラーが出せればいいと思うんだけど、エラーを出すコンテキストを集めておこう。まだ実装できない。
		Debug.LogError("特定のpreloadListを取得、書かれているAssetBundleを全て取得する、、、の開始時処理中で、リストそのものが書き換わったことを検知したので、停止する?");
	}

}
