using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoyaFramework;
using AutoyaFramework.AssetBundles;
using AutoyaFramework.Settings.AssetBundles;
using AutoyaFramework.Settings.Auth;
using Miyamasu;
using UnityEngine;

public class AssetBundlesImplementationTests : MiyamasuTestRunner {
    [MSetup] public IEnumerator Setup () {
        var discarded = false;

        // delete assetBundleList anyway.
        Autoya.AssetBundle_DiscardAssetBundleList(
            () => {
                discarded = true;
            },
            (code, reason) => {
                switch (code) {
                    case -1: {
                        discarded = true;
                        break;
                    }
                    default: {
                        Fail("code:" + code + " reason:" + reason);
                        break;
                    }
                }
            }
        );

        yield return WaitUntil(
            () => discarded,
            () => {throw new TimeoutException("too late.");}
        );

        var listExists = Autoya.AssetBundle_IsAssetBundleFeatureReady();
        True(!listExists, "exists, not intended.");

        True(Caching.CleanCache());

        Autoya.Debug_Manifest_RenewRuntimeManifest();
    }
    [MTeardown] public IEnumerator Teardown () {
        Autoya.AssetBundle_UnloadOnMemoryAssetBundles();


        var discarded = false;

        // delete assetBundleList anyway.
        Autoya.AssetBundle_DiscardAssetBundleList(
            () => {
                discarded = true;
            },
            (code, reason) => {
                switch (code) {
                    case -1: {
                        discarded = true;
                        break;
                    }
                    default: {
                        Fail("code:" + code + " reason:" + reason);
                        break;
                    }
                }
            }
        );

        yield return WaitUntil(
            () => discarded,
            () => {throw new TimeoutException("too late.");}
        );

        var listExists = Autoya.AssetBundle_IsAssetBundleFeatureReady();
        True(!listExists, "exists, not intended.");

        True(Caching.CleanCache());
    }


    [MTest] public IEnumerator GetAssetBundleListFromDebugMethod () {
        var fileName = "AssetBundles.StandaloneOSXIntel64_1_0_0.json";
        var version = "1.0.0";
        
        var done = false;
        Autoya.Debug_AssetBundle_DownloadAssetBundleListFromUrl(
            AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSETBUNDLELIST + version + "/" + fileName,
            status => {
                done = true;
            },
            (code, reason, autoyaStatus) => {
                // do nothing.
            }
        );

        yield return WaitUntil(
            () => done,
            () => {throw new TimeoutException("faild to get assetBundleList.");}
        );
    }

    [MTest] public IEnumerator GetAssetBundleList () {
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListIfNeed(
            status => {
                done = true;
            },
            (code, reason, asutoyaStatus) => {
                Debug.Log("GetAssetBundleList failed, code:" + code + " reason:" + reason);
                // do nothing.
            }
        );

        yield return WaitUntil(
            () => done,
            () => {throw new TimeoutException("faild to get assetBundleList.");}
        );
    }

    [MTest] public IEnumerator GetAssetBundleListUrl () {
        var fileName = "AssetBundles.StandaloneOSXIntel64_1_0_0.json";
        var version = "1.0.0";
        
        var listUrl = Autoya.AssetBundle_GetCurrentAssetBundleListUrl();
        
        True(listUrl == AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSETBUNDLELIST + version + "/" + fileName);
        yield break;
    }

