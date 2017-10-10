using System;
using System.Collections;
using System.Collections.Generic;
using AutoyaFramework;
using AutoyaFramework.AssetBundles;
using AutoyaFramework.Settings.AssetBundles;
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

        var listExists = Autoya.AssetBundle_IsAssetBundleReady();
        True(!listExists, "exists, not intended.");

        True(Caching.CleanCache());
    }
    [MTeardown] public IEnumerator Teardown () {
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

        var listExists = Autoya.AssetBundle_IsAssetBundleReady();
        True(!listExists, "exists, not intended.");

        True(Caching.CleanCache());
    }

    private IEnumerator<bool> ShouldContinuePreloading (string[] bundleNames) {
        yield return true;
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
        Debug.Log("listUrl:" + listUrl);
        
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
        True(!Autoya.AssetBundle_IsAssetBundleReady(), "not match.");
        
        var done = false;

        Autoya.AssetBundle_Preload(
            "1.0.0/sample.preloadList.json",
            ShouldContinuePreloading,
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
            ShouldContinuePreloading,
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
            ShouldContinuePreloading,
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
        
        Autoya.AssetBundle_Preload(
            preloadList,
            ShouldContinuePreloading,
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
        
        Autoya.AssetBundle_Preload(
            preloadList,
            ShouldContinuePreloading,
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

    [MTest] public IEnumerator UpdateListWithOnMemoryAssets () {
		Debug.LogWarning("UpdateListWithNoOnMemoryAssets オンメモリにアセットが存在する状態でそのアセットを変更するリスト更新が発生する。その場合でもロード済みのassetは使える。");
		yield break;
	}

	[MTest] public IEnumerator UpdateListWithOnMemoryAssetsThenReloadChangedAsset () {
		Debug.LogWarning("UpdateListWithOnMemoryAssetsThenReloadChangedAsset オンメモリにアセットが存在する状態でそのアセットを変更するリスト更新が発生、もう一度同様のAssetを取得しようとすると、キャッシュではなく新規ロードが行われ、新しいものが取得できる。");
		yield break;
	}
}