using System.Collections.Generic;
using System.Linq;
using AutoyaFramework.AssetBundles;
using AutoyaFramework.Connections.HTTP;
using Miyamasu;
using UnityEngine;

/**
	tests for Autoya AssetBundle Read from cache.
*/
public class AssetBundleLoaderTests : MiyamasuTestRunner {
	/*
		・リスト取得/更新API 最初のリスト取得 -> リストのデータを保存
		と、
		・シーン単位でのAssetBundlePreloadリスト取得 -> (そもそも全体リストが書き換わっていたら云々のハンドラ -> 全体リスト取得) -> Preloadに入ってるAssetのローディングが終わるまで特定の値を返す関数
		と、
		・AssetBundleから特定のAssetを名前で呼び出す

		このテストはプレイモード時のみ、きちんと動く。
		LoadAssetAsyncがプレイ中でないとisDoneにならないのが原因。
			http://answers.unity3d.com/questions/1215257/proc-assetbundleloadassetasync-thread-in-editor.html
		
	*/
	private const string baseUrl = 
		#if UNITY_EDITOR_OSX
			"https://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/AssetBundle/Mac/";
		#elif UNITY_EDITOR_WIN
			"https://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/AssetBundle/Windows/";
		#elif UNITY_IOS
			"https://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/AssetBundle/iOS/";
		#elif UNITY_ANDROID
			"https://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/AssetBundle/Android/";
		#else
			"https://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/AssetBundle/Mac/";
		#endif

	private const string listNameSuffix =  
		#if UNITY_EDITOR_OSX
			"AssetBundles.StandaloneOSXIntel64";
		#elif UNITY_EDITOR_WIN
			"AssetBundles.Windows";
		#elif UNITY_IOS
			"AssetBundles.iOS";
		#elif UNITY_ANDROID
			"AssetBundles.Android";
		#else
			"AssetBundles.Mac";
		#endif

	private const string listNamePostfix = ".json";


	public static string BundlePath (string version) {
		return baseUrl + version + "/";
	}

	private string ListPath (string version) {
		return baseUrl + version + "/" + listNameSuffix + "_" + version.Replace(".", "_") + listNamePostfix;
	}


	private AssetBundleLoader loader;
	private AssetBundleList dummyList;
	[MSetup] public void Setup () {
		if (!IsTestRunningInPlayingMode()) {
			SkipCurrentTest("AssetBundle request should run on MainThread.");
		};

		dummyList = LoadListFromWeb();

		loader = new AssetBundleLoader(BundlePath("1.0.0"), dummyList, null);

		var cleaned = false;
		RunOnMainThread(
			() => {
				cleaned = loader.CleanCachedAssetBundles();
			}
		);
		if (!cleaned) {
			Assert(false, "clean cache failed.");
		}
	}

	public AssetBundleList LoadListFromWeb () {
		var downloaded = false;
		var downloader = new HTTPConnection();
		
		AssetBundleList listObj = null;

		RunEnumeratorOnMainThread(
			downloader.Get(
				"loadListFromWeb",
				null,
				ListPath("1.0.0"),
				(conId, code, respHeaders, data) => {
					downloaded = true;
					listObj = JsonUtility.FromJson<AssetBundleList>(data);
				},
				(conId, code, reason, respHeaders) => {
					Debug.LogError("failed conId:" + conId + " code:" + code + " reason:" + reason);
				}
			)
		);

		WaitUntil(
			() => downloaded,
			5,
			"failed to download list."
		);

		return listObj;
	}

	[MTeardown] public void Teardown () {
		if (loader != null) {
			RunOnMainThread(() => loader.CleanCachedAssetBundles());
		}
	}

