using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoyaFramework;
using AutoyaFramework.AssetBundles;
using UnityEngine;
using UnityEngine.UI;

public class PreloadAssetBundle2 : MonoBehaviour {

	// Use this for initialization
	IEnumerator Start () {
		
		/*
			this is sample of "preload assetBundles feature".

			the word "preload" in this sample means "download assetBundles without use."
			preloaded assetBundles are stored in storage cache. no difference between preloaded and downloaded assetBundles.

			case2:get preloadList from web, then get described assetBundles.
		 */
		
		Autoya.AssetBundle_DownloadAssetBundleListIfNeed(status => {}, (code, reason, autoyaStatus) => {});

		// wait downloading assetBundleList.
		while (!Autoya.AssetBundle_IsAssetBundleFeatureReady()) {
			yield return null;
		}

		/*
			get preloadList from web.
			the base filePath settings is located at below.
				https://github.com/sassembla/Autoya/blob/f020e02d707781f80e70c91a3dfd943b95cda25c/Assets/Autoya/Settings/AssetBundlesSettings.cs

			this preloadList contains 1 assetBundleName, "bundlename", contains 1 asset, "textureName.png"

			note that:
				this feature requires the condition:"assetBundleList is stored." for getting assetBundleInfo. (crc, hash, and dependencies.)
		 */

		var preloadListPath = "1.0.0/sample.preloadList.json";
		// this will become "https://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/AssetBundle/Mac/1.0.0/sample.preloadList.json".


		// download preloadList from web then preload described assetBundles.
		Autoya.AssetBundle_Preload(
			preloadListPath,
			(willLoadBundleNames, proceed, cancel) => {
				proceed();
			},
			progress => {
				Debug.Log("progress:" + progress);
			},
			() => {
				Debug.Log("preloading 1 listed assetBundles is finished.");
				
				// then, you can use these assetBundles immediately. without any downloading.
				Autoya.AssetBundle_LoadAsset<Texture2D>(
					"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png",
					(assetName, image) => {
						Debug.Log("asset:" + assetName + " is successfully loaded as:" + image);

						// create gameObject, then set tex to it as sprite.
						var gameObj = new GameObject("createdGameObject");
						var imageComponent = gameObj.AddComponent<Image>();
						imageComponent.sprite = Sprite.Create(image, new Rect(0.0f, 0.0f, image.width, image.height), new Vector2(0.5f, 0.5f), 100.0f);

						// find uGUI canvas then set. 
						var canvas = GameObject.Find("Canvas");
						gameObj.transform.SetParent(canvas.transform, false);
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

	private IEnumerator<bool> ShouldContinuePreloading (string[] bundleNames) {
		yield return true;
	}

	void OnApplicationQuit () {
		Autoya.AssetBundle_DeleteAllStorageCache();
	}
}
