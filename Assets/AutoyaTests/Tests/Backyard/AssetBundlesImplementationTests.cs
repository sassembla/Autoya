using System;
using System.Collections;
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
    [MSetup] public IEnumerator Setup () {
        // delete assetBundleList anyway.
        Autoya.AssetBundle_DiscardAssetBundleList();

        var deleted = false;
        Autoya.AssetBundle_DeleteAllStorageCache(
            (result, message) => {
                deleted = result;
                True(deleted, "on setup, not deleted.");
            }
        );
        
        yield return WaitUntil(
            () => deleted,
            
            () => {throw new TimeoutException("not deleted. some assetBundles are in use.");}
        );

        var exists = Autoya.AssetBundle_IsAssetBundleListReady();
        True(!exists, "exists, not intended.");
    }
    [MTeardown] public IEnumerator Teardown () {
        // delete assetBundleList anyway.
        Autoya.AssetBundle_DiscardAssetBundleList();

        var deleted = false;
        Autoya.AssetBundle_DeleteAllStorageCache(
            (result, message) => {
                deleted = result;
                True(deleted, "on teardown, not deleted.");
            }
        );
        
        yield return WaitUntil(
            () => deleted,
            () => {throw new TimeoutException("not deleted. some assetBundles are in use.");}
        );
    }

    private IEnumerator<bool> ShouldContinuePreloading (string[] bundleNames) {
        yield return true;
    }

    [MTest] public IEnumerator GetAssetBundleList () {
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

        yield return WaitUntil(
            () => done,
            () => {throw new TimeoutException("faild to get assetBundleList.");}
        );
    }

    [MTest] public IEnumerator GetAssetBundleListFailThenTryAgain () {
        // fail once.
        {
            var notExistFileName = "fake_AssetBundles.StandaloneOSXIntel64_1_0_0.json";
            var version = "1.0.0";
            var done = false;
            Autoya.AssetBundle_DownloadAssetBundleList(
                notExistFileName,
                version,
                () => {
                    True(false, "should not be succeeded.");
                },
                (code, reason, AutoyaStatus) => {
                    True(code == 404, "code does not match.");
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
            Autoya.AssetBundle_DownloadAssetBundleList(
                fileName,
                version,
                () => {
                    done = true;
                },
                (code, reason, AutoyaStatus) => {
                    // do nothing.
                    True(false, "reason:" + reason);
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
                True(false, "should not comes here.");
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
                True(false, "err:" + err);
            }
        );

        yield return WaitUntil(
            () => done,
            () => {throw new TimeoutException("not yet done.");}
        );

        Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
    }

    [MTest] public IEnumerator PreloadAssetBundleBeforeGetAssetBundleListBecomeFailed () {
        True(!Autoya.AssetBundle_IsAssetBundleListReady(), "not match.");
        var done = false;

        Autoya.AssetBundle_Preload(
            "1.0.0/sample.preloadList.json",
            ShouldContinuePreloading,
            progress => {

            },
            () => {
                True(false, "should not be succeeded.");
            },
            (code, reason, autoyaStatus) => {
                True(code == -1, "not match. code:" + code + " reason:" + reason);
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
                True(false, "should not be failed. code:" + code + " reason:" + reason);
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
                True(false, "should not be failed. code:" + code + " reason:" + reason);
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
                True(false, "should not be failed. code:" + code + " reason:" + reason);
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
                True(false, "should not be failed. code:" + code + " reason:" + reason);
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
        var assetName = "Assets/AutoyaTests/RuntimeData/AssetBundles/TestResources/textureName.png";
        yield return GetAssetBundleList();
        
        var exist = Autoya.AssetBundle_IsAssetExist(assetName);
        True(exist, "not exist:" + assetName);
    }

    [MTest] public IEnumerator IsAssetBundleExistInAssetBundleList () {
        var bundleName = "bundlename";
        yield return GetAssetBundleList();
        
        var exist = Autoya.AssetBundle_IsAssetBundleExist(bundleName);
        True(exist, "not exist:" + bundleName);

    }

    [MTest] public IEnumerator AssetBundle_NotCachedBundleNames () {
        Debug.LogWarning("not yet implemented.");
        yield break;
    }
}