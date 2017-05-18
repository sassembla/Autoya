using System;
using System.Collections;
using AutoyaFramework;
using AutoyaFramework.AssetBundles;
using UnityEngine;

public class LoadAssetBundle : MonoBehaviour {
	private const string basePath = 
		#if UNITY_STANDALONE_OSX
		"https://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/AssetBundle/Mac/AssetBundles/";
		#elif UNITY_STANDALONE_WIN
		"https://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/AssetBundle/Windows/AssetBundles/";
		#elif UNITY_IOS
		"https://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/AssetBundle/iOS/AssetBundles/";
		#elif UNITY_ANDROID
		"https://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/AssetBundle/Android/AssetBundles/";
		#else
		"https://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/AssetBundle/Mac/AssetBundles/";
		#endif

	// Use this for initialization
	IEnumerator Start () {
		while (!Autoya.Auth_IsAuthenticated()) {
			yield return null;
		}
		
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

		// loadList -> preload assetBundles -> load asset.
		Autoya.AssetBundle_UpdateList(basePath, dummyList);

		/*
			load asset from bundle.
			automatically download bundle then load asset.
		*/
		Autoya.AssetBundle_LoadAsset<GameObject>(
			"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/nestedPrefab.prefab",
			(assetName, prefab) => {
				Debug.Log("asset:" + assetName + " is successfully loaded as:" + prefab);
				Instantiate(prefab);
			},
			(assetName, err, reason, status) => {
				Debug.LogError("failed to load assetName:" + assetName + " err:" + err + " reason:" + reason);
			}
		);
	}
	
}
