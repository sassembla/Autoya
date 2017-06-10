using AutoyaFramework;
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
        // delete anyway.
        Autoya.AssetBundle_DiscardAssetBundleList();

        var deleted = false;
        Autoya.AssetBundle_DeleteAllStorageCache(
            result => {
                deleted = result;
                Assert(deleted, "not deleted.");
            }
        );
        
        WaitUntil(
            () => deleted,
            5,
            "not deleted. some assetBundles are in use."
        );
    }
    [MTeardown] public void Teardown () {
        // delete anyway.
        Autoya.AssetBundle_DiscardAssetBundleList();

        var deleted = false;
        Autoya.AssetBundle_DeleteAllStorageCache(
            result => {
                deleted = result;
                Assert(deleted, "not deleted.");
            }
        );
        
        WaitUntil(
            () => deleted,
            5,
            "not deleted. some assetBundles are in use."
        );
    }

    [MTest] public void CheckIsAssetBundleExists () {
        var exists = Autoya.AssetBundle_IsAssetBundleListReady();
        Assert(!exists, "exists, not intended.");
    }

    [MTest] public void GetAssetBundleList () {
        var fileName = "1.0.0/AssetBundles.StandaloneOSXIntel64_1_0_0.json";
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleList(
            fileName,
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
            var notExistFileName = "fake_1.0.0/AssetBundles.StandaloneOSXIntel64_1_0_0.json";
            var done = false;
            Autoya.AssetBundle_DownloadAssetBundleList(
                notExistFileName,
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
            var fileName = "1.0.0/AssetBundles.StandaloneOSXIntel64_1_0_0.json";
            var done = false;
            Autoya.AssetBundle_DownloadAssetBundleList(
                fileName,
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

    [MTest] public void GetAssetBundleBeforeGetAssetBundleList () {
        Assert(false, "not yet implemented.");
    }

    [MTest] public void GetAssetBundle () {
        // get list
        Assert(false, "not yet implemented.");
    }

    [MTest] public void PreloadAssetBundleBeforeGetAssetBundleList () {
        Assert(false, "not yet implemented.");
    }

    [MTest] public void PreloadAssetBundle () {
        // get list
        Assert(false, "not yet implemented.");
    }
}