    [MTest] public IEnumerator GetAssetBundleListFailThenTryAgain () {
        // fail once.
        {
            var notExistFileName = "fake_AssetBundles.StandaloneOSXIntel64_1_0_0.json";
            var version = "1.0.0";
            var done = false;
            Autoya.Debug_AssetBundle_DownloadAssetBundleListFromUrl(
                AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSETBUNDLELIST + version + "/" + notExistFileName,
                status => {
                    Fail("should not be succeeded.");
                },
                (err, reason, autoyaStatus) => {
                    True(err == Autoya.ListDownloadError.FailedToDownload, "err does not match.");
                    done = true;
                }
            );

            yield return WaitUntil(
                () => done,
                () => {throw new TimeoutException("faild to fail getting assetBundleList.");}
            );
        }

        // try again with valid fileName.
        {
            var fileName = "AssetBundles.StandaloneOSXIntel64_1_0_0.json";
            var version = "1.0.0";

            var done = false;
            Autoya.Debug_AssetBundle_DownloadAssetBundleListFromUrl(
                AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSETBUNDLELIST + version + "/" + fileName,
                status => {
                    done = true;
                },
                (code, reason, autoyaStatus) => {
                    // do nothing.
                    Fail("reason:" + reason);
                }
            );

            yield return WaitUntil(
                () => done,
                () => {throw new TimeoutException("faild to get assetBundleList.");}
            );
        }
    }

    [MTest] public IEnumerator GetAssetBundleBeforeGetAssetBundleListBecomeFailed () {
        var loaderTest = new AssetBundleLoaderTests();
        var cor = loaderTest.LoadListFromWeb();
        yield return cor;

        var list = cor.Current as AssetBundleList;

        var done = false;
        var assetName = list.assetBundles[0].assetNames[0];
        Autoya.AssetBundle_LoadAsset<GameObject>(
            assetName,
            (name, obj) => {
                Fail("should not comes here.");
            },
            (name, err, reason, autoyaStatus) => {
                True(err == AssetBundleLoadError.AssetBundleListIsNotReady, "not match.");
                done = true;
            }
        );

        yield return WaitUntil(
            () => done,
            () => {throw new TimeoutException("not yet failed.");}
        );
    }

    [MTest] public IEnumerator GetAssetBundle () {
        yield return GetAssetBundleList();
        
        var list = Autoya.AssetBundle_AssetBundleList();
        True(list != null);

        var done = false;
        var assetName = list.assetBundles[0].assetNames[0];
        Autoya.AssetBundle_LoadAsset<Texture2D>(
            assetName,
            (name, tex) => {
                done = true;
            },
            (name, err, reason, autoyaStatus) => {
                Fail("err:" + err);
            }
        );

        yield return WaitUntil(
            () => done,
            () => {throw new TimeoutException("not yet done.");}
        );

        Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
    }

    [MTest] public IEnumerator PreloadAssetBundleBeforeGetAssetBundleListWillFail () {
        True(!Autoya.AssetBundle_IsAssetBundleFeatureReady(), "not match.");
        
        var done = false;

        Autoya.AssetBundle_Preload(
            "1.0.0/sample.preloadList.json",
            (willLoadBundleNames, proceed, cancel) => {
				proceed();
			},
            progress => {

            },
            () => {
                Fail("should not be succeeded.");
            },
            (code, reason, autoyaStatus) => {
                True(code == -(int)Autoya.AssetBundlesError.NeedToDownloadAssetBundleList, "not match. code:" + code + " reason:" + reason);
                done = true;
            },
            (failedAssetBundleName, code, reason, autoyaStatus) => {

            },
            1
        );

         yield return WaitUntil(
            () => done,
            () => {throw new TimeoutException("not yet done.");}
        );
    }

    [MTest] public IEnumerator PreloadAssetBundle () {
        yield return GetAssetBundleList();
        
        var done = false;

        Autoya.AssetBundle_Preload(
            "1.0.0/sample.preloadList.json",
            (willLoadBundleNames, proceed, cancel) => {
				proceed();
			},
            progress => {

            },
            () => {
                done = true;
            },
            (code, reason, autoyaStatus) => {
                Fail("should not be failed. code:" + code + " reason:" + reason);
            },
            (failedAssetBundleName, code, reason, autoyaStatus) => {

            },
            1
        );

         yield return WaitUntil(
            () => done,
            () => {throw new TimeoutException("not yet done.");}
        );
    }
    [MTest] public IEnumerator PreloadAssetBundles () {
        yield return GetAssetBundleList();

        var done = false;

        Autoya.AssetBundle_Preload(
            "1.0.0/sample.preloadList2.json",
            (willLoadBundleNames, proceed, cancel) => {
				proceed();
			},
            progress => {

            },
            () => {
                done = true;
            },
            (code, reason, autoyaStatus) => {
                Fail("should not be failed. code:" + code + " reason:" + reason);
            },
            (failedAssetBundleName, code, reason, autoyaStatus) => {

            },
            2
        );

         yield return WaitUntil(
            () => done,
            () => {throw new TimeoutException("not yet done.");}
        );
    }

