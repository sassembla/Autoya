using System;
using System.Collections;
using AutoyaFramework;
using AutoyaFramework.AssetBundles;
using AutoyaFramework.Settings.AssetBundles;
using UnityEngine;

public class LoadAssetBundle : MonoBehaviour {

	void Start () {
		/*
			Autoya manages whole assetBundle information as the "AssetBundleList".
			latest file is located at 
				AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSETBUNDLELIST
				
		 */
		Autoya.AssetBundle_DownloadAssetBundleList(
			() => {
				Debug.Log("assetBundleList download succeeded.");
				
				/*
					then, you can load asset from web.
						
					assetBundleList has the information which asset is contained by specific assetBundle.
						(asset <-containes-- assetBundle <-info contains-- assetBundleList)

					the downloaded assetBundleList is stored in device. you can set the location and the way of read/write the list via OverridePoints.cs.
				*/

				/*
					load asset from web or cache.
					automatically download bundle then load asset on memory.
				*/
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
				Debug.LogError("failed to download assetBundleList from url:" + AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSETBUNDLELIST + " code:" + code + " reason:" + reason);
			}
		);
	}

	void OnApplicationQuit () {
		Autoya.AssetBundle_DeleteAllStorageCache();
	}
	
}
