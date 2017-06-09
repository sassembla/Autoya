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

	private AssetBundleLoader loader;

	[MSetup] public void Setup () {
		if (!IsTestRunningInPlayingMode()) {
			SkipCurrentTest("AssetBundle request should run on MainThread.");
		};

		assetBundlePreloader = new AssetBundlePreloader();

		var loaderTestObj = new AssetBundleLoaderTests();
		var assetBundleList = loaderTestObj.LoadListFromWeb();
		loader = new AssetBundleLoader(AssetBundleLoaderTests.BundlePath(assetBundleList.version), assetBundleList);


		var cleaned = false;
		RunOnMainThread(
			() => {
				cleaned = loader.CleanCachedAssetBundles();
			}
		);
		if (!cleaned) {
			Assert(false, "clean cache failed 1.");
		}
	}

	[MTeardownAttribute] public void Teardown () {
		var cleaned = false;
		RunOnMainThread(
			() => {
				cleaned = loader.CleanCachedAssetBundles();
			}
		);
		if (!cleaned) {
			Assert(false, "clean cache failed 2.");
		}
	}

	
	[MTest] public void GetPreloadList () {
		var done = false;

		RunEnumeratorOnMainThread(
			assetBundlePreloader.Preload(
				loader,
				loader.assetDownloadBasePath + "sample.preloadList.json", 
				progress => {
					// do nothing.
				},
				() => {
					Assert(!loader.IsAssetBundleCachedOnMemory("bundlename"), "cached on memory.");
					Assert(loader.IsAssetBundleCachedOnStorage("bundlename"), "not cached on storage.");
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
	}

	[MTest] public void PreloadWithCached () {
		// preload once.
		GetPreloadList();

		var done = false;
		RunEnumeratorOnMainThread(
			assetBundlePreloader.Preload(
				loader,
				loader.assetDownloadBasePath + "sample.preloadList.json", 
				progress => {
					// 0, 1の2つが来るはず
					Debug.Log("progress:" + progress);
					if (progress == 1.0) {
						done = true;
					}
				},
				() => {
					Assert(false, "must not be downloaded.");
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
	}

	[MTest] public void PreloadWithCachedAndNotCached () {
		// preload once.
		GetPreloadList();

		var done = false;
		RunEnumeratorOnMainThread(
			assetBundlePreloader.Preload(
				loader,
				loader.assetDownloadBasePath + "sample.preloadList2.json", 
				progress => {
					// only one assetBundle should be download.
					Assert(progress == 0.0 | progress == 1.0, "not match. progress:" + progress);
				},
				() => {
					// do nothng.
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
	}

	[MTest] public void Preload2AssetBundles () {
		// preload once.
		GetPreloadList();

		var doneCount = 0;
		RunEnumeratorOnMainThread(
			assetBundlePreloader.Preload(
				loader,
				loader.assetDownloadBasePath + "sample.preloadList2.json", 
				progress => {
					// 0, 0.5, 1の3つが来るはず
					doneCount++;
				},
				() => {
					// do nothng.
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

		WaitUntil(() => doneCount == 3, 5, "not yet done.");
	}
}
