using System.Collections.Generic;
using AutoyaFramework;
using AutoyaFramework.AssetBundles;
using Miyamasu;
using UnityEngine;

/**
    AssetBundle_IsAssetBundleListReady
        -> stored.
            -> loader generated.
            -> preloader enabled.
                    
        -> not stored.
            download list.
                -> downloaded.
                    -> loader generated.
                    -> preloader enabled.
                -> failed.
                    go back to "not stored"
 */
public class AssetBundlesImplementationTests : MiyamasuTestRunner {
    [MSetup] public void Setup () {
        // delete assetBundleList anyway.
        Autoya.AssetBundle_DiscardAssetBundleList();

        var deleted = false;
        Autoya.AssetBundle_DeleteAllStorageCache(
            (result, message) => {
                deleted = result;
                Assert(deleted, "on setup, not deleted.");
            }
        );
        
        WaitUntil(
            () => deleted,
            5,
            "not deleted. some assetBundles are in use."
        );

        var exists = Autoya.AssetBundle_IsAssetBundleListReady();
        Assert(!exists, "exists, not intended.");
    }
    [MTeardown] public void Teardown () {
        // delete assetBundleList anyway.
        Autoya.AssetBundle_DiscardAssetBundleList();

        var deleted = false;
        Autoya.AssetBundle_DeleteAllStorageCache(
            (result, message) => {
                deleted = result;
                Assert(deleted, "on teardown, not deleted.");
            }
        );
        
        WaitUntil(
            () => deleted,
            5,
            "not deleted. some assetBundles are in use."
        );
    }

    private IEnumerator<bool> ShouldContinuePreloading (string[] bundleNames) {
        yield return true;
    }

    [MTest] public void GetAssetBundleList () {
        var fileName = "AssetBundles.StandaloneOSXIntel64_1_0_0.json";
        var version = "1.0.0";
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleList(
            fileName,
            version,
            () => {
                done = true;
            },
            (code, reason, AutoyaStatus) => {
                // do nothing.
            }
        );

        WaitUntil(
            () => done,
            5,
            "faild to get assetBundleList."
        );
    }

    [MTest] public void GetAssetBundleListFailThenTryAgain () {
        // fail once.
        {
            var notExistFileName = "fake_AssetBundles.StandaloneOSXIntel64_1_0_0.json";
            var version = "1.0.0";
            var done = false;
            Autoya.AssetBundle_DownloadAssetBundleList(
                notExistFileName,
                version,
                () => {
                    Assert(false, "should not be succeeded.");
                },
                (code, reason, AutoyaStatus) => {
                    Assert(code == 404, "code does not match.");
                    done = true;
                }
            );

            WaitUntil(
                () => done,
                5,
                "faild to fail getting assetBundleList."
            );
        }

        // try again with valid fileName.
        {
            var fileName = "AssetBundles.StandaloneOSXIntel64_1_0_0.json";
            var version = "1.0.0";

            var done = false;
            Autoya.AssetBundle_DownloadAssetBundleList(
                fileName,
                version,
                () => {
                    done = true;
                },
                (code, reason, AutoyaStatus) => {
                    // do nothing.
                    Assert(false, "reason:" + reason);
                }
            );

            WaitUntil(
                () => done,
                5,
                "faild to get assetBundleList."
            );
        }
    }

    [MTest] public void GetAssetBundleBeforeGetAssetBundleListBecomeFailed () {
        var loaderTest = new AssetBundleLoaderTests();
        var list = loaderTest.LoadListFromWeb();

        var done = false;
        var assetName = list.assetBundles[0].assetNames[0];
        Autoya.AssetBundle_LoadAsset<GameObject>(
            assetName,
            (name, obj) => {
                Assert(false, "should not comes here.");
            },
            (name, err, reason, autoyaStatus) => {
                Assert(err == AssetBundleLoadError.AssetBundleListIsNotReady, "not match.");
                done = true;
            }
        );

        WaitUntil(
            () => done,
            5,
            "not yet failed."
        );
    }

    [MTest] public void GetAssetBundle () {
        GetAssetBundleList();
        
        var list = Autoya.AssetBundle_AssetBundleList();

        var done = false;
        var assetName = list.assetBundles[0].assetNames[0];
        Autoya.AssetBundle_LoadAsset<Texture2D>(
            assetName,
            (name, tex) => {
                done = true;
            },
            (name, err, reason, autoyaStatus) => {
                Assert(false, "err:" + err);
            }
        );

        WaitUntil(
            () => done,
            5,
            "not yet done."
        );

        RunOnMainThread(
            Autoya.AssetBundle_UnloadOnMemoryAssetBundles
        );
    }

    [MTest] public void PreloadAssetBundleBeforeGetAssetBundleListBecomeFailed () {
        Assert(!Autoya.AssetBundle_IsAssetBundleListReady(), "not match.");
        var done = false;

        Autoya.AssetBundle_Preload(
            "1.0.0/sample.preloadList.json",
            ShouldContinuePreloading,
            progress => {

            },
            () => {
                Assert(false, "should not be succeeded.");
            },
            (code, reason, autoyaStatus) => {
                Assert(code == -1, "not match. code:" + code + " reason:" + reason);
                done = true;
            },
            (failedAssetBundleName, code, reason, autoyaStatus) => {

            },
            1
        );

         WaitUntil(
            () => done,
            5,
            "not yet done."
        );
    }

    [MTest] public void PreloadAssetBundle () {
        GetAssetBundleList();
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
                Assert(false, "should not be failed. code:" + code + " reason:" + reason);
            },
            (failedAssetBundleName, code, reason, autoyaStatus) => {

            },
            1
        );

         WaitUntil(
            () => done,
            5,
            "not yet done."
        );
    }
    [MTest] public void PreloadAssetBundles () {
        GetAssetBundleList();
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
                Assert(false, "should not be failed. code:" + code + " reason:" + reason);
            },
            (failedAssetBundleName, code, reason, autoyaStatus) => {

            },
            2
        );

         WaitUntil(
            () => done,
            5,
            "not yet done."
        );
    }

    [MTest] public void PreloadAssetBundleWithGeneratedPreloadList () {
        GetAssetBundleList();
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
                Assert(false, "should not be failed. code:" + code + " reason:" + reason);
            },
            (failedAssetBundleName, code, reason, autoyaStatus) => {

            },
            1
        );

         WaitUntil(
            () => done,
            5,
            "not yet done."
        );
    }
    [MTest] public void PreloadAssetBundlesWithGeneratedPreloadList () {
        GetAssetBundleList();
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
                Assert(false, "should not be failed. code:" + code + " reason:" + reason);
            },
            (failedAssetBundleName, code, reason, autoyaStatus) => {

            },
            4
        );

         WaitUntil(
            () => done,
            5,
            "not yet done."
        );
    }

    [MTest] public void AssetBundle_NotCachedBundleNames () {
        Debug.LogWarning("not yet implemented.");
    }
}