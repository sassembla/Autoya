using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoyaFramework;
using AutoyaFramework.AssetBundles;
using UnityEngine;

public class PreloadAssetBundle : MonoBehaviour {

	// Use this for initialization
	IEnumerator Start () {
		/*
			this is sample of "preload assetBundles feature".

			the word "preload" in this sample means "download assetBundles before use."
			preloaded assetBundles are stored in storage cache. no difference between preloaded and downloaded assetBundles.

			case1:generate preloadList from assetBundleList, then get described assetBundles.
		 */

		// Autoya.AssetBundle_DiscardAssetBundleList();

		// download assetBundleList if assetBundleList is not stored.
		var isListStored = Autoya.AssetBundle_IsAssetBundleListReady();

		if (!isListStored) {
			// store assetBundleList in file storage.

			var done = false;
			Autoya.AssetBundle_DownloadAssetBundleList(
				"https://raw.githubusercontent.com/sassembla/Autoya/master/AssetBundles/StandaloneOSXIntel64/1.0.0/AssetBundles.StandaloneOSXIntel64_1_0_0.json",
				() => {
					done = true;
				},
				(code, reason, autoyaStatus) => {
					Debug.LogError("failed to download assetBundleList. code:" + code + " reason:" + reason);
				}
			);

			while (!done) {
				yield return null;
			}
		}

		/*
			let's preload specific assetBundle into storage.
		*/

		// get stored assetBundleList.
		var storedList = Autoya.AssetBundle_AssetBundleList();
		
		// create sample preloadList which contains all assetBundle names in assetBundleList.
		var assetBundleNames = storedList.assetBundles.Select(abInfo => abInfo.bundleName).ToArray();
		var newPreloadList = new PreloadList("samplePreloadList", assetBundleNames);

		Autoya.AssetBundle_Preload(
			newPreloadList,
			ShouldContinuePreloading,
			progress => {
				Debug.Log("progress:" + progress);
			},
			() => {
				Debug.Log("preloading all listed assetBundles is finished.");
				
				// then, you can use these assetBundles immediately. without any downloading.
				Autoya.AssetBundle_LoadAsset<GameObject>(
					"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName1.prefab",
					(assetName, prefab) => {
						Debug.Log("asset:" + assetName + " is successfully loaded as:" + prefab);

						// instantiate asset.
						Instantiate(prefab);
					},
					(assetName, err, reason, status) => {
						Debug.LogError("failed to load assetName:" + assetName + " err:" + err + " reason:" + reason);
					}
				);
			},
			(code, reason, autoyaStatus) => {
				Debug.LogError("preload failed. code:" + code + " reason:" + reason);
			},
			(downloadFailedAssetBundleName, code, reason, autoyaStatus) => {
				Debug.LogError("failed to preload assetBundle:" + downloadFailedAssetBundleName + ". code:" + code + " reason:" + reason);
			},
			10 // 10 parallel download! you can set more than 0.
		);
	}

	private IEnumerator<bool> ShouldContinuePreloading (string[] preloadingBundleNames) {
		yield return true;
	}

	void OnApplicationQuit () {
		Autoya.AssetBundle_DeleteAllStorageCache(
			(result, message) => {
				Debug.Log("the end of demo. in OnApplicationQuit, deleting all storage cached assetBundles. result:" + result + " (message:" + message + ")");
			},
			true
		);
	}
}