	[MTest] public void LoadAssetByAssetName () {
		Texture2D tex = null;
		var done = false;

		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png", 
				(string assetName, Texture2D texAsset) => {
					tex = texAsset;
					done = true;
				},
				(assetName, failEnum, reason, status) => {
					done = true;
					Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
				}
			),
			false
		);

		WaitUntil(() => done, 10, "timeout to load AssetBundle.");
		Assert(tex != null, "tex is null");
	}

	[MTest] public void LoadSameAssetByAssetName () {
		{// 1
			Texture2D tex = null;
			var done = false;
			
			RunEnumeratorOnMainThread(
				loader.LoadAsset(
					"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png", 
					(string assetName, Texture2D texAsset) => {
						tex = texAsset;
						done = true;
					},
					(assetName, failEnum, reason, status) => {
						done = true;
						Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
					}
				),
				false
			);
			WaitUntil(() => done, 10, "timeout to load AssetBundle.");
			Assert(tex != null, "tex is null");
		}

		{// 2 maybe cached on memory.
			Texture2D tex = null;
			var done = false;
			
			RunEnumeratorOnMainThread(
				loader.LoadAsset(
					"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png", 
					(string assetName, Texture2D texAsset) => {
						tex = texAsset;
						done = true;
					},
					(assetName, failEnum, reason, status) => {
						done = true;
						Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
					}
				),
				false
			);
			WaitUntil(() => done, 10, "timeout to load AssetBundle.");
			Assert(tex != null, "tex is null");
		}
	}

	[MTest] public void LoadAssetWithDependency () {
		GameObject prefab = null;
		var done = false;

		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName1.prefab", 
				(string assetName, GameObject prefabAsset) => {
					prefab = prefabAsset;
					done = true;
				},
				(assetName, failEnum, reason, status) => {
					done = true;
					Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
				}
			),
			false
		);

		WaitUntil(() => done, 10, "timeout to load depends prefab.");
		Assert(prefab != null, "prefab is null");

		if (prefab != null) {
			RunOnMainThread(
				() => {
					// check prefab instance contains dependent texture.
					var obj = GameObject.Instantiate(prefab);
					obj.hideFlags = obj.hideFlags | HideFlags.HideAndDontSave;
					var renderer = obj.GetComponent<SpriteRenderer>();
					var sprite = renderer.sprite;
					Assert(sprite != null, "sprite is null.");
				}
			);
		}
	}

	[MTest] public void LoadSameAssetWithDependsOnOneAssetBundle () {
		{// 1
			GameObject prefab = null;
			var done = false;

			RunEnumeratorOnMainThread(
				loader.LoadAsset(
					"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName1.prefab", 
					(string assetName, GameObject prefabAsset) => {
						prefab = prefabAsset;
						done = true;
					},
					(assetName, failEnum, reason, status) => {
						done = true;
						Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
					}
				),
				false
			);

			WaitUntil(() => done, 10, "timeout to load depends prefab.");
			Assert(prefab != null, "prefab is null");

			if (prefab != null) {
				RunOnMainThread(
					() => {
						// check prefab instance contains dependent texture.
						var obj = GameObject.Instantiate(prefab);
						obj.hideFlags = obj.hideFlags | HideFlags.HideAndDontSave;
						var renderer = obj.GetComponent<SpriteRenderer>();
						var sprite = renderer.sprite;
						Assert(sprite != null, "sprite is null.");
					}
				);
			}
		}
		
		{// 2 maybe cached on memory.
			GameObject prefab = null;
			var done = false;

			RunEnumeratorOnMainThread(
				loader.LoadAsset(
					"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName1.prefab", 
					(string assetName, GameObject prefabAsset) => {
						prefab = prefabAsset;
						done = true;
					},
					(assetName, failEnum, reason, status) => {
						done = true;
						Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
					}
				),
				false
			);

			WaitUntil(() => done, 10, "timeout to load depends prefab.");
			Assert(prefab != null, "prefab is null");

			if (prefab != null) {
				RunOnMainThread(
					() => {
						// check prefab instance contains dependent texture.
						var obj = GameObject.Instantiate(prefab);
						obj.hideFlags = obj.hideFlags | HideFlags.HideAndDontSave;
						var renderer = obj.GetComponent<SpriteRenderer>();
						var sprite = renderer.sprite;
						Assert(sprite != null, "sprite is null.");
					}
				);
			}
		}
	}

	/*
		1 <- 2
	*/
	[MTest] public void Load2Assets_1isDependsOnAnother_DependedFirst () {
		// texture = depended asset.
		Texture2D tex = null;
		var textureLoadDone = false;
		
		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png", 
				(string assetName, Texture2D texAsset) => {
					tex = texAsset;
					textureLoadDone = true;
				},
				(assetName, failEnum, reason, status) => {
					textureLoadDone = true;
					Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
				}
			),
			false
		);

		// prefab = depending asset.
		GameObject prefab = null;
		var prefabLoadDone = false;

		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName1.prefab", 
				(string assetName, GameObject prefabAsset) => {
					prefab = prefabAsset;
					prefabLoadDone = true;
				},
				(assetName, failEnum, reason, status) => {
					prefabLoadDone = true;
					Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
				}
			),
			false
		);

		WaitUntil(() => textureLoadDone && prefabLoadDone, 10, "texture and prefab load failed in time.");
		Assert(tex, "tex is null.");
		Assert(prefab, "prefab is null.");

		if (prefab != null) {
			RunOnMainThread(
				() => {
					// check prefab instance contains dependent texture.
					var obj = GameObject.Instantiate(prefab);
					obj.hideFlags = obj.hideFlags | HideFlags.HideAndDontSave;
					var renderer = obj.GetComponent<SpriteRenderer>();
					var sprite = renderer.sprite;
					Assert(sprite != null, "sprite is null.");
				}
			);
		}
	}

	/*
		1 -> 2
	*/
	[MTest] public void Load2Assets_1isDependsOnAnother_DependingFirst () {
		// prefab = depending asset.
		GameObject prefab = null;
		var prefabLoadDone = false;

		// load async
		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName1.prefab", 
				(string assetName, GameObject prefabAsset) => {
					prefab = prefabAsset;
					prefabLoadDone = true;
				},
				(assetName, failEnum, reason, status) => {
					prefabLoadDone = true;
					Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
				}
			),
			false
		);

		
		// texture = depended asset.
		Texture2D tex = null;
		var textureLoadDone = false;
		
		// load async
		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png", 
				(string assetName, Texture2D texAsset) => {
					tex = texAsset;
					textureLoadDone = true;
				},
				(assetName, failEnum, reason, status) => {
					textureLoadDone = true;
					Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
				}
			),
			false
		);
		

		WaitUntil(() => textureLoadDone && prefabLoadDone, 10, "texture and prefab load failed in time.");
		Assert(tex, "tex is null.");
		Assert(prefab, "prefab is null.");

		if (prefab != null) {
			RunOnMainThread(
				() => {
					// check prefab instance contains dependent texture.
					var obj = GameObject.Instantiate(prefab);
					obj.hideFlags = obj.hideFlags | HideFlags.HideAndDontSave;
					var renderer = obj.GetComponent<SpriteRenderer>();
					var sprite = renderer.sprite;
					Assert(sprite != null, "sprite is null.");
				}
			);
		}
	}

	/*
		A -> B <- C
	*/
	[MTest] public void Load2AssetsWhichDependsOnSameAssetBundle () {
		GameObject prefab1 = null;
		var prefabLoadDone1 = false;
		
		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName1.prefab", 
				(string assetName, GameObject prefabAsset) => {
					prefab1 = prefabAsset;
					prefabLoadDone1 = true;
				},
				(assetName, failEnum, reason, status) => {
					prefabLoadDone1 = true;
					Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
				}
			),
			false
		);

		GameObject prefab2 = null;
		var prefabLoadDone2 = false;

		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName2.prefab", 
				(string assetName, GameObject prefabAsset) => {
					prefab2 = prefabAsset;
					prefabLoadDone2 = true;
				},
				(assetName, failEnum, reason, status) => {
					prefabLoadDone2 = true;
					Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
				}
			),
			false
		);

		WaitUntil(() => prefabLoadDone1 && prefabLoadDone2, 10, "prefabs load failed.");
		Assert(prefab1, "prefab1 is null.");
		Assert(prefab2, "prefab2 is null.");

		if (prefab1 != null && prefab2 != null) {
			RunOnMainThread(
				() => {
					// check prefab instance contains dependent texture.
					var obj = GameObject.Instantiate(prefab1);
					obj.hideFlags = obj.hideFlags | HideFlags.HideAndDontSave;
					var renderer = obj.GetComponent<SpriteRenderer>();
					var sprite = renderer.sprite;
					Assert(sprite != null, "sprite is null.");

					var obj2 = GameObject.Instantiate(prefab2);
					obj2.hideFlags = obj2.hideFlags | HideFlags.HideAndDontSave;
					var renderer2 = obj2.GetComponent<SpriteRenderer>();
					var sprite2 = renderer2.sprite;
					Assert(sprite2 != null, "sprite is null.");
				}
			);
		}
	}

	/*
		A -> B -> C
	*/
	[MTest] public void NestedDependency () {
		GameObject prefab = null;
		var prefabLoadDone = false;

		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/nestedPrefab.prefab", 
				(string assetName, GameObject prefabAsset) => {
					prefab = prefabAsset;
					prefabLoadDone = true;
				},
				(assetName, failEnum, reason, status) => {
					Debug.Log("fail, failEnum:" + failEnum + " reason:" + reason);
					Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
					prefabLoadDone = true;
				}
			),
			false
		);

		WaitUntil(() => prefabLoadDone, 10, "prefabs load failed.");
		Assert(prefab, "prefab is null.");

		if (prefab != null) {
			RunOnMainThread(
				() => {
					// check prefab instance contains dependent texture.
					var nestedObj = GameObject.Instantiate(prefab);
					nestedObj.hideFlags = nestedObj.hideFlags | HideFlags.HideAndDontSave;
					var scriptObj = nestedObj.GetComponent<PrefabHolder>();
					Assert(scriptObj != null, "failed to get script.");

					var obj = scriptObj.prefab;
					Assert(obj != null, "failed to get contained prefab.");

					var renderer = obj.GetComponent<SpriteRenderer>();
					var sprite = renderer.sprite;
					Assert(sprite != null, "sprite is null.");
				}
			);
		}
	}

	[MTest] public void LoadCrcMismatchedBundle () {
		var modifiedDummyList = new AssetBundleList(dummyList);
		modifiedDummyList.assetBundles[0].crc = 1;// set wrong crc.

		loader = new AssetBundleLoader(BundlePath("1.0.0"), modifiedDummyList, null);

		// intentional fail.
		{
			var done = false;
			
			RunEnumeratorOnMainThread(
				loader.LoadAsset(
					"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png", 
					(string assetName, Texture2D texAsset) => {
						// do nothing.
					},
					(assetName, failEnum, reason, status) => {
						Assert(failEnum == AssetBundleLoadError.CrcMismatched, "error is not crc mismatched. failEnum:" + failEnum);
						done = true;
					}
				),
				false
			);

			WaitUntil(
				() => done,
				5,
				"failed to wait crc mismatch."
			);
		}
		
		modifiedDummyList = new AssetBundleList(dummyList);// use valid crc.
		loader = new AssetBundleLoader(BundlePath("1.0.0"), dummyList, null);

		// retry.
		{
			Texture2D tex = null;
			var done = false;
			
			RunEnumeratorOnMainThread(
				loader.LoadAsset(
					"Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png", 
					(string assetName, Texture2D texAsset) => {
						tex = texAsset;
						done = true;
					},
					(assetName, failEnum, reason, status) => {
						Debug.Log("fail, failEnum:" + failEnum + " reason:" + reason);
						Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
						done = true;
					}
				),
				false
			);

			WaitUntil(
				() => done,
				5,
				"failed to wait crc mismatch."
			);

			Assert(tex != null, "tex is null.");
		}
	}

	[MTest] public void UpdateListWithoutOnMemoryAssets () {
		Debug.LogError("UpdateListWithoutOnMemoryAssets、リストが更新されたらどうしよう的な。検知する機構からどう繋ぐか。");
	// 	// assume that: list is updated.
	// 	// 1.0.0 -> 1.0.1

	// 	var lostDownloader = new HTTPConnection();
	// 	var notified = false;
	// 	var listUpdated = false;

	// 	// renew list.
	// 	var newVersionStr = "1.0.1";
	// 	RunEnumeratorOnMainThread(
	// 		lostDownloader.Get(
	// 			"loadListFromWeb",
	// 			null,
	// 			ListPath(newVersionStr),
	// 			(conId, code, respHeaders, data) => {
	// 				var newList = JsonUtility.FromJson<AssetBundleList>(data);
					
	// 				// update loader's list.
	// 				loader.UpdateList(
	// 					BundlePath(newVersionStr), 
	// 					newList, 
	// 					(updatedAssetNames, bundleName) => {
	// 						notified = true;
	// 					}
	// 				);
	// 				listUpdated = true;
	// 			},
	// 			(conId, code, reason, respHeaders) => {
	// 				Debug.LogError("failed conId:" + conId + " code:" + code + " reason:" + reason);
	// 			}
	// 		)
	// 	);

	// 	WaitUntil(
	// 		() => listUpdated,
	// 		5,
	// 		"failed to update list."
	// 	);

	// 	Assert(!notified, "should not be notified. nothing on memory yet.");
	}

	// [MTest] public void UpdateListAndReceiveOnMemoryAssetsUpdated () {
	// 	{
	// 		var done = false;
	// 		// load assets.
	// 		RunEnumeratorOnMainThread(
	// 			loader.LoadAsset(
	// 				dummyList.assetBundles[0].assetNames[0],
	// 				(string assetName, Texture2D t) => {
	// 					done = true;
	// 				},
	// 				(assetName, e, reason, status) => {

	// 				}
	// 			)
	// 		);

	// 		WaitUntil(
	// 			() => done,
	// 			5,
	// 			"failed to get asset."
	// 		);
	// 	}
		
	// 	// assume that: list is updated.
	// 	// 1.0.0 -> 1.0.1

	// 	var lostDownloader = new HTTPConnection();
	// 	var notified = false;
	// 	var updatedBundleName = string.Empty;

	// 	// renew list.
	// 	var newVersionStr = "1.0.1";
	// 	RunEnumeratorOnMainThread(
	// 		lostDownloader.Get(
	// 			"loadListFromWeb",
	// 			null,
	// 			ListPath(newVersionStr),
	// 			(conId, code, respHeaders, data) => {
	// 				var newList = JsonUtility.FromJson<AssetBundleList>(data);
					
	// 				// update loader's list.
	// 				loader.UpdateList(
	// 					BundlePath(newVersionStr), 
	// 					newList, 
	// 					(updatedAssetNames, bundleName) => {
	// 						notified = true;
	// 						updatedBundleName = bundleName;
	// 					}
	// 				);
	// 			},
	// 			(conId, code, reason, respHeaders) => {
	// 				Debug.LogError("failed conId:" + conId + " code:" + code + " reason:" + reason);
	// 			}
	// 		)
	// 	);

	// 	WaitUntil(
	// 		() => notified,
	// 		5,
	// 		"failed to get on memory asset is notified."
	// 	);

	// 	Assert(updatedBundleName == dummyList.assetBundles[0].bundleName, "not match, actual:" + updatedBundleName + " expected:" + dummyList.assetBundles[0].bundleName);
	// }

	// [MTest] public void UpdateListAndGetAlreadyOnMemoryOldAsset () {
	// 	UnityEngine.Object oldAsset = null;
	// 	{
	// 		var done = false;
	// 		// load assets.
	// 		RunEnumeratorOnMainThread(
	// 			loader.LoadAsset(
	// 				dummyList.assetBundles[0].assetNames[0],
	// 				(string assetName, Texture2D t) => {
	// 					done = true;
	// 					oldAsset = t;
	// 				},
	// 				(assetName, e, reason, status) => {

	// 				}
	// 			)
	// 		);

	// 		WaitUntil(
	// 			() => done,
	// 			5,
	// 			"failed to get asset."
	// 		);
	// 	}
		
	// 	// assume that: list is updated.
	// 	// 1.0.0 -> 1.0.1

	// 	var lostDownloader = new HTTPConnection();
	// 	var downloaded = false;
		
	// 	// renew list.
	// 	var newVersionStr = "1.0.1";
	// 	RunEnumeratorOnMainThread(
	// 		lostDownloader.Get(
	// 			"loadListFromWeb",
	// 			null,
	// 			ListPath(newVersionStr),
	// 			(conId, code, respHeaders, data) => {
	// 				downloaded = true;
	// 				var newList = JsonUtility.FromJson<AssetBundleList>(data);
					
	// 				// update loader's list.
	// 				loader.UpdateList(
	// 					BundlePath(newVersionStr), 
	// 					newList, 
	// 					(updatedAssetNames, bundleName) => {
	// 						// update & on memory assets are detected.
	// 					}
	// 				);
	// 			},
	// 			(conId, code, reason, respHeaders) => {
	// 				Debug.LogError("failed conId:" + conId + " code:" + code + " reason:" + reason);
	// 			}
	// 		)
	// 	);

	// 	WaitUntil(
	// 		() => downloaded,
	// 		5,
	// 		"failed to download list."
	// 	);

	// 	// new list is downloaded and loader is updated.

	// 	// get old on memory asset from on memory cache.
	// 	{
	// 		var done = false;
	// 		var isSameOldAsset = false;
	// 		// load assets.
	// 		RunEnumeratorOnMainThread(
	// 			loader.LoadAsset(
	// 				dummyList.assetBundles[0].assetNames[0],
	// 				(string assetName, Texture2D t) => {
	// 					done = true;
	// 					if (oldAsset.GetInstanceID() == t.GetInstanceID()) {
	// 						isSameOldAsset = true;
	// 					}
	// 				},
	// 				(assetName, e, reason, status) => {
	// 					// do nothing.
	// 				}
	// 			)
	// 		);

	// 		WaitUntil(
	// 			() => done,
	// 			5,
	// 			"failed to get asset."
	// 		);

	// 		Assert(isSameOldAsset, "not same asset.");
	// 	}
	// }

	// [MTest] public void UpdateListAndUnloadNotifiedAssetThenGetNewAsset () {
	// 	UnityEngine.Object oldAsset = null;
	// 	{
	// 		var done = false;
	// 		// load assets.
	// 		RunEnumeratorOnMainThread(
	// 			loader.LoadAsset(
	// 				dummyList.assetBundles[0].assetNames[0],
	// 				(string assetName, Texture2D t) => {
	// 					done = true;
	// 					oldAsset = t;
	// 				},
	// 				(assetName, e, reason, status) => {

	// 				}
	// 			)
	// 		);

	// 		WaitUntil(
	// 			() => done,
	// 			5,
	// 			"failed to get asset."
	// 		);
	// 	}
		
	// 	// assume that: list is updated.
	// 	// 1.0.0 -> 1.0.1

	// 	var listDownloader = new HTTPConnection();
	// 	var downloaded = false;
		
	// 	// renew list.
	// 	var newVersionStr = "1.0.1";
	// 	RunEnumeratorOnMainThread(
	// 		listDownloader.Get(
	// 			"loadListFromWeb",
	// 			null,
	// 			ListPath(newVersionStr),
	// 			(conId, code, respHeaders, data) => {
	// 				downloaded = true;
	// 				var newList = JsonUtility.FromJson<AssetBundleList>(data);
					
	// 				// update loader's list.
	// 				loader.UpdateList(
	// 					BundlePath(newVersionStr), 
	// 					newList, 
	// 					(updatedAssetNames, bundleName) => {
	// 						// updated && on memory loaded assets are detected.
	// 						// unload it from memory then get again later.
	// 						loader.UnloadOnMemoryAssetBundle(bundleName);
	// 					}
	// 				);
	// 			},
	// 			(conId, code, reason, respHeaders) => {
	// 				Debug.LogError("failed conId:" + conId + " code:" + code + " reason:" + reason);
	// 			}
	// 		)
	// 	);

	// 	WaitUntil(
	// 		() => downloaded,
	// 		5,
	// 		"failed to download list."
	// 	);

	// 	// new list is downloaded and loader list is updated.

	// 	// get new asset from server.
	// 	{
	// 		var done = false;
	// 		var isAssetUpdated = false;
	// 		// load assets.
	// 		RunEnumeratorOnMainThread(
	// 			loader.LoadAsset(
	// 				dummyList.assetBundles[0].assetNames[0],
	// 				(string assetName, Texture2D t) => {
	// 					done = true;
	// 					if (oldAsset.GetInstanceID() != t.GetInstanceID()) {
	// 						isAssetUpdated = true;
	// 					}
	// 				},
	// 				(assetName, e, reason, status) => {
	// 					// do nothing.
	// 				}
	// 			)
	// 		);

	// 		WaitUntil(
	// 			() => done,
	// 			5,
	// 			"failed to get asset."
	// 		);

	// 		Assert(isAssetUpdated, "not updated.");
	// 	}
	// }

	[MTest] public void LoadMissingBundle () {
		Debug.LogWarning("指定したassetを含むbundleがDLできない場合のテスト");
	}

	[MTest] public void LoadMissingDependentBundle () {
		Debug.LogWarning("依存したassetが依存しているbundleが存在しなかったり、エラーを出すので、そのエラーがちゃんと出るか試す場合のテスト");
	}

	[MTest] public void LoadBundleWithTimeout () {
		Debug.LogWarning("指定したassetを時間内にDL、展開する(httpにのみ関連する)テスト");
	}

	[MTest] public void LoadAllAssetsOnce () {
		var loadedAssetAssets = new Dictionary<string, object>();
		var assetNames = dummyList.assetBundles.SelectMany(a => a.assetNames).ToArray();

		foreach (var loadingAssetName in assetNames) {
			switch (loadingAssetName) {
				case "Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png": {
					RunEnumeratorOnMainThread(
						loader.LoadAsset<Texture2D>(
							loadingAssetName, 
							(assetName, asset) => {
								loadedAssetAssets[assetName] = asset;
							},
							(assetName, failEnum, reason, status) => {
								Assert(false, "fail to load assetName:" + assetName + " failEnum:" + failEnum + " reason:" + reason);
							}
						),
						false
					);
					break;
				}
				default: {
					RunEnumeratorOnMainThread(
						loader.LoadAsset<GameObject>(
							loadingAssetName, 
							(assetName, asset) => {
								loadedAssetAssets[assetName] = asset;
							},
							(assetName, failEnum, reason, status) => {
								Assert(false, "fail to load assetName:" + assetName + " failEnum:" + failEnum + " reason:" + reason);
							}
						),
						false
					);
					break;
				}
			}
		}

		WaitUntil(() => loadedAssetAssets.Count == assetNames.Length, 10, "failed to load all assets.");
		foreach (var loadedAssetAssetKey in loadedAssetAssets.Keys) {
			var key = loadedAssetAssetKey;
			var asset = loadedAssetAssets[key];
			Assert(asset != null, "loaded asset:" + key + " is null.");
		}
	}

	[MTest] public void OnMemoryBundleNames () {
		/*
			load all assets.
		*/
		LoadAllAssetsOnce();

		var totalBundleCount = dummyList.assetBundles.Length;

		var onMemoryBundleNames = loader.OnMemoryBundleNames();
		Assert(onMemoryBundleNames.Length == totalBundleCount, "unmatched.");
	}

	[MTest] public void OnMemoryAssetNames () {
		/*
			load all assets.
		*/
		LoadAllAssetsOnce();

		var totalAssetCount = dummyList.assetBundles.SelectMany(ab => ab.assetNames).ToArray().Length;

		var onMemoryAssetNames = loader.OnMemoryAssetNames();
		Assert(onMemoryAssetNames.Length == totalAssetCount, "unmatched.");
	}

	[MTest] public void UnloadAllAssetBundles () {
		/*
			load all.
		*/
		LoadAllAssetsOnce();

		/*
			unload all.
		*/
		RunOnMainThread(() => loader.UnloadOnMemoryAssetBundles());

		WaitUntil(
			() => {
				var loadedAssetNames = loader.OnMemoryAssetNames();
				if (loadedAssetNames.Length == 0) {
					return true; 
				}
				return false;
			}, 
			10, 
			"failed to unload all assets."
		);
	}

	[MTest] public void GetContainedAssetBundleName () {
		var assetName = "Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png";
		var done = false;

		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				assetName, 
				(string loadedAssetName, Texture2D tex) => {
					done = true;
				},
				(loadedAssetName, failEnum, reason, status) => {
					
				}
			),
			false
		);

		WaitUntil(() => done, 10, "failed to load asset in time.");

		var containedAssetBundleName = loader.GetContainedAssetBundleName(assetName);
		Assert(containedAssetBundleName == "bundlename", "not match.");
	}

	[MTest] public void UnloadAssetBundle () {
		var assetName = "Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png";
		var done = false;

		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				assetName, 
				(string loadedAssetName, Texture2D prefabAsset) => {
					done = true;
				},
				(loadedAssetName, failEnum, reason, status) => {}
			),
			false
		);

		WaitUntil(() => done, 10, "failed to load asset in time.");

		var bundleName = loader.GetContainedAssetBundleName(assetName);
		RunOnMainThread(() => loader.UnloadOnMemoryAssetBundle(bundleName));

		Assert(!loader.OnMemoryAssetNames().Any(), "not unloaded.");
	}

	[MTest] public void IsBundleCachedOnStorage () {
		var assetName = "Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png";
		var done = false;

		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				assetName, 
				(string loadedAssetName, Texture2D prefabAsset) => {
					done = true;
				},
				(loadedAssetName, failEnum, reason, status) => {}
			),
			false
		);

		WaitUntil(() => done, 10, "failed to load asset in time.");

		var bundleName = loader.GetContainedAssetBundleName(assetName);

		RunOnMainThread(
			() => {
				Assert(loader.IsAssetBundleCachedOnStorage(bundleName), "not cached on storage.");
			}
		);
	}

	[MTest] public void IsBundleCachedOnMemory () {
		var assetName = "Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png";
		var done = false;

		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				assetName, 
				(string loadedAssetName, Texture2D prefabAsset) => {
					done = true;
				},
				(loadedAssetName, failEnum, reason, status) => {}
			),
			false
		);

		WaitUntil(() => done, 10, "failed to load asset in time.");

		var bundleName = loader.GetContainedAssetBundleName(assetName);
		Assert(loader.IsAssetBundleCachedOnMemory(bundleName), "not cached on memory.");
	}
	
	[MTest] public void AssetBundleInfoFromAssetName () {
		var assetName = "Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png";
		var assetInfo = loader.AssetBundleInfoOfAsset(assetName);
		
		Assert(assetInfo.assetNames.Any(), "no assetBundle containes this asset.");
	}

	[MTest] public void GetAssetBundleSize () {
		var assetName = "Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png";
		var assetInfo = loader.AssetBundleInfoOfAsset(assetName);

		Assert(assetInfo.size == 100, "not match.");
	}

	[MTest] public void UnloadOnMemoryAssetBundle () {
		Debug.LogError("UnloadOnMemoryAssetBundle not yet.");
	}

	[MTest] public void UnloadOnMemoryAsset () {
		Debug.LogError("UnloadOnMemoryAsset not yet.");
	}

	[MTest] public void Offline () {
		Debug.LogWarning("オフライン時のテストを追加したい。");
	}
}
