using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoyaFramework;
using AutoyaFramework.AssetBundles;
using AutoyaFramework.Settings.AssetBundles;
using Miyamasu;
using UnityEngine;



/**
	tests for download preloadList of the AssetBundles.
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
		loader = new AssetBundleLoader(AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSET + "1.0.0/", assetBundleList);


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
				AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_PRELOADLIST + "1.0.0/sample.preloadList.json", 
				progress => {
					Assert(progress == 1.0, "not match. progress:" + progress);
				},
				() => {
					Assert(!loader.IsAssetBundleCachedOnMemory("bundlename"), "cached on memory.");
					Assert(loader.IsAssetBundleCachedOnStorage("bundlename"), "not cached on storage.");
					done = true;
				},
				(code, reason, autoyaStatus) => {
					Debug.LogError("failed to download, code:" + code + " reason:" + reason);
				},
				(preloadFailedAssetBundleName, code, reason, autoyaStatus) => {
					Debug.LogError("failed to download, name:" + preloadFailedAssetBundleName + " code:" + code );
				}
			)
		);

		WaitUntil(() => done, 5, "not yet done.");
	}

	[MTest] public void PreloadWithCached_NoAdditionalDownload () {
		// preload once.
		GetPreloadList();

		var done = false;
		RunEnumeratorOnMainThread(
			assetBundlePreloader.Preload(
				loader,
				AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_PRELOADLIST + "1.0.0/sample.preloadList.json", 
				progress => {
					Assert(false, "should not be progress.");
				},
				() => {
					done = true;
				},
				(code, reason, autoyaStatus) => {
					Debug.LogError("failed to download, code:" + code + " reason:" + reason);
				},
				(preloadFailedAssetBundleName, code, reason, autoyaStatus) => {
					Debug.LogError("failed to download, name:" + preloadFailedAssetBundleName + " code:" + code );
				}
			)
		);

		WaitUntil(() => done, 5, "not yet done.");
	}

	[MTest] public void PreloadWithCachedAndNotCached () {
		// preload once.
		GetPreloadList();

		var doneCount = 0;
		RunEnumeratorOnMainThread(
			assetBundlePreloader.Preload(
				loader,
				AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_PRELOADLIST + "1.0.0/sample.preloadList2.json", 
				progress => {
					// 1.0
					Assert(progress == 1.0, "not match. progress:" + progress);
					doneCount++;
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
			)
		);

		WaitUntil(() => doneCount == 1, 5, "not yet done.");
	}

	[MTest] public void Preload2AssetBundles () {
		var doneCount = 0;
		RunEnumeratorOnMainThread(
			assetBundlePreloader.Preload(
				loader,
				AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_PRELOADLIST + "1.0.0/sample.preloadList2.json", 
				progress => {
					// 0.5, 1 の2つが来るはず
					Assert(
						progress == 0.5 ||
						progress == 1.0, 
						"not match. progress:" + progress
					);
					doneCount++;
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
			)
		);

		WaitUntil(() => doneCount == 2, 5, "not yet done. doneCount:" + doneCount);
	}

	[MTest] public void PreloadWithPreloadList () {
		var preloadBundleNames = loader.list.assetBundles.Select(info => info.bundleName).ToArray();
		var preloadList = new PreloadList("PreloadWithPreloadList", preloadBundleNames);
		
		var doneCount = 0;

		RunEnumeratorOnMainThread(
			assetBundlePreloader.Preload(
				loader,
				preloadList,
				progress => {
					doneCount++;
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
			)
		);

		WaitUntil(() => doneCount == preloadBundleNames.Length, 5, "not yet done. doneCount:" + doneCount);
	}
}
