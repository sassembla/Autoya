using System.Collections;
using System.Collections.Generic;
using AutoyaFramework.AssetBundles;
using UnityEngine;

public class LoadAssetBundle : MonoBehaviour {
	private const string basePath = "https://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/AssetBundle/AssetBundles/";

	// 試しに単機能を使う。prefabにtextureがつく、つかないでいろいろ判断する。
	// Use this for initialization
	void Start () {
		Debug.LogError("起動！");
		var dummyList = new AssetBundleList("1.0.0", 
			new AssetBundleInfo[]{
				// pngが一枚入ったAssetBundle
				new AssetBundleInfo(
					"bundlename", 
					new string[]{"Assets/AutoyaTests/Editor/Tests/AssetBundles/TestResources/textureName.png"}, 
					new string[0], 
					1434496255
				),
				// 他のAssetBundleへの依存があるAssetBundle
				new AssetBundleInfo(
					"dependsbundlename", 
					new string[]{"Assets/AutoyaTests/Editor/Tests/AssetBundles/TestResources/GameObject.prefab"}, 
					new string[]{"bundlename"}, 
					60598050
				)
			}
		);

		var loader = new AssetBundleLoader(basePath, dummyList);
		StartCoroutine(
			loader.LoadAsset(
				"Assets/AutoyaTests/Editor/Tests/AssetBundles/TestResources/GameObject.prefab", 
				(string assetName, GameObject data) => {
					Instantiate(data);
				},
				(assetName, error, reason) => {
					Debug.LogError("reason:" + reason);
				}
			)
		);
	}
	
}
