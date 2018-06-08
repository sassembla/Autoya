using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoyaFramework.AssetBundles;
using AutoyaFramework.Connections.HTTP;
using AutoyaFramework.Settings.AssetBundles;
using Miyamasu;
using UnityEngine;
using UnityEngine.SceneManagement;

/**
	tests for Autoya AssetBundle Read from cache.
*/
public class AssetBundleLoaderTests : MiyamasuTestRunner
{

    private string abListPath = "https://raw.githubusercontent.com/sassembla/Autoya/master/AssetBundles/main_assets/" + AssetBundlesSettings.PLATFORM_STR + "/1.0.0/main_assets.json";
    private string abDlPath = "https://raw.githubusercontent.com/sassembla/Autoya/master/AssetBundles/main_assets/" + AssetBundlesSettings.PLATFORM_STR + "/";

    private string sceneListPath = "https://raw.githubusercontent.com/sassembla/Autoya/master/AssetBundles/scenes/" + AssetBundlesSettings.PLATFORM_STR + "/1.0.0/scenes.json";
    private string sceneAbDlPath = "https://raw.githubusercontent.com/sassembla/Autoya/master/AssetBundles/scenes/" + AssetBundlesSettings.PLATFORM_STR + "/";


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

    private AssetBundleLoader loader;
    private AssetBundleList bundleList;
    [MSetup]
    public IEnumerator Setup()
    {
        var listCor = LoadListFromWeb(abListPath);

        yield return listCor;
        bundleList = listCor.Current as AssetBundleList;

        loader = new AssetBundleLoader(identity => abDlPath + "1.0.0/");
        loader.UpdateAssetBundleList(bundleList);

        var cleaned = loader.CleanCachedAssetBundles();

        if (!cleaned)
        {
            Fail("clean cache failed.");
        }
    }

