using System.Collections.Generic;
using System.Linq;
using AutoyaFramework.AssetBundles;
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
	private const string basePath = "https://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/AssetBundle/AssetBundles/";
	private AssetBundleLoader loader;
	private AssetBundleList dummyList;
	[MSetup] public void Setup () {
		if (!IsTestRunningInPlayingMode()) {
			SkipCurrentTest("AssetBundle request should run on MainThread.");
		};

		/*
			set dummy list of AssetBundleList.
		*/
		dummyList = new AssetBundleList("1.0.0", 
			new AssetBundleInfo[]{
				// pngが一枚入ったAssetBundle
				new AssetBundleInfo(
					"bundlename", 
					new string[]{"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName.png"}, 
					new string[0], 
					621985162
				),
				// 他のAssetBundleへの依存があるAssetBundle
				new AssetBundleInfo(
					"dependsbundlename", 
					new string[]{"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName1.prefab"}, 
					new string[]{"bundlename"}, 
					2389728195
				),
				// もう一つ、他のAssetBundleへの依存があるAssetBundle
				new AssetBundleInfo(
					"dependsbundlename2", 
					new string[]{"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName2.prefab"}, 
					new string[]{"bundlename"}, 
					1194278944
				),
				// nestedprefab -> dependsbundlename -> bundlename
				new AssetBundleInfo(
					"nestedprefab", 
					new string[]{"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/nestedPrefab.prefab"}, 
					new string[]{"dependsbundlename"}, 
					779842307
				),
				
			}
		);

		loader = new AssetBundleLoader(basePath, dummyList, null);
	}
	[MTeardown] public void Teardown () {
		if (loader != null) {
			RunOnMainThread(() => loader.CleanCachedAssetBundles());
		}
	}

	[MTest] public void LoadAssetByAssetName () {
		var cleaned = false;
		RunOnMainThread(
			() => {
				cleaned = loader.CleanCachedAssetBundles();
			}
		);
		if (!cleaned) {
			Assert(false, "clean cache failed.");
			return;
		}

		Texture2D tex = null;
		var done = false;

		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName.png", 
				(string assetName, Texture2D texAsset) => {
					tex = texAsset;
					done = true;
				},
				(assetName, failEnum, reason) => {
					Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
					done = true;
				}
			),
			false
		);

		WaitUntil(() => done, 10, "timeout to load AssetBundle.");
		Assert(tex != null, "tex is null");
    }

	[MTest] public void LoadSameAssetByAssetName () {
		var cleaned = false;
		RunOnMainThread(
			() => {
				cleaned = loader.CleanCachedAssetBundles();
			}
		);
		if (!cleaned) {
			Assert(false, "clean cache failed.");
			return;
		}

		{// 1
			Texture2D tex = null;
			var done = false;
			
			RunEnumeratorOnMainThread(
				loader.LoadAsset(
					"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName.png", 
					(string assetName, Texture2D texAsset) => {
						tex = texAsset;
						done = true;
					},
					(assetName, failEnum, reason) => {
						Debug.Log("fail, failEnum:" + failEnum + " reason:" + reason);
						Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
						done = true;
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
					"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName.png", 
					(string assetName, Texture2D texAsset) => {
						tex = texAsset;
						done = true;
					},
					(assetName, failEnum, reason) => {
						Debug.Log("fail, failEnum:" + failEnum + " reason:" + reason);
						Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
						done = true;
					}
				),
				false
			);
			WaitUntil(() => done, 10, "timeout to load AssetBundle.");
			Assert(tex != null, "tex is null");
		}
    }

	[MTest] public void LoadAssetWithDependency () {
		var cleaned = false;
		RunOnMainThread(
			() => {
				cleaned = loader.CleanCachedAssetBundles();
			}
		);
		if (!cleaned) {
			Assert(false, "clean cache failed.");
			return;
		}

		GameObject prefab = null;
		var done = false;

		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName1.prefab", 
				(string assetName, GameObject prefabAsset) => {
					prefab = prefabAsset;
					done = true;
				},
				(assetName, failEnum, reason) => {
					Debug.Log("fail, failEnum:" + failEnum + " reason:" + reason);
					Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
					done = true;
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
					var renderer = obj.GetComponent<SpriteRenderer>();
					var sprite = renderer.sprite;
					Assert(sprite != null, "sprite is null.");
				}
			);
		}
    }

	[MTest] public void LoadSameAssetWithDependsOnOneAssetBundle () {
        var cleaned = false;
		RunOnMainThread(
			() => {
				cleaned = loader.CleanCachedAssetBundles();
			}
		);
		if (!cleaned) {
			Assert(false, "clean cache failed.");
			return;
		}

		{// 1
			GameObject prefab = null;
			var done = false;

			RunEnumeratorOnMainThread(
				loader.LoadAsset(
					"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName1.prefab", 
					(string assetName, GameObject prefabAsset) => {
						prefab = prefabAsset;
						done = true;
					},
					(assetName, failEnum, reason) => {
						Debug.Log("fail, failEnum:" + failEnum + " reason:" + reason);
						Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
						done = true;
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
					"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName1.prefab", 
					(string assetName, GameObject prefabAsset) => {
						prefab = prefabAsset;
						done = true;
					},
					(assetName, failEnum, reason) => {
						Debug.Log("fail, failEnum:" + failEnum + " reason:" + reason);
						Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
						done = true;
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
		var cleaned = false;
		RunOnMainThread(
			() => {
				cleaned = loader.CleanCachedAssetBundles();
			}
		);
		if (!cleaned) {
			Assert(false, "clean cache failed.");
			return;
		}

		// texture = depended asset.
		Texture2D tex = null;
		var textureLoadDone = false;
		
		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName.png", 
				(string assetName, Texture2D texAsset) => {
					tex = texAsset;
					textureLoadDone = true;
				},
				(assetName, failEnum, reason) => {
					Debug.Log("fail, failEnum:" + failEnum + " reason:" + reason);
					Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
					textureLoadDone = true;
				}
			),
			false
		);

		// prefab = depending asset.
        GameObject prefab = null;
		var prefabLoadDone = false;

		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName1.prefab", 
				(string assetName, GameObject prefabAsset) => {
					prefab = prefabAsset;
					prefabLoadDone = true;
				},
				(assetName, failEnum, reason) => {
					Debug.Log("fail, failEnum:" + failEnum + " reason:" + reason);
					Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
					prefabLoadDone = true;
				}
			),
			false
		);
		
		WaitUntil(() => textureLoadDone && prefabLoadDone, 10, "texture and prefab load failed in time.");

		if (prefab != null) {
			RunOnMainThread(
				() => {
					// check prefab instance contains dependent texture.
					var obj = GameObject.Instantiate(prefab);
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
		var cleaned = false;
		RunOnMainThread(
			() => {
				cleaned = loader.CleanCachedAssetBundles();
			}
		);
		if (!cleaned) {
			Assert(false, "clean cache failed.");
			return;
		}

		// prefab = depending asset.
        GameObject prefab = null;
		var prefabLoadDone = false;

		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName1.prefab", 
				(string assetName, GameObject prefabAsset) => {
					
					prefab = prefabAsset;
					prefabLoadDone = true;
				},
				(assetName, failEnum, reason) => {
					Debug.Log("fail, failEnum:" + failEnum + " reason:" + reason);
					Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
					prefabLoadDone = true;
				}
			),
			false
		);

        // texture = depended asset.
		Texture2D tex = null;
		var textureLoadDone = false;
		
		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName.png", 
				(string assetName, Texture2D texAsset) => {
					tex = texAsset;
					textureLoadDone = true;
				},
				(assetName, failEnum, reason) => {
					Debug.Log("fail, failEnum:" + failEnum + " reason:" + reason);
					Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
					textureLoadDone = true;
				}
			),
			false
		);

		WaitUntil(() => textureLoadDone && prefabLoadDone, 10, "texture and prefab load failed in time.");

		if (prefab != null) {
			RunOnMainThread(
				() => {
					// check prefab instance contains dependent texture.
					var obj = GameObject.Instantiate(prefab);
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
		var cleaned = false;
		RunOnMainThread(
			() => {
				cleaned = loader.CleanCachedAssetBundles();
			}
		);
		if (!cleaned) {
			Assert(false, "clean cache failed.");
			return;
		}

		GameObject prefab1 = null;
		var prefabLoadDone1 = false;

		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName1.prefab", 
				(string assetName, GameObject prefabAsset) => {
					prefab1 = prefabAsset;
					prefabLoadDone1 = true;
				},
				(assetName, failEnum, reason) => {
					Debug.Log("fail, failEnum:" + failEnum + " reason:" + reason);
					Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
					prefabLoadDone1 = true;
				}
			),
			false
		);

		GameObject prefab2 = null;
		var prefabLoadDone2 = false;

		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName2.prefab", 
				(string assetName, GameObject prefabAsset) => {
					prefab2 = prefabAsset;
					prefabLoadDone2 = true;
				},
				(assetName, failEnum, reason) => {
					Debug.Log("fail, failEnum:" + failEnum + " reason:" + reason);
					Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
					prefabLoadDone2 = true;
				}
			),
			false
		);

		WaitUntil(() => prefabLoadDone1 && prefabLoadDone2, 10, "prefabs load failed.");

		if (prefab1 != null && prefab2 != null) {
			RunOnMainThread(
				() => {
					// check prefab instance contains dependent texture.
					var obj = GameObject.Instantiate(prefab1);
					var renderer = obj.GetComponent<SpriteRenderer>();
					var sprite = renderer.sprite;
					Assert(sprite != null, "sprite is null.");

					var obj2 = GameObject.Instantiate(prefab2);
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
		var cleaned = false;
		RunOnMainThread(
			() => {
				cleaned = loader.CleanCachedAssetBundles();
			}
		);
		if (!cleaned) {
			Assert(false, "clean cache failed.");
			return;
		}

		GameObject prefab = null;
		var prefabLoadDone = false;

		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/nestedPrefab.prefab", 
				(string assetName, GameObject prefabAsset) => {
					prefab = prefabAsset;
					prefabLoadDone = true;
				},
				(assetName, failEnum, reason) => {
					Debug.Log("fail, failEnum:" + failEnum + " reason:" + reason);
					Assert(false, "fail, failEnum:" + failEnum + " reason:" + reason);
					prefabLoadDone = true;
				}
			),
			false
		);

		WaitUntil(() => prefabLoadDone, 10, "prefabs load failed.");

		if (prefab != null) {
			RunOnMainThread(
				() => {
					// check prefab instance contains dependent texture.
					var nestedObj = GameObject.Instantiate(prefab);

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


	[MTest] public void LoadMissingBundle () {
		Debug.LogError("指定したassetを含むbundleがDLできない");
	}
	[MTest] public void LoadMissingDependentBundle () {
		Debug.LogError("依存したassetが依存しているbundleが存在しなかったり、エラーを出すので、そのエラーがちゃんと出るか試す");
	}

	[MTest] public void LoadBundleWithTimeout () {
		Debug.LogError("指定したassetを時間内にDL、展開する(httpにのみ関連する)");
	}

	[MTest] public void LoadAllAssetsOnce () {
		var cleaned = false;
		RunOnMainThread(
			() => {
				cleaned = loader.CleanCachedAssetBundles();
			}
		);
		if (!cleaned) {
			Assert(false, "clean cache failed.");
			return;
		}

		var loadedAssetAssets = new Dictionary<string, object>();
		var assetNames = dummyList.assetBundles.SelectMany(a => a.assetNames).ToArray();

		foreach (var loadingAssetName in assetNames) {
			switch (loadingAssetName) {
				case "Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName.png": {
					RunEnumeratorOnMainThread(
						loader.LoadAsset<Texture2D>(
							loadingAssetName, 
							(assetName, asset) => {
								loadedAssetAssets[assetName] = asset;
							},
							(assetName, failEnum, reason) => {
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
							(assetName, failEnum, reason) => {
								Assert(false, "fail to load assetName:" + assetName + " failEnum:" + failEnum + " reason:" + reason);
							}
						),
						false
					);
					break;
				}
			}
		}

		WaitUntil(() => loadedAssetAssets.Count == assetNames.Length, 20, "failed to load all assets.");
		foreach (var loadedAssetAssetKey in loadedAssetAssets.Keys) {
			var key = loadedAssetAssetKey;
			var asset = loadedAssetAssets[key];
			Assert(asset != null, "loaded asset:" + key + " is null.");
		}
    }

	[MTest] public void OnMemoryAssetNames () {
		var cleaned = false;
		RunOnMainThread(
			() => {
				cleaned = loader.CleanCachedAssetBundles();
			}
		);
		if (!cleaned) {
			Assert(false, "clean cache failed.");
			return;
		}

		/*
			load all assets.
		*/
		LoadAllAssetsOnce();

		var totalAssetCount = dummyList.assetBundles.SelectMany(ab => ab.assetNames).ToArray().Length;

		var onMemoryAssetNames = loader.OnMemoryAssetNames();
		Assert(onMemoryAssetNames.Length == totalAssetCount, "unmatched.");
	}

	[MTest] public void UnloadAllAssetBundles () {
		var cleaned = false;
		RunOnMainThread(
			() => {
				cleaned = loader.CleanCachedAssetBundles();
			}
		);
		if (!cleaned) {
			Assert(false, "clean cache failed.");
			return;
		}

		/*
			load all.
		*/
		LoadAllAssetsOnce();

		/*
			unload all.
		*/
		RunOnMainThread(() => loader.UnloadAllAssetBundles());

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
		var cleaned = false;
		RunOnMainThread(
			() => {
				cleaned = loader.CleanCachedAssetBundles();
			}
		);

		if (!cleaned) {
			Assert(false, "clean cache failed.");
			return;
		}

		var assetName = "Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName.png";
		var done = false;

		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				assetName, 
				(string loadedAssetName, Texture2D tex) => {
					done = true;
				},
				(loadedAssetName, failEnum, reason) => {
					
				}
			),
			false
		);

		WaitUntil(() => done, 10, "failed to load asset in time.");

		var containedAssetBundleName = loader.GetContainedAssetBundleName(assetName);
		Assert(containedAssetBundleName == "bundlename", "not match.");
	}

	[MTest] public void UnloadAssetBundle () {
		var cleaned = false;
		RunOnMainThread(
			() => {
				cleaned = loader.CleanCachedAssetBundles();
			}
		);
		if (!cleaned) {
			Assert(false, "clean cache failed.");
			return;
		}

		var assetName = "Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName.png";
		var done = false;

		RunEnumeratorOnMainThread(
			loader.LoadAsset(
				assetName, 
				(string loadedAssetName, Texture2D prefabAsset) => {
					done = true;
				},
				(loadedAssetName, failEnum, reason) => {}
			),
			false
		);

		WaitUntil(() => done, 10, "failed to load asset in time.");

		var bundleName = loader.GetContainedAssetBundleName(assetName);
		RunOnMainThread(() => loader.UnloadAssetBundle(bundleName));

		Assert(!loader.OnMemoryAssetNames().Any(), "not unloaded.");
    }

	[MTest] public void Offline () {
		Debug.LogError("オフライン時のテストを追加したい。");
	}
}