    [MTest] public IEnumerator PreloadAssetBundleWithGeneratedPreloadList () {
        yield return GetAssetBundleList();

        var done = false;

        var list = Autoya.AssetBundle_AssetBundleList();
        var preloadList = new PreloadList("test", list);

        // rewrite. set 1st content of bundleName.
        preloadList.bundleNames = new string[]{preloadList.bundleNames[0]};
        
        Autoya.AssetBundle_PreloadByList(
            preloadList,
            (willLoadBundleNames, proceed, cancel) => {
				proceed();
			},
            progress => {

            },
            () => {
                done = true;
            },
            (code, reason, autoyaStatus) => {
                Fail("should not be failed. code:" + code + " reason:" + reason);
            },
            (failedAssetBundleName, code, reason, autoyaStatus) => {

            },
            1
        );

         yield return WaitUntil(
            () => done,
            () => {throw new TimeoutException("not yet done.");}
        );
    }
    [MTest] public IEnumerator PreloadAssetBundlesWithGeneratedPreloadList () {
        yield return GetAssetBundleList();

        var done = false;

        var list = Autoya.AssetBundle_AssetBundleList();
        var preloadList = new PreloadList("test", list);
        
        Autoya.AssetBundle_PreloadByList(
            preloadList,
            (willLoadBundleNames, proceed, cancel) => {
                proceed();
            },
            progress => {

            },
            () => {
                done = true;
            },
            (code, reason, autoyaStatus) => {
                Fail("should not be failed. code:" + code + " reason:" + reason);
            },
            (failedAssetBundleName, code, reason, autoyaStatus) => {

            },
            4
        );

         yield return WaitUntil(
            () => done,
            () => {throw new TimeoutException("not yet done.");}
        );
    }

    [MTest] public IEnumerator IsAssetExistInAssetBundleList () {
        yield return GetAssetBundleList();
        var assetName = "Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png";
        var exist = Autoya.AssetBundle_IsAssetExist(assetName);
        True(exist, "not exist:" + assetName);
    }

    [MTest] public IEnumerator IsAssetBundleExistInAssetBundleList () {
        yield return GetAssetBundleList();
        var bundleName = "bundlename";
        var exist = Autoya.AssetBundle_IsAssetBundleExist(bundleName);
        True(exist, "not exist:" + bundleName);

    }

    [MTest] public IEnumerator AssetBundle_NotCachedBundleNames () {
        Debug.LogWarning("AssetBundle_NotCachedBundleNames not yet implemented.");
        yield break;
    }

    private IEnumerator LoadAllAssetBundles (Action<UnityEngine.Object[]> onLoaded) {
        var bundles = Autoya.AssetBundle_AssetBundleList().assetBundles;
        
        var loaded = 0;
        var allAssetCount = bundles.Sum(s => s.assetNames.Length);
        True(1 < allAssetCount);

        var loadedAssets = new UnityEngine.Object[allAssetCount];

        foreach (var bundle in bundles) {
            foreach (var assetName in bundle.assetNames) {
                if (assetName.EndsWith(".png")) {
                    Autoya.AssetBundle_LoadAsset(
                        assetName,
                        (string name, Texture2D o) => {
                            loadedAssets[loaded] = o;
                            loaded++;
                        },
                        (name, error, reason, autoyaStatus) => {
                            Fail(name + " reason:" + reason);
                        }
                    );
                } else {
                    Autoya.AssetBundle_LoadAsset(
                        assetName,
                        (string name, GameObject o) => {
                            loadedAssets[loaded] = o;
                            loaded++;
                        },
                        (name, error, reason, autoyaStatus) => {
                            Fail(name + " reason:" + reason);
                        }
                    );
                }
            }
        }
    
        yield return WaitUntil(
            () => allAssetCount == loaded,
            () => {throw new TimeoutException("");},
            10
        );

        onLoaded(loadedAssets);
    }