    public IEnumerator LoadListFromWeb(string listDownloadPath)
    {
        var downloaded = false;
        var downloader = new HTTPConnection();

        AssetBundleList listObj = null;
        yield return downloader.Get(
            "loadListFromWeb",
            null,
            listDownloadPath,
            (conId, code, respHeaders, data) =>
            {
                downloaded = true;
                listObj = JsonUtility.FromJson<AssetBundleList>(data);
            },
            (conId, code, reason, respHeaders) =>
            {
                Debug.LogError("failed conId:" + conId + " code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => downloaded,
            () => { throw new TimeoutException("failed to download list."); }
        );
        yield return listObj;
    }

    [MTeardown]
    public void Teardown()
    {
        loader.CleanCachedAssetBundles();
    }



    [MTest]
    public IEnumerator LoadAssetByAssetName()
    {
        Texture2D tex = null;
        var done = false;

        yield return loader.LoadAsset(
            "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName.png",
            (string assetName, Texture2D texAsset) =>
            {
                tex = texAsset;
                done = true;
            },
            (assetName, failEnum, reason, status) =>
            {
                done = true;
                Fail("fail, failEnum:" + failEnum + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("timeout to load AssetBundle."); }
        );
        True(tex != null, "tex is null");
    }

    [MTest]
    public IEnumerator LoadSameAssetByAssetName()
    {
        {// 1
            Texture2D tex = null;
            var done = false;

            yield return loader.LoadAsset(
                "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName.png",
                (string assetName, Texture2D texAsset) =>
                {
                    tex = texAsset;
                    done = true;
                },
                (assetName, failEnum, reason, status) =>
                {
                    done = true;
                    Fail("fail, failEnum:" + failEnum + " reason:" + reason);
                }
            );

            yield return WaitUntil(
                () => done,
                () => { throw new TimeoutException("timeout to load AssetBundle."); }
            );

            True(tex != null, "tex is null");
        }

        {// 2 maybe cached on memory.
            Texture2D tex = null;
            var done = false;

            yield return loader.LoadAsset(
                "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName.png",
                (string assetName, Texture2D texAsset) =>
                {
                    tex = texAsset;
                    done = true;
                },
                (assetName, failEnum, reason, status) =>
                {
                    done = true;
                    Fail("fail, failEnum:" + failEnum + " reason:" + reason);
                }
            );

            yield return WaitUntil(() => done, () => { throw new TimeoutException("timeout to load AssetBundle."); });
            True(tex != null, "tex is null");
        }
    }

    [MTest]
    public IEnumerator LoadAssetWithDependency()
    {
        GameObject prefab = null;
        var done = false;

        yield return loader.LoadAsset(
            "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName1.prefab",
            (string assetName, GameObject prefabAsset) =>
            {
                prefab = prefabAsset;
                done = true;
            },
            (assetName, failEnum, reason, status) =>
            {
                done = true;
                Fail("fail, failEnum:" + failEnum + " reason:" + reason);
            }
        );

        yield return WaitUntil(() => done, () => { throw new TimeoutException("timeout to load depends prefab."); });
        True(prefab != null, "prefab is null");

        if (prefab != null)
        {
            // check prefab instance contains dependent texture.
            var obj = GameObject.Instantiate(prefab);
            obj.hideFlags = obj.hideFlags | HideFlags.HideAndDontSave;
            var renderer = obj.GetComponent<SpriteRenderer>();
            var sprite = renderer.sprite;

            True(sprite != null, "sprite is null.");
        }
    }

    [MTest]
    public IEnumerator LoadSameAssetWithDependsOnOneAssetBundle()
    {
        {// 1
            GameObject prefab = null;
            var done = false;

            yield return loader.LoadAsset(
                "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName1.prefab",
                (string assetName, GameObject prefabAsset) =>
                {
                    prefab = prefabAsset;
                    done = true;
                },
                (assetName, failEnum, reason, status) =>
                {
                    done = true;
                    Fail("fail, failEnum:" + failEnum + " reason:" + reason);
                }
            );

            yield return WaitUntil(() => done, () => { throw new TimeoutException("timeout to load depends prefab."); });
            True(prefab != null, "prefab is null");

            if (prefab != null)
            {

                // check prefab instance contains dependent texture.
                var obj = GameObject.Instantiate(prefab);
                obj.hideFlags = obj.hideFlags | HideFlags.HideAndDontSave;
                var renderer = obj.GetComponent<SpriteRenderer>();
                var sprite = renderer.sprite;
                True(sprite != null, "sprite is null.");
            }
        }

        {// 2 maybe cached on memory.
            GameObject prefab = null;
            var done = false;

            yield return loader.LoadAsset(
                "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName1.prefab",
                (string assetName, GameObject prefabAsset) =>
                {
                    prefab = prefabAsset;
                    done = true;
                },
                (assetName, failEnum, reason, status) =>
                {
                    done = true;
                    Fail("fail, failEnum:" + failEnum + " reason:" + reason);
                }
            );

            yield return WaitUntil(() => done, () => { throw new TimeoutException("timeout to load depends prefab."); });
            True(prefab != null, "prefab is null");

            if (prefab != null)
            {
                // check prefab instance contains dependent texture.
                var obj = GameObject.Instantiate(prefab);
                obj.hideFlags = obj.hideFlags | HideFlags.HideAndDontSave;
                var renderer = obj.GetComponent<SpriteRenderer>();
                var sprite = renderer.sprite;
                True(sprite != null, "sprite is null.");
            }
        }
    }

    /*
		1 <- 2
	*/
    [MTest]
    public IEnumerator Load2Assets_1isDependsOnAnother_DependedFirst()
    {
        // texture = depended asset.
        Texture2D tex = null;
        var textureLoadDone = false;


        yield return loader.LoadAsset(
            "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName.png",
            (string assetName, Texture2D texAsset) =>
            {
                tex = texAsset;
                textureLoadDone = true;
            },
            (assetName, failEnum, reason, status) =>
            {
                textureLoadDone = true;
                Fail("fail, failEnum:" + failEnum + " reason:" + reason);
            }
        );

        // prefab = depending asset.
        GameObject prefab = null;
        var prefabLoadDone = false;

        yield return loader.LoadAsset(
            "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName1.prefab",
            (string assetName, GameObject prefabAsset) =>
            {
                prefab = prefabAsset;
                prefabLoadDone = true;
            },
            (assetName, failEnum, reason, status) =>
            {
                prefabLoadDone = true;
                Fail("fail, failEnum:" + failEnum + " reason:" + reason);
            }
        );

        yield return WaitUntil(() => textureLoadDone && prefabLoadDone, () => { throw new TimeoutException("texture and prefab load failed in time."); });
        True(tex, "tex is null.");
        True(prefab, "prefab is null.");

        if (prefab != null)
        {
            // check prefab instance contains dependent texture.
            var obj = GameObject.Instantiate(prefab);
            obj.hideFlags = obj.hideFlags | HideFlags.HideAndDontSave;
            var renderer = obj.GetComponent<SpriteRenderer>();
            var sprite = renderer.sprite;
            True(sprite != null, "sprite is null.");
        }
    }

    /*
		1 -> 2
	*/
    [MTest]
    public IEnumerator Load2Assets_1isDependsOnAnother_DependingFirst()
    {
        // prefab = depending asset.
        GameObject prefab = null;
        var prefabLoadDone = false;

        // load async
        yield return loader.LoadAsset(
            "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName1.prefab",
            (string assetName, GameObject prefabAsset) =>
            {
                prefab = prefabAsset;
                prefabLoadDone = true;
            },
            (assetName, failEnum, reason, status) =>
            {
                prefabLoadDone = true;
                Fail("fail, failEnum:" + failEnum + " reason:" + reason);
            }
        );

        // texture = depended asset.
        Texture2D tex = null;
        var textureLoadDone = false;

        // load async
        yield return loader.LoadAsset(
            "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName.png",
            (string assetName, Texture2D texAsset) =>
            {
                tex = texAsset;
                textureLoadDone = true;
            },
            (assetName, failEnum, reason, status) =>
            {
                textureLoadDone = true;
                Fail("fail, failEnum:" + failEnum + " reason:" + reason);
            }
        );

        yield return WaitUntil(() => textureLoadDone && prefabLoadDone, () => { throw new TimeoutException("texture and prefab load failed in time."); });
        True(tex, "tex is null.");
        True(prefab, "prefab is null.");

        if (prefab != null)
        {
            // check prefab instance contains dependent texture.
            var obj = GameObject.Instantiate(prefab);
            obj.hideFlags = obj.hideFlags | HideFlags.HideAndDontSave;
            var renderer = obj.GetComponent<SpriteRenderer>();
            var sprite = renderer.sprite;
            True(sprite != null, "sprite is null.");
        }
    }

    /*
		A -> B <- C
	*/
    [MTest]
    public IEnumerator Load2AssetsWhichDependsOnSameAssetBundle()
    {
        GameObject prefab1 = null;
        var prefabLoadDone1 = false;

        yield return loader.LoadAsset(
            "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName1.prefab",
            (string assetName, GameObject prefabAsset) =>
            {
                prefab1 = prefabAsset;
                prefabLoadDone1 = true;
            },
            (assetName, failEnum, reason, status) =>
            {
                prefabLoadDone1 = true;
                Fail("fail, failEnum:" + failEnum + " reason:" + reason);
            }
        );

        GameObject prefab2 = null;
        var prefabLoadDone2 = false;
        yield return loader.LoadAsset(
            "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName2.prefab",
            (string assetName, GameObject prefabAsset) =>
            {
                prefab2 = prefabAsset;
                prefabLoadDone2 = true;
            },
            (assetName, failEnum, reason, status) =>
            {
                prefabLoadDone2 = true;
                Fail("fail, failEnum:" + failEnum + " reason:" + reason);
            }
        );

        yield return WaitUntil(() => prefabLoadDone1 && prefabLoadDone2, () => { throw new TimeoutException("prefabs load failed."); });
        True(prefab1, "prefab1 is null.");
        True(prefab2, "prefab2 is null.");

        if (prefab1 != null && prefab2 != null)
        {
            // check prefab instance contains dependent texture.
            var obj = GameObject.Instantiate(prefab1);
            obj.hideFlags = obj.hideFlags | HideFlags.HideAndDontSave;
            var renderer = obj.GetComponent<SpriteRenderer>();
            var sprite = renderer.sprite;
            True(sprite != null, "sprite is null.");

            var obj2 = GameObject.Instantiate(prefab2);
            obj2.hideFlags = obj2.hideFlags | HideFlags.HideAndDontSave;
            var renderer2 = obj2.GetComponent<SpriteRenderer>();
            var sprite2 = renderer2.sprite;
            True(sprite2 != null, "sprite is null.");
        }
    }

    /*
		A -> B -> C
	*/
    [MTest]
    public IEnumerator NestedDependency()
    {
        GameObject prefab = null;
        var prefabLoadDone = false;


        yield return loader.LoadAsset(
            "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/nestedPrefab.prefab",
            (string assetName, GameObject prefabAsset) =>
            {
                prefab = prefabAsset;
                prefabLoadDone = true;
            },
            (assetName, failEnum, reason, status) =>
            {
                Debug.Log("fail, failEnum:" + failEnum + " reason:" + reason);
                Fail("fail, failEnum:" + failEnum + " reason:" + reason);
                prefabLoadDone = true;
            }
        );

        yield return WaitUntil(() => prefabLoadDone, () => { throw new TimeoutException("prefabs load failed."); });
        True(prefab, "prefab is null.");

        if (prefab != null)
        {
            // check prefab instance contains dependent texture.
            var nestedObj = GameObject.Instantiate(prefab);
            nestedObj.hideFlags = nestedObj.hideFlags | HideFlags.HideAndDontSave;
            var scriptObj = nestedObj.GetComponent<PrefabHolder>();
            True(scriptObj != null, "failed to get script.");

            var obj = scriptObj.prefab;
            True(obj != null, "failed to get contained prefab.");

            var renderer = obj.GetComponent<SpriteRenderer>();
            var sprite = renderer.sprite;
            True(sprite != null, "sprite is null.");
        }
    }

    [MTest]
    public IEnumerator LoadCrcMismatchedBundle()
    {
        // change specific bumdle's crc to incorrect one.
        for (var i = 0; i < bundleList.assetBundles.Length; i++)
        {
            var bundle = bundleList.assetBundles[i];
            if (bundle.bundleName == "texturename")
            {
                bundleList.assetBundles[i] = new AssetBundleInfo(bundle.bundleName, bundle.assetNames, bundle.dependsBundleNames, 1, bundle.hash, bundle.size);
            }
        }

        loader = new AssetBundleLoader(identity => abDlPath + "1.0.0/");
        loader.UpdateAssetBundleList(bundleList);

        // intentional fail.
        {
            var done = false;

            yield return loader.LoadAsset(
                            "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName.png",
                            (string assetName, Texture2D texAsset) =>
                            {
                                // do nothing.
                            },
                            (assetName, failEnum, reason, status) =>
                            {
                                True(failEnum == AssetBundleLoadError.CrcMismatched, "error is not crc mismatched. failEnum:" + failEnum);
                                done = true;
                            }
                        );

            yield return WaitUntil(
                () => done,
                () => { throw new TimeoutException("failed to wait crc mismatch."); }
            );
        }

        // refresh list.
        var listCor = LoadListFromWeb(abListPath);

        yield return listCor;
        bundleList = listCor.Current as AssetBundleList;

        loader.UpdateAssetBundleList(bundleList);

        // retry.
        {
            Texture2D tex = null;
            var done = false;

            yield return loader.LoadAsset(
                            "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName.png",
                            (string assetName, Texture2D texAsset) =>
                            {
                                tex = texAsset;
                                done = true;
                            },
                            (assetName, failEnum, reason, status) =>
                            {
                                Debug.Log("fail, failEnum:" + failEnum + " reason:" + reason);
                                Fail("fail, failEnum:" + failEnum + " reason:" + reason);
                                done = true;
                            }
                        );

            yield return WaitUntil(
                () => done,
                () => { throw new TimeoutException("failed to wait crc mismatch."); }
            );

            True(tex != null, "tex is null.");
        }
    }

    [MTest]
    public IEnumerator LoadMissingBundle()
    {
        Debug.LogWarning("指定したassetを含むbundleがDLできない場合のテスト");
        yield break;
    }

    [MTest]
    public IEnumerator LoadMissingDependentBundle()
    {
        Debug.LogWarning("依存したassetが依存しているbundleが存在しなかったり、エラーを出すので、そのエラーがちゃんと出るか試す場合のテスト");
        yield break;
    }

    [MTest]
    public IEnumerator LoadBundleWithTimeout()
    {
        Debug.LogWarning("指定したassetを時間内にDL、展開する(httpにのみ関連する)テスト");
        yield break;
    }

    [MTest]
    public IEnumerator LoadAllAssetsOnce()
    {
        var loadedAssetAssets = new Dictionary<string, object>();
        var assetNames = bundleList.assetBundles.SelectMany(a => a.assetNames).ToArray();

        var loaderGameObject = new GameObject();
        var runner = loaderGameObject.AddComponent<TestMBRunner>();

        foreach (var loadingAssetName in assetNames)
        {
            IEnumerator cor = null;

            switch (loadingAssetName)
            {
                case "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName.png":
                    {

                        cor = loader.LoadAsset<Texture2D>(
                            loadingAssetName,
                            (assetName, asset) =>
                            {
                                loadedAssetAssets[assetName] = asset;
                            },
                            (assetName, failEnum, reason, status) =>
                            {
                                Fail("fail to load assetName:" + assetName + " failEnum:" + failEnum + " reason:" + reason);
                            }
                        );
                        break;
                    }
                default:
                    {
                        cor = loader.LoadAsset<GameObject>(
                            loadingAssetName,
                            (assetName, asset) =>
                            {
                                loadedAssetAssets[assetName] = asset;
                            },
                            (assetName, failEnum, reason, status) =>
                            {
                                Fail("fail to load assetName:" + assetName + " failEnum:" + failEnum + " reason:" + reason);
                            }
                        );
                        break;
                    }
            }

            runner.StartCoroutine(cor);
        }


        yield return WaitUntil(() => loadedAssetAssets.Count == assetNames.Length, () => { throw new TimeoutException("failed to load all assets."); });

        GameObject.Destroy(runner.gameObject);

        foreach (var loadedAssetAssetKey in loadedAssetAssets.Keys)
        {
            var key = loadedAssetAssetKey;
            var asset = loadedAssetAssets[key];
            True(asset != null, "loaded asset:" + key + " is null.");
        }
    }

    [MTest]
    public IEnumerator OnMemoryBundleNames()
    {
        /*
            load all assets.
        */
        yield return LoadAllAssetsOnce();

        var totalBundleCount = bundleList.assetBundles.Length;

        var onMemoryBundleNames = loader.OnMemoryBundleNames();
        True(onMemoryBundleNames.Length == totalBundleCount, "unmatched.");
    }

    [MTest]
    public IEnumerator OnMemoryAssetNames()
    {
        /*
            load all assets.
        */
        yield return LoadAllAssetsOnce();

        var totalAssetCount = bundleList.assetBundles.SelectMany(ab => ab.assetNames).ToArray().Length;

        var onMemoryAssetNames = loader.OnMemoryAssetNames();
        True(onMemoryAssetNames.Length == totalAssetCount, "unmatched.");
    }

    [MTest]
    public IEnumerator UnloadAllAssetBundles()
    {
        /*
            load all.
        */
        LoadAllAssetsOnce();

        /*
            unload all.
        */
        loader.UnloadOnMemoryAssetBundles();

        yield return WaitUntil(
            () =>
            {
                var loadedAssetNames = loader.OnMemoryAssetNames();
                if (loadedAssetNames.Length == 0)
                {
                    return true;
                }
                return false;
            },
            () => { throw new TimeoutException("failed to unload all assets."); }
        );
    }

    [MTest]
    public IEnumerator GetContainedAssetBundleName()
    {
        var assetName = "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName.png";
        var done = false;

        yield return loader.LoadAsset(
            assetName,
            (string loadedAssetName, Texture2D tex) =>
            {
                done = true;
            },
            (loadedAssetName, failEnum, reason, status) =>
            {

            }
        );

        yield return WaitUntil(() => done, () => { throw new TimeoutException("failed to load asset in time."); });

        var containedAssetBundleName = loader.GetContainedAssetBundleName(assetName);
        True(containedAssetBundleName == "texturename", "not match. actual:" + containedAssetBundleName);
    }

    [MTest]
    public IEnumerator UnloadAssetBundle()
    {
        var assetName = "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName.png";
        var done = false;

        yield return loader.LoadAsset(
            assetName,
            (string loadedAssetName, Texture2D prefabAsset) =>
            {
                done = true;
            },
            (loadedAssetName, failEnum, reason, status) => { }
        );

        yield return WaitUntil(() => done, () => { throw new Exception("failed to load asset in time."); });

        var bundleName = loader.GetContainedAssetBundleName(assetName);
        loader.UnloadOnMemoryAssetBundle(bundleName);

        True(!loader.OnMemoryAssetNames().Any(), "not unloaded.");
    }

    [MTest]
    public IEnumerator IsBundleCachedOnStorage()
    {
        var assetName = "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName.png";
        var done = false;

        yield return loader.LoadAsset(
            assetName,
            (string loadedAssetName, Texture2D prefabAsset) =>
            {
                done = true;
            },
            (loadedAssetName, failEnum, reason, status) => { }
        );

        yield return WaitUntil(() => done, () => { throw new TimeoutException("failed to load asset in time."); });

        var bundleName = loader.GetContainedAssetBundleName(assetName);
        True(loader.IsAssetBundleCachedOnStorage(bundleName), "not cached on storage.");
    }

    [MTest]
    public IEnumerator IsBundleCachedOnMemory()
    {
        var assetName = "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName.png";
        var done = false;

        yield return loader.LoadAsset(
            assetName,
            (string loadedAssetName, Texture2D prefabAsset) =>
            {
                done = true;
            },
            (loadedAssetName, failEnum, reason, status) => { }
        );

        yield return WaitUntil(() => done, () => { throw new TimeoutException("failed to load asset in time."); });

        var bundleName = loader.GetContainedAssetBundleName(assetName);
        True(loader.IsAssetBundleCachedOnMemory(bundleName), "not cached on memory.");
    }

    [MTest]
    public IEnumerator AssetBundleInfoFromAssetName()
    {
        var assetName = "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName.png";
        var bundleInfo = loader.AssetBundleInfoOfAsset(assetName);
        NotNull(bundleInfo.assetNames);
        True(bundleInfo.assetNames.Any(), "no assetBundle containes this asset.");
        yield break;
    }

    [MTest]
    public IEnumerator GetAssetBundleSize()
    {
        var assetName = "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName.png";
        var bundleInfo = loader.AssetBundleInfoOfAsset(assetName);

        True(bundleInfo.size != 0, "not match. actual:" + bundleInfo.size);
        yield break;
    }

    [MTest]
    public IEnumerator GetSameAssetBundleOnceThenFailToDownload()
    {
        // 同じbundleをDL中に、最初にDL開始したassetがDL失敗になった際の処理。
        yield break;
    }

    [MTest]
    public IEnumerator LoadSceneFromAssetBundle()
    {
        var listCor = LoadListFromWeb(sceneListPath);

        yield return listCor;
        bundleList = listCor.Current as AssetBundleList;

        loader = new AssetBundleLoader(identity => sceneAbDlPath + "1.0.0/");
        loader.UpdateAssetBundleList(bundleList);

        var cleaned = loader.CleanCachedAssetBundles();

        if (!cleaned)
        {
            Fail("clean cache failed.");
        }

        var done = false;
        var sceneName = string.Empty;

        // シーンをロードする。
        yield return loader.LoadScene(
            "Assets/AutoyaTests/RuntimeData/bundledScene.unity",
            LoadSceneMode.Additive,
            loadedSceneName =>
            {
                sceneName = loadedSceneName;
                done = true;
            },
            (loadFailedSceneName, error, reason, status) =>
            {
                Fail("failed to load scene, loadFailedSceneName:" + loadFailedSceneName + " error:" + error + " reason:" + reason);
            }
        );

        True(done);

        var cor = SceneManager.UnloadSceneAsync(SceneManager.GetSceneByPath(sceneName));
        while (!cor.isDone)
        {
            yield return null;
        }
    }

    // [MTest] public IEnumerator UnloadOnMemoryAssetBundle () {
    // 	Debug.LogError("UnloadOnMemoryAssetBundle not yet.");
    // }

    // [MTest] public IEnumerator UnloadOnMemoryAsset () {
    // 	Debug.LogError("UnloadOnMemoryAsset not yet.");
    // }

    // [MTest] public IEnumerator Offline () {
    // 	Debug.LogError("オフライン時のテストを追加したい。");
    // }
}