    [MTest] public IEnumerator UpdateListWithOnMemoryAssets () {
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListIfNeed(
            status => {
                done = true;
            },
            (code, reason, asutoyaStatus) => {
                Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done,
            () => {throw new TimeoutException("faild to get assetBundleList.");}
        );
        
        True(Autoya.AssetBundle_IsAssetBundleFeatureReady());
        

        UnityEngine.Object[] loadedAssets = null;

        // 全てのABをロード
        yield return LoadAllAssetBundles(objs => {loadedAssets = objs;});

        True(loadedAssets != null);

        // 1.0.1 リストの更新判断の関数をセット
        var listContainsUsingAssetsAndShouldBeUpdate = false;
        Autoya.Debug_SetOverridePoint_ShouldRequestNewAssetBundleList(
            ver => {
                var url = AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSETBUNDLELIST + ver + "/AssetBundles.StandaloneOSXIntel64_" + ver.Replace(".", "_") + ".json";
                return Autoya.ShouldRequestOrNot.Yes(url);
            }
        );

        Autoya.Debug_SetOverridePoint_ShouldUpdateToNewAssetBundleList(
            condition => {
                if (condition == Autoya.CurrentUsingBundleCondition.UsingAssetsAreChanged) {
                    listContainsUsingAssetsAndShouldBeUpdate = true;
                }
                return true;
            }
        );

        // 1.0.1リストを取得
        Autoya.Http_Get(
            "https://httpbin.org/response-headers?" + AuthSettings.AUTH_RESPONSEHEADER_RESVERSION + "=1.0.1", 
            (conId, data) => {
                // pass.
            }, 
            (conId, code, reason, status) => {
                Fail();
            }
        );

        yield return WaitUntil(
            () => listContainsUsingAssetsAndShouldBeUpdate,
            () => {throw new TimeoutException("failed to get response.");},
            10
        );
        
        True(Autoya.AssetBundle_AssetBundleList().version == "1.0.1");

        // load状態のAssetはそのまま使用できる
        for (var i = 0; i < loadedAssets.Length; i++) {
            var loadedAsset = loadedAssets[i];
            True(loadedAsset != null);
        }
	}

	[MTest] public IEnumerator UpdateListWithOnMemoryAssetsThenReloadChangedAsset () {
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListIfNeed(
            status => {
                done = true;
            },
            (code, reason, asutoyaStatus) => {
                Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done,
            () => {throw new TimeoutException("faild to get assetBundleList.");}
        );
        
        True(Autoya.AssetBundle_IsAssetBundleFeatureReady());
        

        UnityEngine.Object[] loadedAssets = null;

        // 全てのABをロード
        yield return LoadAllAssetBundles(objs => {loadedAssets = objs;});

        True(loadedAssets != null);
        var guids = loadedAssets.Select(a => a.GetInstanceID()).ToArray();

        // 1.0.1 リストの更新判断の関数をセット
        var listContainsUsingAssetsAndShouldBeUpdate = false;
        Autoya.Debug_SetOverridePoint_ShouldRequestNewAssetBundleList(
            ver => {
                var url = AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSETBUNDLELIST + ver + "/AssetBundles.StandaloneOSXIntel64_" + ver.Replace(".", "_") + ".json";
                return Autoya.ShouldRequestOrNot.Yes(url);
            }
        );

        Autoya.Debug_SetOverridePoint_ShouldUpdateToNewAssetBundleList(
            condition => {
                if (condition == Autoya.CurrentUsingBundleCondition.UsingAssetsAreChanged) {
                    listContainsUsingAssetsAndShouldBeUpdate = true;
                }
                return true;
            }
        );

        // 1.0.1リストを取得
        Autoya.Http_Get(
            "https://httpbin.org/response-headers?" + AuthSettings.AUTH_RESPONSEHEADER_RESVERSION + "=1.0.1", 
            (conId, data) => {
                // pass.
            }, 
            (conId, code, reason, status) => {
                Fail();
            }
        );

        yield return WaitUntil(
            () => listContainsUsingAssetsAndShouldBeUpdate,
            () => {throw new TimeoutException("failed to get response.");},
            10
        );
        
        True(Autoya.AssetBundle_AssetBundleList().version == "1.0.1");

		// 再度ロード済みのAssetをLoadしようとすると、更新があったABについて最新を取得してくる。

        UnityEngine.Object[] loadedAssets2 = null;
        yield return LoadAllAssetBundles(objs => {loadedAssets2 = objs;});

        var newGuids = loadedAssets2.Select(a => a.GetInstanceID()).ToArray();

        foreach (var newGuid in newGuids) {
            if (guids.Contains(newGuid)) {
                Fail("same instanceId detected. failed to refresh changed assetBundle.");
            }
        }
	}

    [MTest] public IEnumerator UpdateListWithOnMemoryAssetsThenPRreloadChangedAsset () {
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListIfNeed(
            status => {
                done = true;
            },
            (code, reason, asutoyaStatus) => {
                Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done,
            () => {throw new TimeoutException("faild to get assetBundleList.");}
        );
        
        True(Autoya.AssetBundle_IsAssetBundleFeatureReady());
        

        UnityEngine.Object[] loadedAssets = null;

        // 全てのABをロード
        yield return LoadAllAssetBundles(objs => {loadedAssets = objs;});

        True(loadedAssets != null);
        var guids = loadedAssets.Select(a => a.GetInstanceID()).ToArray();
        
        var loadedAssetBundleNames = Autoya.AssetBundle_AssetBundleList().assetBundles.Select(a => a.bundleName).ToArray();

        // 1.0.1 リストの更新判断の関数をセット
        var listContainsUsingAssetsAndShouldBeUpdate = false;
        Autoya.Debug_SetOverridePoint_ShouldRequestNewAssetBundleList(
            ver => {
                var url = AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSETBUNDLELIST + ver + "/AssetBundles.StandaloneOSXIntel64_" + ver.Replace(".", "_") + ".json";
                return Autoya.ShouldRequestOrNot.Yes(url);
            }
        );

        Autoya.Debug_SetOverridePoint_ShouldUpdateToNewAssetBundleList(
            condition => {
                if (condition == Autoya.CurrentUsingBundleCondition.UsingAssetsAreChanged) {
                    listContainsUsingAssetsAndShouldBeUpdate = true;
                }
                return true;
            }
        );

        // 1.0.1リストを取得
        Autoya.Http_Get(
            "https://httpbin.org/response-headers?" + AuthSettings.AUTH_RESPONSEHEADER_RESVERSION + "=1.0.1", 
            (conId, data) => {
                // pass.
            }, 
            (conId, code, reason, status) => {
                Fail();
            }
        );

        yield return WaitUntil(
            () => listContainsUsingAssetsAndShouldBeUpdate,
            () => {throw new TimeoutException("failed to get response.");},
            10
        );
        
        True(Autoya.AssetBundle_AssetBundleList().version == "1.0.1");


        // preload all.
        var preloadDone = false;

        var preloadList = new PreloadList("dummy", loadedAssetBundleNames);
        Autoya.AssetBundle_PreloadByList(
            preloadList,
            (preloadCandidateBundleNames, go, stop) => {
                // all assetBundles should be download.
                True(preloadCandidateBundleNames.Length == guids.Length);
                go();
            },
            progress => {},
            () => {
                preloadDone = true;
            },
            (code, reason, status) => {
                Fail("code:" + code + " reason:" + reason);
            },
            (failedAssetBundleName, code, reason, status) => {
                Fail("failedAssetBundleName:" + failedAssetBundleName + " code:" + code + " reason:" + reason);
            },
            5
        );

        yield return WaitUntil(
            () => preloadDone,
            () => {throw new TimeoutException("failed to preload.");},
            10
        );
    }
}